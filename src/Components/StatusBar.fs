module App.Components.StatusBar

open App
open Feliz

let private coordinateText point =
    match point with
    | Some position -> $"X: {position.X} Y: {position.Y}"
    | None -> "X: -- Y: --"

[<ReactComponent>]
let StatusBar (model: Model) =
    Html.footer [
        prop.testId "status-bar"
        prop.className "flex flex-wrap items-center justify-between gap-3 rounded border border-zinc-300 bg-white px-3 py-2 text-xs font-medium text-zinc-700"
        prop.children [
            Html.span [ prop.testId "status-coordinates"; prop.text (coordinateText model.Mouse.Current) ]
            Html.span [ prop.testId "status-zoom"; prop.text $"Zoom: {model.UI.Zoom}x" ]
        ]
    ]
