module App.Components.CanvasView

open App
open App.Canvas
open Browser.Types
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
    let scaleX = if rect.width > 0.0 then float canvas.width / rect.width else 1.0
    let scaleY = if rect.height > 0.0 then float canvas.height / rect.height else 1.0
    let x = int ((ev.clientX - rect.left) * scaleX) / safeZoom
    let y = int ((ev.clientY - rect.top) * scaleY) / safeZoom

    {
        X = clamp 0 (BitCanvas.Width - 1) x
        Y = clamp 0 (BitCanvas.Height - 1) y
    }

let private drawPixelGrid (canvasContext: CanvasRenderingContext2D) scaledWidth scaledHeight zoom =
    if zoom >= 4 then
        canvasContext.beginPath ()

        for x in zoom .. zoom .. (scaledWidth - 1) do
            let xPos = float x + 0.5
            canvasContext.moveTo (xPos, 0.0)
            canvasContext.lineTo (xPos, float scaledHeight)

        for y in zoom .. zoom .. (scaledHeight - 1) do
            let yPos = float y + 0.5
            canvasContext.moveTo (0.0, yPos)
            canvasContext.lineTo (float scaledWidth, yPos)

        canvasContext.stroke ()

[<ReactComponent>]
let CanvasView (model: Model) (dispatch: Msg -> unit) =
    let canvasRef = React.useRef<HTMLCanvasElement option> (None)
    let zoom = max 1 model.UI.Zoom
    let scaledWidth = BitCanvas.Width * zoom
    let scaledHeight = BitCanvas.Height * zoom

    React.useEffect (fun () ->
        match canvasRef.current with
        | None -> ()
        | Some canvas ->
            match canvas.getContext ("2d") with
            | null -> ()
            | context ->
                let canvasContext = context :?> CanvasRenderingContext2D
                canvasContext.imageSmoothingEnabled <- false
                canvasContext.putImageData (BitCanvas.toImageData zoom model.Canvas, 0.0, 0.0)
                drawPixelGrid canvasContext scaledWidth scaledHeight zoom
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
