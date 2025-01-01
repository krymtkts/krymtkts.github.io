module Booklog

open System
open Common

type Booklog =
    abstract date: string
    abstract bookTitle: string
    abstract readCount: int option
    abstract pages: string
    abstract notes: string option

type Book =
    abstract id: string
    abstract bookTitle: string
    abstract bookAuthor: string
    abstract previouslyRead: bool option

type Streak =
    { startDate: DateTime
      endDate: DateTime
      count: int }

type StreakSummary =
    { longest: Streak
      current: Streak
      maxPagesRead: int }

[<RequireQualifiedAccess>]
module Parser =
    open Yaml

    let parseBooklogs (str: string) =
        // NOTE: requires to define as ResizeArray to convert from raw JavaScript array.
        let bs: Booklog ResizeArray = Yaml.parse str

        bs |> List.ofSeq

    let parseBooks (str: string) =
        // NOTE: requires to define as ResizeArray to convert from raw JavaScript array.
        let bs: Book ResizeArray = Yaml.parse str

        bs |> List.ofSeq

[<AutoOpen>]
module Misc =
    open System
    open Feliz

    let private datesInYear year =
        let startDate =
            let d = DateTime(year, 1, 1)

            if d.DayOfWeek = DayOfWeek.Sunday then
                d
            else
                let toMonday = (int d.DayOfWeek + 6) % 7 |> float
                d.AddDays(-toMonday)

        let endDate =
            let d = DateTime(year, 12, 31)

            if d.DayOfWeek = DayOfWeek.Saturday then
                d
            else
                let toSaturday = DayOfWeek.Saturday - d.DayOfWeek |> float
                d.AddDays(toSaturday)

        printfn "startDate: %A, endDate: %A" startDate endDate

        List.unfold
            (fun date ->
                if date > endDate then
                    None
                else
                    Some(date, date.AddDays(1.0)))
            startDate

    let private generateCalendar (year: int) (logs: Booklog list) =
        let dateMap =
            logs
            |> List.map (_.date >> DateTime.Parse)
            |> List.groupBy id
            |> List.map (fun (d, logs) -> (d, logs |> List.length))
            |> Map.ofList

        let days = datesInYear year |> List.groupBy _.DayOfWeek
        let empty = Html.th []

        let header =
            days
            |> List.head
            |> snd
            |> List.map (fun d ->
                let startOfWeek = d
                let endOfWeek = d.AddDays(6)

                if startOfWeek.Day = 1 then
                    startOfWeek |> DateTime.toShortMonthName
                else if endOfWeek.Year <> year then
                    ""
                else if startOfWeek.Month <> endOfWeek.Month then
                    endOfWeek |> DateTime.toShortMonthName
                else
                    "")
            |> List.map (fun m -> Html.th [ prop.children [ Html.span m ] ])

        let calendar =
            days
            |> List.map (fun (dow, dates) ->
                let dowCell =
                    match dow with
                    | DayOfWeek.Sunday
                    | DayOfWeek.Wednesday
                    | DayOfWeek.Friday ->
                        Html.td [
                            prop.className "dow"
                            prop.children [ Html.span (dow |> DateTime.toShortDayName) ]
                        ]
                    | _ -> Html.td []

                let dowRows =
                    dates
                    |> List.map (fun d ->
                        let cls, anchor =
                            match Map.tryFind d dateMap with
                            | Some count ->
                                let date = d |> DateTime.toRFC3339Date
                                "log", [ Html.a [ prop.href $"#{date}-{count}"; prop.title $"{date} ({count})" ] ]
                            | None -> if d.Year <> year then "other-year", [] else "no-log", []

                        Html.td [ prop.className cls; prop.children anchor ])

                Html.tr [ prop.children (dowCell :: dowRows) ])

        Html.div [
            prop.className "section calendar-container"
            prop.children [
                Html.table [
                    prop.className "calendar"
                    prop.children [ Html.thead [ Html.tr (empty :: header) ]; Html.tbody calendar ]
                ]
            ]
        ]

    let private (|Int|_|) (str: string) =
        match System.Int32.TryParse str with
        | true, int -> Some int
        | _ -> None

    let private romanMap = Map [ ('i', 1); ('v', 5); ('x', 10) ]

    let private toInt x =
        x
        |> Char.ToLower
        |> romanMap.TryFind
        |> function
            | Some v -> v
            | None -> 0

    let private romanToArabic (roman: string) =
        let roman = roman |> _.Trim()

        roman
        |> Seq.map toInt
        |> Seq.pairwise
        |> Seq.fold (fun acc (v1, v2) -> if v1 < v2 then acc - v1 else acc + v1) 0
        |> (+) (Seq.last roman |> toInt)

    let private (|Roman|_|) (str: string) =
        match romanToArabic str with
        | 0 -> None
        | i -> Some i

    let readPages (pages: string) =
        pages
        |> _.Split([| ',' |])
        |> Seq.map _.Split([| '~' |])
        |> Seq.map (function
            | [| Int s; Int e |] -> e - s + 1
            | [| Int _ |] -> 1
            | [| Roman s; Roman e |] -> e - s + 1
            | [| Roman _ |] -> 1
            | _ -> 0)
        |> Seq.sum

    let private getMaxPagesRead (booklogs: Booklog list) =
        booklogs |> List.map (_.pages >> readPages) |> List.max

    let private getStreakSummary (booklogs: Booklog list) =
        let current, longest =
            booklogs
            |> List.map (_.date >> DateTime.Parse)
            |> List.groupBy id
            |> List.map fst
            |> List.sortBy id
            |> List.fold
                (fun (current, longest) dt ->
                    let current =
                        match current with
                        | Some current when dt = current.endDate.AddDays(1) ->
                            { current with
                                endDate = dt
                                count = current.count + 1 }
                        | Some _
                        | None ->
                            { startDate = dt
                              endDate = dt
                              count = 1 }

                    let longest =
                        match longest with
                        | Some longest -> if current.count > longest.count then current else longest
                        | None -> current

                    Some current, Some longest)
                (None, None)

        let maxPagesRead = getMaxPagesRead booklogs

        { longest = longest.Value
          current = current.Value
          maxPagesRead = maxPagesRead }

    let generateBooklogStats (booklogs: Booklog list) =
        let streak = booklogs |> getStreakSummary

        Html.div [
            prop.className "streak"
            prop.children [
                Html.text
                    $"current: {streak.current.count}, longest: {streak.longest.count}, max pages read: {streak.maxPagesRead}"
            ]
        ]

    let private generateLink (href: 'T -> string) (text: 'T -> string) (x: 'T) =
        Html.a [ prop.href (href x); prop.children [ Html.text (text x) ] ]

    let private generateLinks (className: string) (href: 'T -> string) (text: 'T -> string) pages =
        let links = pages |> List.map (fun x -> Html.li [ generateLink href text x ])

        Html.ul [ prop.className className; prop.children links ]

    let generateBooklogLinks baseUrl years =
        generateLinks "booklog-links" (fun year -> $"{baseUrl}/{year}.html") string years

    let private generateBookLink baseUrl (book: Book) =
        generateLink (fun (book: Book) -> $"{baseUrl}/{book.id}.html") _.bookTitle book

    let generateBookLinks baseUrl (books: Book list) =
        generateLinks "book-links" (fun (book: Book) -> $"{baseUrl}/{book.id}.html") _.bookTitle books

    let generateBooklogNotes (notes: string option) =
        notes
        |> function
            | Some notes -> Html.p [ prop.dangerouslySetInnerHTML (Parser.parseMarkdown notes) ]
            | None -> Html.p []

    let private generateBooklogList
        baseUrl
        links
        booklogStreaks
        (books: Map<string, Book>)
        (year: int)
        (logs: Booklog list)
        =
        let header =
            Html.h1 [ prop.className "title"; prop.children (Html.text $"Booklog {year}") ]

        let booklogCalendar = generateCalendar year logs

        let readPages (pages: string) =
            pages
            |> readPages
            |> function
                | 0 -> ""
                | i -> $", pages read: {i}"

        let booklogRows =
            logs
            |> List.groupBy _.date
            |> List.map (fun (_, logs) -> logs |> List.mapi (fun i log -> (i, log)))
            |> List.concat
            |> List.rev
            |> List.map (fun (i, log) ->
                let notes = log.notes |> generateBooklogNotes
                let book = Map.tryFind log.bookTitle books

                Html.div [
                    prop.className "section"
                    prop.children [
                        Html.p [
                            prop.id $"{log.date}-{i + 1}"
                            prop.className "title is-4"
                            prop.children [ Html.text log.date ]
                        ]
                        Html.p [
                            prop.className "subtitle content is-small"
                            prop.children [
                                (match book with
                                 | Some book -> book |> generateBookLink baseUrl
                                 | None -> Html.text log.bookTitle)
                                Html.text ", read count: "
                                Html.text (
                                    let rc =
                                        log.readCount
                                        |> function
                                            | Some rc -> rc
                                            | None -> 1

                                    book
                                    |> function
                                        | Some book -> book.previouslyRead
                                        | _ -> None
                                    |> function
                                        | Some pr when pr -> $"n+{rc}"
                                        | _ -> $"{rc}"
                                )
                                Html.text ", page: "
                                Html.text log.pages
                                log.pages |> readPages |> Html.text
                            ]
                        ]
                        notes
                    ]
                ])

        [ header; booklogStreaks; booklogCalendar; Html.div booklogRows; links ]

    let private generateBooklogSummary links (book: Book) (logs: Booklog list) =
        let header =
            Html.h1 [
                prop.className "title"
                prop.children (Html.text $"Booklog - {book.bookTitle}")
            ]

        let booklogRows =
            logs
            |> List.filter (_.notes >> Option.isSome)
            |> List.map (fun (log) ->
                let notes = log.notes |> generateBooklogNotes

                [ notes
                  Html.p [
                      prop.className "content is-small booklog-info"
                      prop.children [
                          Html.text log.date
                          Html.text ", read count: "
                          Html.text (
                              let rc =
                                  log.readCount
                                  |> function
                                      | Some rc -> rc
                                      | None -> 1

                              book.previouslyRead
                              |> function
                                  | Some pr when pr -> $"n+{rc}"
                                  | _ -> $"{rc}"
                          )
                          Html.text ", page: "
                          Html.text log.pages
                      ]
                  ] ])


        [ header
          Html.div [ prop.className "section"; prop.children (booklogRows |> List.concat) ]
          links ]

    let groupBooklogsByYear (booklogs: Booklog list) =
        let minYear = booklogs |> List.map (_.date >> DateTime.Parse >> _.Year) |> List.min

        let booklogPerYear =
            booklogs |> List.groupBy (_.date >> DateTime.Parse >> _.Year) |> Map.ofList

        (minYear, booklogPerYear)

    let groupBooklogsByTitle (booklogs: Booklog list) =
        booklogs |> List.groupBy (_.bookTitle) |> Map.ofList

    let getBookMap (books: Book list) =
        books |> List.map (fun book -> book.bookTitle, book) |> Map.ofList

    let inline private parseBooklog<'D, 'T
        when 'D: (member basePath: string) and 'D: (member priority: string) and 'T: (member date: string)>
        (conf: FrameConfiguration)
        (def: 'D)
        (getId: 'D -> string)
        (generate: 'D -> 'T list -> Fable.React.ReactElement list)
        (booklogs: 'T list)
        =
        let id = getId def

        let content =
            booklogs
            |> generate def
            |> frame
                { conf with
                    title = $"%s{conf.title} - %s{id}"
                    url = $"%s{conf.url}%s{def.basePath}" }
            |> Parser.parseReactStaticHtml

        let lastmod = booklogs |> List.maxBy _.date |> _.date

        let loc: Xml.SiteLocation =
            { loc = sourceToSitemap def.basePath id
              lastmod = lastmod
              priority = def.priority }

        content, loc, id

    type BooklogDef =
        { priority: string
          basePath: string
          links: Fable.React.ReactElement
          books: Map<string, Book>
          year: int
          stats: Fable.React.ReactElement }

    let generateYearlyBooklogContent (conf: FrameConfiguration) (def: BooklogDef) (booklogs: Booklog list) =
        parseBooklog
            conf
            def
            (fun def -> def.year |> string)
            (fun def -> generateBooklogList def.basePath def.links def.stats def.books def.year)
            booklogs

    type BookDef =
        { priority: string
          basePath: string
          links: Fable.React.ReactElement
          book: Book }

    let generateBooklogSummaryContent (conf: FrameConfiguration) (def: BookDef) (booklogs: Booklog list) =
        parseBooklog conf def (fun def -> def.book.id) (fun def -> generateBooklogSummary def.links def.book) booklogs
