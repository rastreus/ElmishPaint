namespace App.Canvas

open App
open Browser.Dom
open Browser.Types
open Fable.Core.JsInterop

[<RequireQualifiedAccess>]
module ImageExport =
    let private scaleFactor = function
        | Scale1x -> 1
        | Scale2x -> 2
        | Scale4x -> 4

    let private scaleLabel = function
        | Scale1x -> "1x"
        | Scale2x -> "2x"
        | Scale4x -> "4x"

    let private fileName scale = $"elmishpaint-{scaleLabel scale}.png"

    let private toPngDataUrl (canvas: HTMLCanvasElement) : string =
        emitJsExpr canvas "($0.toDataURL ? $0.toDataURL('image/png') : 'data:image/png;base64,')"

    let private triggerDownload name dataUrl =
        let anchor = document.createElement ("a") :?> HTMLAnchorElement
        anchor.href <- dataUrl
        anchor.setAttribute ("download", name)
        anchor.setAttribute ("style", "display:none;")
        document.body.appendChild anchor |> ignore
        anchor.click ()
        document.body.removeChild anchor |> ignore

    let exportPng scale (canvas: App.BitCanvas) =
        let factor = scaleFactor scale
        let imageData = BitCanvas.toImageData factor canvas
        let exportCanvas = document.createElement ("canvas") :?> HTMLCanvasElement
        exportCanvas.width <- int imageData.width
        exportCanvas.height <- int imageData.height

        match exportCanvas.getContext ("2d") with
        | null -> ()
        | rawContext ->
            let context = rawContext :?> CanvasRenderingContext2D
            context.imageSmoothingEnabled <- false
            context.putImageData (imageData, 0.0, 0.0)
            triggerDownload (fileName scale) (toPngDataUrl exportCanvas)
