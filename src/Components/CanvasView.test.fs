module Tests.Components.CanvasView

open App
open App.Canvas
open App.Components.CanvasView
open Browser.Types
open Fable.Core.JsInterop
open Vitest

let private defaultModel () = fst (Runtime.init ())

let private modelWithZoom zoom =
    let model = defaultModel ()

    {
        model with
            UI = { model.UI with Zoom = zoom }
    }

let private getStrokeCalls (canvas: HTMLCanvasElement) =
    if isNullOrUndefined canvas?__strokeCalls then
        0
    else
        unbox<int> canvas?__strokeCalls

let private getStrokeRectCalls (canvas: HTMLCanvasElement) =
    if isNullOrUndefined canvas?__strokeRectCalls then
        0
    else
        unbox<int> canvas?__strokeRectCalls

let private getLineDashCalls (canvas: HTMLCanvasElement) =
    if isNullOrUndefined canvas?__lineDashCalls then
        0
    else
        unbox<int> canvas?__lineDashCalls

let private getLineDashOffsets (canvas: HTMLCanvasElement) =
    if isNullOrUndefined canvas?__lineDashOffsets then
        [||]
    else
        unbox<float array> canvas?__lineDashOffsets

let private selectMarquee model =
    Runtime.update (SelectTool Marquee) model |> fst

let private liftSinglePixelSelection x y model =
    let marqueeModel = selectMarquee model

    let modifiers = {
        Shift = false
        Ctrl = false
        Alt = false
        Meta = false
    }

    let downModel, _ =
        Runtime.update (CanvasMouseDown({ X = x; Y = y }, modifiers)) marqueeModel

    Runtime.update (CanvasMouseUp({ X = x; Y = y }, modifiers)) downModel |> fst

Vitest.describe (
    "CanvasView",
    fun () ->
        Vitest.test (
            "renders canvas at 512x342 pixels",
            fun () ->
                let view = RTL.render (CanvasView (defaultModel ()) ignore)
                let canvas = view.getByTestId ("paint-canvas")

                Vitest.expect(canvas).toHaveAttribute ("width", "512")
                Vitest.expect(canvas).toHaveAttribute ("height", "342")
        )

        Vitest.test (
            "renders canvas dimensions scaled by zoom",
            fun () ->
                let view = RTL.render (CanvasView (modelWithZoom 8) ignore)
                let canvas = view.getByTestId ("paint-canvas")

                Vitest.expect(canvas).toHaveAttribute ("width", "4096")
                Vitest.expect(canvas).toHaveAttribute ("height", "2736")
        )

        Vitest.test (
            "renders BitCanvas pixels to 2D canvas context",
            fun () ->
                let model = defaultModel ()

                let nextModel, _ =
                    Runtime.update
                        (CanvasMouseDown(
                            { X = 0; Y = 0 },
                            {
                                Shift = false
                                Ctrl = false
                                Alt = false
                                Meta = false
                            }
                        ))
                        model

                let view = RTL.render (CanvasView nextModel ignore)
                let canvas = view.getByTestId ("paint-canvas") :?> HTMLCanvasElement
                let imageData: ImageData = unbox canvas?__lastImageData
                let putImageDataCalls: int = unbox canvas?__putImageDataCalls

                Vitest.expect(putImageDataCalls).toBeGreaterThanOrEqual (1)
                Vitest.expect(imageData.width).toBe (512.0)
                Vitest.expect(imageData.height).toBe (342.0)
                Vitest.expect(imageData.data[0]).toBe (0uy)
                Vitest.expect(imageData.data[1]).toBe (0uy)
                Vitest.expect(imageData.data[2]).toBe (0uy)
                Vitest.expect(imageData.data[3]).toBe (255uy)
        )

        Vitest.test (
            "dispatches canvas mouse messages with pixel coordinates",
            fun () ->
                let mutable dispatchedMessages: Msg list = []

                let dispatch message =
                    dispatchedMessages <- message :: dispatchedMessages

                let view = RTL.render (CanvasView (defaultModel ()) dispatch)
                let canvas = view.getByTestId ("paint-canvas")

                RTL.fireEvent.custom (
                    "mouseDown",
                    canvas,
                    createObj [ "clientX" ==> 10; "clientY" ==> 20; "shiftKey" ==> true ]
                )

                RTL.fireEvent.custom (
                    "mouseMove",
                    canvas,
                    createObj [ "clientX" ==> 11; "clientY" ==> 21; "ctrlKey" ==> true ]
                )

                RTL.fireEvent.custom (
                    "mouseUp",
                    canvas,
                    createObj [ "clientX" ==> 12; "clientY" ==> 22; "altKey" ==> true ]
                )

                match List.rev dispatchedMessages with
                | [ CanvasMouseDown(downPoint, downMods)
                    CanvasMouseMove(movePoint, moveMods)
                    CanvasMouseUp(upPoint, upMods) ] ->
                    Vitest.expect(downPoint).toEqual ({ X = 10; Y = 20 })
                    Vitest.expect(movePoint).toEqual ({ X = 11; Y = 21 })
                    Vitest.expect(upPoint).toEqual ({ X = 12; Y = 22 })
                    Vitest.expect(downMods.Shift).toBeTruthy ()
                    Vitest.expect(moveMods.Ctrl).toBeTruthy ()
                    Vitest.expect(upMods.Alt).toBeTruthy ()
                | _ -> failwith "expected down, move, and up canvas messages"
        )

        Vitest.test (
            "maps mouse coordinates to pixel coordinates using zoom",
            fun () ->
                let mutable dispatchedMessage: Msg option = None

                let dispatch message = dispatchedMessage <- Some message

                let view = RTL.render (CanvasView (modelWithZoom 4) dispatch)
                let canvas = view.getByTestId ("paint-canvas")

                RTL.fireEvent.custom ("mouseDown", canvas, createObj [ "clientX" ==> 40; "clientY" ==> 84 ])

                match dispatchedMessage with
                | Some(CanvasMouseDown(point, _)) -> Vitest.expect(point).toEqual ({ X = 10; Y = 21 })
                | _ -> failwith "expected a zoom-mapped CanvasMouseDown message"
        )

        Vitest.test (
            "draws pixel grid overlay at zoom 4 and above",
            fun () ->
                let view = RTL.render (CanvasView (modelWithZoom 4) ignore)
                let canvas = view.getByTestId ("paint-canvas") :?> HTMLCanvasElement

                Vitest.expect(getStrokeCalls canvas).toBeGreaterThan (0)
        )

        Vitest.test (
            "does not draw pixel grid overlay below zoom 4",
            fun () ->
                let view = RTL.render (CanvasView (modelWithZoom 2) ignore)
                let canvas = view.getByTestId ("paint-canvas") :?> HTMLCanvasElement

                Vitest.expect(getStrokeCalls canvas).toBe (0)
        )

        Vitest.test (
            "renders moved marquee selection as composed preview pixels",
            fun () ->
                let model = defaultModel ()
                BitCanvas.setPixel 30 12 Black model.Canvas
                let liftedModel = liftSinglePixelSelection 30 12 model
                let movedModel, _ = Runtime.update (MoveSelection { X = 2; Y = 0 }) liftedModel

                let view = RTL.render (CanvasView movedModel ignore)
                let canvas = view.getByTestId ("paint-canvas") :?> HTMLCanvasElement
                let imageData: ImageData = unbox canvas?__lastImageData
                let pixelOffset x y = ((y * 512) + x) * 4

                Vitest.expect(imageData.data[pixelOffset 30 12]).toBe (255uy)
                Vitest.expect(imageData.data[pixelOffset 32 12]).toBe (0uy)
        )

        Vitest.test (
            "draws marching ants rectangle while marquee drag is active",
            fun () ->
                let model = defaultModel ()
                let marqueeModel = selectMarquee model

                let modifiers = {
                    Shift = false
                    Ctrl = false
                    Alt = false
                    Meta = false
                }

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 5; Y = 5 }, modifiers)) marqueeModel

                let dragModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 8; Y = 8 }, modifiers)) downModel

                let view = RTL.render (CanvasView dragModel ignore)
                let canvas = view.getByTestId ("paint-canvas") :?> HTMLCanvasElement

                Vitest.expect(getStrokeRectCalls canvas).toBeGreaterThan (0)
                Vitest.expect(getLineDashCalls canvas).toBeGreaterThan (0)
        )

        Vitest.test (
            "marching ants animation advances dash offset over time",
            fun () -> promise {
                let model = defaultModel ()
                BitCanvas.setPixel 50 50 Black model.Canvas
                let liftedModel = liftSinglePixelSelection 50 50 model

                let view = RTL.render (CanvasView liftedModel ignore)
                let canvas = view.getByTestId ("paint-canvas") :?> HTMLCanvasElement
                let initialOffsets = getLineDashOffsets canvas

                do! RTL.act (fun () -> promise { do! Promise.sleep 260 })

                let animatedOffsets = getLineDashOffsets canvas
                let distinctOffsets = animatedOffsets |> Array.distinct

                Vitest.expect(animatedOffsets.Length).toBeGreaterThan (initialOffsets.Length)
                Vitest.expect(distinctOffsets.Length).toBeGreaterThan (1)
            }
        )
)