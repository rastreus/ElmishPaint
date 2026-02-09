module App.Components.CanvasView

open App
open App.Canvas
open App.Tools
open Browser.Dom
open Browser.Types
open Fable.Core
open Feliz

let private toModifiers (ev: MouseEvent) = {
    Shift = ev.shiftKey
    Ctrl = ev.ctrlKey
    Alt = ev.altKey
    Meta = ev.metaKey
}

let private clamp minimum maximum value =
    if value < minimum then minimum
    elif value > maximum then maximum
    else value

let private toCanvasPoint (canvas: HTMLCanvasElement) zoom (ev: MouseEvent) =
    let safeZoom = max 1 zoom
    let rect = canvas.getBoundingClientRect ()

    let scaleX =
        if rect.width > 0.0 then
            float canvas.width / rect.width
        else
            1.0

    let scaleY =
        if rect.height > 0.0 then
            float canvas.height / rect.height
        else
            1.0

    let x = int ((ev.clientX - rect.left) * scaleX) / safeZoom
    let y = int ((ev.clientY - rect.top) * scaleY) / safeZoom

    {
        X = clamp 0 (BitCanvas.Width - 1) x
        Y = clamp 0 (BitCanvas.Height - 1) y
    }

let private drawPixelGrid (canvasContext: CanvasRenderingContext2D) scaledWidth scaledHeight zoom =
    if zoom >= 4 then
        canvasContext.beginPath ()

        for x in zoom..zoom .. (scaledWidth - 1) do
            let xPos = float x + 0.5
            canvasContext.moveTo (xPos, 0.0)
            canvasContext.lineTo (xPos, float scaledHeight)

        for y in zoom..zoom .. (scaledHeight - 1) do
            let yPos = float y + 0.5
            canvasContext.moveTo (0.0, yPos)
            canvasContext.lineTo (float scaledWidth, yPos)

        canvasContext.stroke ()

let private tryGetMarqueeBounds model =
    match model.Selection with
    | Some selection when selection.FloatingPixels.IsSome -> Some(Marquee.boundsWithOffset selection)
    | _ when model.Tool = Marquee && model.Mouse.IsDown ->
        match model.Mouse.Start, model.Mouse.Current with
        | Some startPoint, Some currentPoint -> Some(Marquee.normalizeBounds startPoint currentPoint)
        | _ -> None
    | _ -> None

let private drawMarqueeAnts (canvasContext: CanvasRenderingContext2D) zoom antsPhase (left, top, right, bottom) =
    let clampedLeft = max left 0
    let clampedTop = max top 0
    let clampedRight = min right (BitCanvas.Width - 1)
    let clampedBottom = min bottom (BitCanvas.Height - 1)

    if clampedLeft <= clampedRight && clampedTop <= clampedBottom then
        let drawX = (float (clampedLeft * zoom)) + 0.5
        let drawY = (float (clampedTop * zoom)) + 0.5
        let drawWidth = float ((clampedRight - clampedLeft + 1) * zoom)
        let drawHeight = float ((clampedBottom - clampedTop + 1) * zoom)
        let dashOffset = -float antsPhase

        canvasContext.lineWidth <- 1.0
        canvasContext.setLineDash [| 4.0; 4.0 |]
        canvasContext.strokeStyle <- U3.Case1 "#000000"
        canvasContext.lineDashOffset <- dashOffset
        canvasContext.strokeRect (drawX, drawY, drawWidth, drawHeight)
        canvasContext.strokeStyle <- U3.Case1 "#ffffff"
        canvasContext.lineDashOffset <- dashOffset + 4.0
        canvasContext.strokeRect (drawX, drawY, drawWidth, drawHeight)
        canvasContext.setLineDash [||]

[<ReactComponent>]
let CanvasView (model: Model) (dispatch: Msg -> unit) =
    let canvasRef = React.useRef<HTMLCanvasElement option> (None)
    let antsPhase, setAntsPhase = React.useState (0)
    let zoom = max 1 model.UI.Zoom
    let scaledWidth = BitCanvas.Width * zoom
    let scaledHeight = BitCanvas.Height * zoom
    let marqueeBounds = tryGetMarqueeBounds model

    let activeCanvas =
        match model.Mouse.StrokeCanvas with
        | Some strokeCanvas -> strokeCanvas
        | None ->
            match model.Selection with
            | Some selection when selection.FloatingPixels.IsSome -> Marquee.compose selection model.Canvas
            | _ -> model.Canvas

    React.useEffect (
        (fun () ->
            let intervalId =
                window.setTimeout ((fun () -> setAntsPhase ((antsPhase + 1) % 8)), 120)

            fun () -> window.clearTimeout intervalId
        ),
        [| box antsPhase |]
    )

    React.useEffect (fun () ->
        match canvasRef.current with
        | None -> ()
        | Some canvas ->
            match canvas.getContext ("2d") with
            | null -> ()
            | context ->
                let canvasContext = context :?> CanvasRenderingContext2D
                canvasContext.imageSmoothingEnabled <- false
                canvasContext.putImageData (BitCanvas.toImageData zoom activeCanvas, 0.0, 0.0)
                drawPixelGrid canvasContext scaledWidth scaledHeight zoom
                marqueeBounds |> Option.iter (drawMarqueeAnts canvasContext zoom antsPhase)
    )

    let dispatchMouseEvent makeMsg (ev: MouseEvent) =
        match canvasRef.current with
        | None -> ()
        | Some canvas ->
            let point = toCanvasPoint canvas zoom ev
            let modifiers = toModifiers ev
            dispatch (makeMsg (point, modifiers))

    Html.canvas [
        prop.testId "paint-canvas"
        prop.ref canvasRef
        prop.width scaledWidth
        prop.height scaledHeight
        prop.onMouseDown (dispatchMouseEvent CanvasMouseDown)
        prop.onMouseMove (dispatchMouseEvent CanvasMouseMove)
        prop.onMouseUp (dispatchMouseEvent CanvasMouseUp)
    ]