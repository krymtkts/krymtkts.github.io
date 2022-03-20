(ns cryogen.compiler
  (:require [clojure.java.io :as io]
            [clojure.pprint :refer [pprint]]
            [io.aviso.exception :refer [write-exception]]
            [text-decoration.core :refer :all]
            [cryogen-core.io :as cryogen-io]
            [cryogen-core.compiler :refer :all
             :exclude [add-description compile-assets compile-assets-timed -main]]
            [cryogen-core.config :refer [resolve-config]]
            [cryogen-core.klipse :as klipse]
            [cryogen-core.rss :as rss]
            [cryogen-core.sass :as sass]
            [cryogen-core.sitemap :as sitemap]
            [cryogen-core.util :as util]
            [clojure.string :as str])
  (:import java.util.Locale
           (java.util Date)))

(defn add-description
  "Add plain text `:description` to the page/post for use in meta description etc."
  [{:keys [blocks-per-preview description-include-elements]
    :or   {description-include-elements #{:p :h1 :h2 :h3 :h4 :h5 :h6}}}
   page]
  (update
    page :description
    #(cond
       (false? %) nil  ;; if set via page meta to false, do not set
       % %    ;; if set via page meta, use it
       :else (util/enlive->html-text (:content-dom page)))))

(defn compile-assets
  "Generates all the html and copies over resources specified in the config.

  Params:
   - `overrides-and-hooks` - may contain overrides for `config.edn`; anything
      here will be available to the page templates, except for the following special
                parameters:
     - `:extend-params-fn` - a function (`params`, `site-data`) -> `params` -
                             use it to derive/add additional params for templates
     - `:postprocess-article-html-fn` - a function (`article`, `params`) -> `article`
                             called after the `:content` has been rendered to HTML and
                              right before it is written to the disk. Example fn:
                              `(fn postprocess [article params] (update article :content selmer.parser/render params))`
     - `:update-article-fn` - a function (`article`, `config`) -> `article` to update a
                            parsed page/post. Return nil to exclude it.
     - `changeset` - as supplied by
                   [[cryogen-core.watcher/start-watcher-for-changes!]] to its callback
                   for incremental compilation, see [[only-changed-files-filter]]
                   for details

  Note on terminology:
   - `article` - a post or page data (including its title, content, etc.)
   - `config` - the site-wide configuration ± from `config.edn` and the provided overrides
   - `params` - `config` + content such as `:pages` etc.
   - `site-data` - a subset of the site content such as `:pages`, `:posts` - see the code below"
  ([] (compile-assets {} nil))
  ([overrides-and-hooks] (compile-assets overrides-and-hooks nil))
  ([{:keys [extend-params-fn update-article-fn]
     :or   {extend-params-fn            (fn [params _] params)
            update-article-fn           (fn [article _] article)}
     :as   overrides-and-hooks}
    changeset]
   (println (green "compiling assets..."))
   (when-not (empty? overrides-and-hooks)
     (println (yellow "overriding config.edn with:"))
     (pprint overrides-and-hooks))
   (let [inc-compile? (seq changeset) ; Don't recompile unchanged posts/pages (ie. most time-consuming)
         inc-compile-filter (only-changed-files-filter changeset)
         overrides    (dissoc overrides-and-hooks
                              :extend-params-fn :update-article-fn)
         {:keys [^String site-url blog-prefix rss-name recent-posts keep-files ignored-files previews? author-root-uri theme]
          :as   config} (resolve-config overrides)
         posts        (->> (read-posts config inc-compile-filter)
                           (add-prev-next)
                           (map klipse/klipsify)
                           (map (partial add-description config))
                           (map #(update-article-fn % config))
                           (remove nil?))
         posts-by-tag (group-by-tags posts)
         posts        (tag-posts posts config)
         latest-posts (->> posts (take recent-posts) vec)
         pages        (->> (read-pages config inc-compile-filter)
                           (map klipse/klipsify)
                           (map (partial add-description config))
                           (map #(update-article-fn % config))
                           (remove nil?))
         home-page    (->> pages
                           (filter #(boolean (:home? %)))
                           (first))
         other-pages  (->> pages
                           (remove #{home-page})
                           (add-prev-next))
         [navbar-pages
          sidebar-pages] (group-pages other-pages)
         params0      (merge
                       config
                       {:today         (Date.)
                        :title         (:site-title config)
                        :active-page   "home"
                        :tags          (map (partial tag-info config) (keys posts-by-tag))
                        :latest-posts  latest-posts
                        :navbar-pages  navbar-pages
                        :sidebar-pages sidebar-pages
                        :home-page     (if home-page
                                         home-page
                                         (assoc (first latest-posts) :layout "home.html"))
                        :archives-uri  (page-uri "archives.html" config)
                        :index-uri     (page-uri "index.html" config)
                        :tags-uri      (page-uri "tags.html" config)
                        :rss-uri       (cryogen-io/path "/" blog-prefix rss-name)
                        :site-url      (if (.endsWith site-url "/") (.substring site-url 0 (dec (count site-url))) site-url)})
         params       (extend-params-fn
                        params0
                        {:posts posts
                         :pages pages
                         :posts-by-tag posts-by-tag
                         :navbar-pages navbar-pages
                         :sidebar-pages sidebar-pages})]

     (assert (not (and (:posts params) (not (:posts params0))))
             (str "Sorry, you cannot add `:posts` to params because this is"
                  " used internally at some places. Pick a different keyword."))

     (selmer.parser/set-resource-path!
       (util/file->url (io/as-file (cryogen-io/path "themes" theme))))
     (cryogen-io/set-public-path! (:public-dest config))

     (when-not inc-compile?
       (cryogen-io/wipe-public-folder keep-files))
     (println (blue "compiling sass"))
     (sass/compile-sass->css! config)
     (println (blue "copying theme resources"))
     (cryogen-io/copy-resources-from-theme config)
     (println (blue "copying resources"))
     (cryogen-io/copy-resources "content" config)
     (copy-resources-from-markup-folders config)
     (compile-pages params other-pages)
     (compile-posts params posts)
     (compile-tags params posts-by-tag)
     (compile-tags-page params)
     (if previews?
       (compile-preview-pages params posts)
       (compile-index params))
     (compile-archives params posts)
     (when author-root-uri
       (println (blue "generating authors views"))
       (compile-authors params posts))
     (println (blue "generating site map"))
     (->> (sitemap/generate site-url config)
          (cryogen-io/create-file (cryogen-io/path "/" blog-prefix "sitemap.xml")))
     (println (blue "generating main rss"))
     (->> (rss/make-channel config posts)
          (cryogen-io/create-file (cryogen-io/path "/" blog-prefix rss-name)))
     (if (:rss-filters config) (println (blue "generating filtered rss")))
     (rss/make-filtered-channels config posts-by-tag)
     (when inc-compile?
       (println (yellow "BEWARE: Incremental compilation, things are missing. Make a full build before publishing."))))))

(defn compile-assets-timed
  "See the docstring for [[compile-assets]]"
  ([] (compile-assets-timed {}))
  ([config] (compile-assets-timed config {}))
  ([config changeset]
   (time
    (try
      (compile-assets config changeset)
      (catch Exception e
        (if (or (instance? IllegalArgumentException e)
                (instance? clojure.lang.ExceptionInfo e))
          (println (red "Error:") (yellow (.getMessage e)))
          (write-exception e)))))))
