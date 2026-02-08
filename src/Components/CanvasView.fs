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

let private toCanvasPoint (canvas: HTMLCanvasElement) (ev: MouseEvent) =
    let rect = canvas.getBoundingClientRect ()
    let x = int (ev.clientX - rect.left)
    let y = int (ev.clientY - rect.top)

    {
        X = clamp 0 (BitCanvas.Width - 1) x
        Y = clamp 0 (BitCanvas.Height - 1) y
    }

[<ReactComponent>]
let CanvasView (model: Model) (dispatch: Msg -> unit) =
    let canvasRef = React.useRef<HTMLCanvasElement option> (None)

    React.useEffect (fun () ->
        match canvasRef.current with
        | None -> ()
        | Some canvas ->
            match canvas.getContext ("2d") with
            | null -> ()
            | context ->
                let canvasContext = context :?> CanvasRenderingContext2D
                canvasContext.imageSmoothingEnabled <- false
                canvasContext.putImageData (BitCanvas.toImageData 1 model.Canvas, 0.0, 0.0)
    )

    let dispatchMouseEvent makeMsg (ev: MouseEvent) =
        match canvasRef.current with
        | None -> ()
        | Some canvas ->
            let point = toCanvasPoint canvas ev
            let modifiers = toModifiers ev
            dispatch (makeMsg (point, modifiers))

    Html.canvas [
        prop.testId "paint-canvas"
        prop.ref canvasRef
        prop.width 512
        prop.height 342
        prop.onMouseDown (dispatchMouseEvent CanvasMouseDown)
        prop.onMouseMove (dispatchMouseEvent CanvasMouseMove)
        prop.onMouseUp (dispatchMouseEvent CanvasMouseUp)
    ]