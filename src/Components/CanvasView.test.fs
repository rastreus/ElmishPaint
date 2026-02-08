module Tests.Components.CanvasView

open App
open App.Components.CanvasView
open Browser.Types
open Fable.Core.JsInterop
open Vitest

let private defaultModel () = fst (Runtime.init ())

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
)