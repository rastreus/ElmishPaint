module App.Components.PatternPalette

open App
open App.Canvas
open Feliz

let private swatchClass isActive =
    if isActive then
        "rounded-sm border-2 border-zinc-900 bg-white p-1 ring-2 ring-zinc-900 ring-offset-1"
    else
        "rounded-sm border-2 border-zinc-400 bg-white p-1"

[<ReactComponent>]
let private PatternPreview (pattern: Pattern) =
    Html.div [
        prop.className "grid grid-cols-8"
        prop.style [
            style.width (length.px 24)
            style.height (length.px 24)
        ]
        prop.children [
            for y in 0..7 do
                for x in 0..7 do
                    Html.div [
                        prop.key $"{pattern.Id}-{x}-{y}"
                        prop.className (if pattern.Tile[y][x] then "bg-black" else "bg-white")
                        prop.style [
                            style.width (length.px 3)
                            style.height (length.px 3)
                        ]
                    ]
        ]
    ]

[<ReactComponent>]
let PatternPalette (activePattern: Pattern) (dispatch: Msg -> unit) =
    Html.div [
        prop.testId "pattern-palette"
        prop.className "grid grid-cols-4 gap-2"
        prop.children [
            for pattern in Patterns.all do
                let isActive = pattern.Id = activePattern.Id

                Html.button [
                    prop.key pattern.Id
                    prop.testId $"pattern-swatch-{pattern.Id}"
                    prop.type'.button
                    prop.className (swatchClass isActive)
                    prop.title pattern.Name
                    prop.ariaLabel $"select pattern {pattern.Name}"
                    prop.onClick (fun _ -> dispatch (SelectPattern pattern))
                    prop.children [ PatternPreview pattern ]
                ]
        ]
    ]
