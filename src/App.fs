module App

open StaticWebGenerator

let private readAndWrite navbar title source dist =
    promise {
        printfn "Rendering %s..." source
        let! m = IO.readFile source

        let frontMatter, content =
            m
            |> Parser.parseMarkdownAsReactEl "content"
            |> fun (fm, c) ->
                let title =
                    match fm with
                    | Some fm -> sprintf "%s - %s" title fm.title
                    | None -> title

                fm, frame navbar title c |> Parser.parseReactStatic


        printfn "Writing %s..." dist

        do! IO.writeFile dist content
        return frontMatter
    }

let renderTags navbar title (meta: Meta seq) dist =
    let tagsContent, tagPageContents = generateTagsContent meta
    let frame = frame navbar

    promise {
        printfn "Rendering tags..."
        let title = (sprintf "%s - Tags" title)

        let content =
            tagsContent
            |> frame title
            |> Parser.parseReactStatic

        printfn "Writing tags %s..." dist

        do! IO.writeFile dist content

        return!
            tagPageContents
            |> List.map (fun (tag, tagPageContent) ->
                let dist = IO.resolve (sprintf "docs/tags/%s.html" tag)

                printfn "Writing tag %s..." dist

                let content =
                    tagPageContent
                    |> frame (sprintf "%s - %s" title tag)
                    |> Parser.parseReactStatic

                IO.writeFile dist content |> Promise.map ignore)
            |> Promise.all
            |> Promise.map ignore
    }

let renderArchives navbar title (metaPosts: Meta seq) (metaPages: Meta seq) dist =
    promise {
        printfn "Rendering archives..."
        let! archives = generateArchives metaPosts metaPages

        let content =
            archives
            |> frame navbar (sprintf "%s - Archives" title)
            |> Parser.parseReactStatic

        printfn "Writing archives %s..." dist

        do! IO.writeFile dist content
    }

let private renderPosts sourceDir distDir (navbar: Fable.React.ReactElement) title =
    promise {
        let! files = getMarkdownFiles sourceDir
        let rw = readAndWrite navbar title

        do!
            getLatestPost files
            |> fun source ->
                let dist = IO.resolve "docs/index.html"

                rw source dist |> Promise.map ignore

        return!
            files
            |> List.map (fun source ->
                let dist = getDistPath source distDir

                promise {
                    let! fm = rw source dist

                    return
                        { frontMatter = fm
                          source = source
                          dist = dist }
                })
            |> Promise.all
    }

let private renderMarkdowns sourceDir distDir navbar title =
    promise {
        let! files = getMarkdownFiles sourceDir
        let rw = readAndWrite navbar title

        return!
            files
            |> List.map (fun source ->
                let dist = getDistPath source distDir

                promise {
                    let! fm = rw source dist

                    return
                        { frontMatter = fm
                          source = source
                          dist = dist }
                })
            |> Promise.all
    }

let private render () =
    promise {
        let title = "Blog Title"
        let navbar = generateNavbar title

        let! metaPosts = renderPosts "contents/posts" "docs/posts" navbar title
        let! metaPages = renderMarkdowns "contents/pages" "docs/pages" navbar title

        do! renderArchives navbar title metaPosts metaPages "docs/archives.html"
        let meta = Seq.concat [ metaPosts; metaPages ]
        do! renderTags navbar title meta "docs/tags.html"
        do! IO.copy "contents/fable.ico" "docs/fable.ico"

        printfn "Render complete!"
    }
    |> ignore

render ()
