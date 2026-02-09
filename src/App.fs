namespace App

open Fable.Core
open Feliz
open Feliz.UseElmish
open App.Components.CanvasView
open App.Components.PatternPalette
open App.Components.Toolbar
open App.Components.StatusBar

[<Erase; Mangle(false)>]
type AppRoot =

    [<ReactComponent(true)>]
    static member App() =
        let model, dispatch = React.useElmish (Runtime.init, Runtime.update, [||])

        Html.main [
            prop.className "min-h-screen bg-stone-100 px-4 py-6"
            prop.children [
                Html.div [
                    prop.className "mx-auto flex w-full max-w-[1280px] flex-col gap-3"
                    prop.children [
                        Html.h1 [
                            prop.testId "hello-title"
                            prop.className "text-4xl font-bold tracking-tight text-zinc-900"
                            prop.text "Hello ElmishPaint"
                        ]
                        Toolbar model dispatch
                        Html.div [
                            prop.className "grid grid-cols-1 gap-3 xl:grid-cols-[18rem_minmax(0,_1fr)]"
                            prop.children [
                                Html.aside [
                                    prop.className "rounded border border-zinc-300 bg-white p-3"
                                    prop.children [
                                        Html.h2 [
                                            prop.className "mb-2 text-sm font-semibold uppercase tracking-wide text-zinc-700"
                                            prop.text "Patterns"
                                        ]
                                        PatternPalette model.Pattern dispatch
                                    ]
                                ]
                                Html.section [
                                    prop.className "rounded border border-zinc-300 bg-white p-2"
                                    prop.children [ CanvasView model dispatch ]
                                ]
                            ]
                        ]
                        StatusBar model
                        Html.section [
                            prop.className "flex items-center gap-4 text-xs text-zinc-600"
                            prop.children [
                                Html.p [ prop.testId "active-tool"; prop.text $"Active tool: {model.Tool}" ]
                                Html.p [
                                    prop.testId "active-pattern"
                                    prop.text $"Active pattern: {model.Pattern.Name}"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
