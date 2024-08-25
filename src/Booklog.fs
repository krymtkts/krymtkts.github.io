module Booklog

open Common

module Parser =
    open Yaml

    type Booklog =
        abstract date: string
        abstract bookTitle: string
        abstract bookAuthor: string
        abstract readCount: int option
        abstract previouslyRead: bool option
        abstract pages: string
        abstract tags: string ResizeArray option
        abstract notes: string option

    let parseBooklogs (str: string) =
        // NOTE: requires to define as ResizeArray to convert from raw JavaScript array.
        let bs: Booklog ResizeArray = Yaml.parse str

        bs |> List.ofSeq

[<AutoOpen>]
module Misc =
    open System
    open Feliz
    open Parser

    let datesInYear year =
        let startDate =
            let d = DateTime(year, 1, 1)
            if d.DayOfWeek = DayOfWeek.Sunday then
                d
            else
                d.AddDays(-(2.0 - (d.DayOfWeek |> float)))
        let endDate =
            let d = DateTime(year, 12, 31)
            if d.DayOfWeek = DayOfWeek.Saturday then
                d
            else
                d.AddDays(2.0 - (d.DayOfWeek |> float))
        printfn "startDate: %A, endDate: %A" startDate endDate

        List.unfold
            (fun date ->
                if date > endDate then
                    None
                else
                    Some(date, date.AddDays(1.0)))
            startDate

    let generateCalendar (year: int) (logs: Booklog list) =
        let map = logs |> List.map (_.date >> DateTime.Parse) |> Set.ofList
        let days = datesInYear year |> List.groupBy _.DayOfWeek
        let calendar=
            days |> List.map (fun (dow, dates) ->
                dates |> List.map (fun d ->
                    let d =
                        if Set.contains d map then
                            "log"
                        else if d.Year <> year then
                            "other-year"
                        else
                            "no-log"

                    Html.td [ prop.className [d] ]
                )
                |> Html.tr
        )
        Html.table [ prop.className "calendar"
                     prop.children [
                            Html.thead [
                                Html.tr [
                                ]
                            ]
                            Html.tbody calendar
                     ] ]

        // TODO: sample implementation. generating HTML table from Booklog list.
    let generateBooklogTable (year: int)(logs: Booklog list) =
        let header = Html.h1 [ Html.text $"Booklog {year}" ]
        let booklogCalendar =  generateCalendar year logs
        let booklogRows =
            logs
            |> List.map (fun log ->
                let tags =
                    log.tags
                    |> function
                        | Some ts -> ts |> List.ofSeq
                        | None -> []
                    |> String.concat ", "

                Html.tr [ Html.td [ Html.text log.date ]
                          Html.td [ Html.text log.bookTitle ]
                          Html.td [ Html.text log.bookAuthor ]
                          Html.td [ Html.text $"read count: {log.readCount |> Option.defaultValue 0}" ]
                          Html.td [ Html.text $"previously read: {log.previouslyRead |> Option.defaultValue false}" ]
                          Html.td [ Html.text $"pages: {log.pages}" ]
                          Html.td [ Html.text tags ]
                          Html.td [ Html.text (log.notes |> Option.defaultValue "") ] ])

        let booklogTable =
            Html.table [ Html.thead [ Html.tr [ Html.th [ Html.text "Date" ]
                                                Html.th [ Html.text "Title" ]
                                                Html.th [ Html.text "Author" ]
                                                Html.th [ Html.text "Read Count" ]
                                                Html.th [ Html.text "Previously Read" ]
                                                Html.th [ Html.text "Pages" ]
                                                Html.th [ Html.text "Tags" ]
                                                Html.th [ Html.text "Notes" ] ] ]
                         Html.tbody booklogRows ]

        [ header; booklogCalendar; booklogTable ]

    let groupBooklogs (booklogs: Booklog list) =
        let minYear = booklogs |> List.map (_.date >> DateTime.Parse >> _.Year) |> List.min
        let booklogPerYear = booklogs |> List.groupBy (_.date >> DateTime.Parse >> _.Year) |> Map.ofList
        (minYear, booklogPerYear)
