module App.Components.CanvasView

open App
open Feliz

[<ReactComponent>]
let CanvasView (_model: Model) (_dispatch: Msg -> unit) =
    Html.canvas [
        prop.testId "paint-canvas"
        prop.width 512
        prop.height 342
    ]
