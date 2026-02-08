namespace App

open Fable.Core
open Feliz
open Feliz.UseElmish
open App.Components.CanvasView

[<Erase; Mangle(false)>]
type Components =

    [<ReactComponent(true)>]
    static member App() =
        let model, dispatch = React.useElmish (Runtime.init, Runtime.update, [||])

        Html.main [
            prop.className "min-h-screen bg-stone-100 grid place-items-center"
            prop.children [
                Html.h1 [
                    prop.testId "hello-title"
                    prop.className "text-4xl font-bold tracking-tight text-zinc-900"
                    prop.text "Hello ElmishPaint"
                ]
                CanvasView model dispatch
                Html.p [ prop.testId "active-tool"; prop.text $"Active tool: {model.Tool}" ]
                Html.button [
                    prop.testId "select-line"
                    prop.text "Select Line Tool"
                    prop.onClick (fun _ -> dispatch (SelectTool Line))
                ]
            ]
        ]
