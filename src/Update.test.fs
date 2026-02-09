module Tests.Update

open App
open App.Canvas
open Elmish
open Vitest

let private noModifiers = {
    Shift = false
    Ctrl = false
    Alt = false
    Meta = false
}

Vitest.describe (
    "Runtime.init",
    fun () ->
        Vitest.test (
            "returns a valid default model and Cmd.none",
            fun () ->
                let model, cmd = Runtime.init ()

                Vitest.expect(model.Tool).toEqual (Pencil)
                Vitest.expect(model.Canvas.Width).toBe (BitCanvas.Width)
                Vitest.expect(model.Canvas.Height).toBe (BitCanvas.Height)
                Vitest.expect(BitCanvas.getPixel 0 0 model.Canvas).toEqual (White)
                Vitest.expect(cmd).toEqual (Cmd.none)
        )
)

Vitest.describe (
    "Runtime.update",
    fun () ->
        Vitest.test (
            "SelectTool changes tool and returns Cmd.none",
            fun () ->
                let model = fst (Runtime.init ())
                let nextModel, cmd = Runtime.update (SelectTool Line) model

                Vitest.expect(nextModel.Tool).toEqual (Line)
                Vitest.expect(nextModel.Canvas).toEqual (model.Canvas)
                Vitest.expect(cmd).toEqual (Cmd.none)
        )

        Vitest.test (
            "same input produces same output",
            fun () ->
                let model = fst (Runtime.init ())
                let result1 = Runtime.update (SelectTool Eraser) model
                let result2 = Runtime.update (SelectTool Eraser) model

                Vitest.expect(result1).toEqual (result2)
        )

        Vitest.test (
            "SetZoom applies supported zoom levels and ignores unsupported values",
            fun () ->
                let model = fst (Runtime.init ())

                let zoomTwoModel, zoomTwoCmd = Runtime.update (SetZoom 2) model
                Vitest.expect(zoomTwoModel.UI.Zoom).toBe (2)
                Vitest.expect(zoomTwoCmd).toEqual (Cmd.none)

                let zoomEightModel, zoomEightCmd = Runtime.update (SetZoom 8) zoomTwoModel
                Vitest.expect(zoomEightModel.UI.Zoom).toBe (8)
                Vitest.expect(zoomEightCmd).toEqual (Cmd.none)

                let invalidZoomModel, invalidZoomCmd = Runtime.update (SetZoom 3) zoomEightModel
                Vitest.expect(invalidZoomModel.UI.Zoom).toBe (8)
                Vitest.expect(invalidZoomCmd).toEqual (Cmd.none)
        )

        Vitest.test (
            "pencil stroke started on white remains preview-only until mouse up then commits black line",
            fun () ->
                let model = fst (Runtime.init ())

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 5; Y = 5 }, noModifiers)) model

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 7; Y = 5 }, noModifiers)) downModel

                Vitest.expect(BitCanvas.getPixel 5 5 moveModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 6 5 moveModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 7 5 moveModel.Canvas).toEqual (White)

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 7; Y = 5 }, noModifiers)) moveModel

                Vitest.expect(BitCanvas.getPixel 5 5 upModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 6 5 upModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 7 5 upModel.Canvas).toEqual (Black)
        )

        Vitest.test (
            "pencil polarity locks for entire stroke started on black",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 10 10 Black model.Canvas

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 10; Y = 10 }, noModifiers)) model

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 12; Y = 10 }, noModifiers)) downModel

                Vitest.expect(BitCanvas.getPixel 12 10 moveModel.Canvas).toEqual (White)

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 12; Y = 10 }, noModifiers)) moveModel

                Vitest.expect(BitCanvas.getPixel 10 10 upModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 11 10 upModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 12 10 upModel.Canvas).toEqual (White)
        )

        Vitest.test (
            "pencil drag uses interpolation for gap-free diagonal lines",
            fun () ->
                let model = fst (Runtime.init ())

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 0; Y = 0 }, noModifiers)) model

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 5; Y = 5 }, noModifiers)) downModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 5; Y = 5 }, noModifiers)) moveModel

                for i in 0..5 do
                    Vitest.expect(BitCanvas.getPixel i i upModel.Canvas).toEqual (Black)
        )
)