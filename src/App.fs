module App

open StaticWebGenerator

render
    { stage = dev
      lang = "ja"
      siteName = "krymtkts"
      description = "krymtkts's personal blog"
      siteUrl = "https://krymtkts.github.io"
      pathRoot = ""
      copyright = "2019 - 2025 krymtkts"
      favicon = "/img/favicon.ico"
      highlightStyle = "node_modules/highlight.js/styles/base16/solarized-dark.min.css"

      src = "contents"
      dst = "docs"

      posts = { root = "/posts"; title = "Posts" }
      pages = { root = "/pages"; title = "Pages" }
      tags = { root = "/tags"; title = "Tags" }
      archives =
        { root = "/archives"
          title = "Archives" }
      books = { root = "/books"; title = "Books" }
      booklogs =
        { root = "/booklogs"
          title = "Booklogs" }
      images = "/img"

      feedName = "feed"

      additionalNavs =
        [ { text = "About"
            path = "/pages/about.html" } ]

      additionalMetaContents =
        [ { name = "google-site-verification"
            content = "MGs7mZDRzR9kWtzDfPFcrtqiup4RFjjSMM8m8DZpmJg" }
          { name = "msvalidate.01"
            content = "ECC9D19FB8864FDFF9B374295C7A0399" } ]

      timeZone = "Asia/Tokyo"

      sitemap =
        { index = 1.0
          archives = 0.8
          tags = 0.8
          posts = 0.9
          pages = 0.8
          booklogs = 0.9 } }
