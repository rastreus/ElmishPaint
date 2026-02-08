module Tests.Update

open App
open App.Canvas
open Elmish
open Vitest

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
)
