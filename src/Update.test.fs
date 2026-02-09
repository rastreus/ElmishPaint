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

let private clickStroke x y model =
    let downModel, _ =
        Runtime.update (CanvasMouseDown({ X = x; Y = y }, noModifiers)) model

    let upModel, _ =
        Runtime.update (CanvasMouseUp({ X = x; Y = y }, noModifiers)) downModel

    upModel

let private selectEraser brushSize model =
    let toolModel, _ = Runtime.update (SelectTool Eraser) model
    Runtime.update (SetEraserBrushSize brushSize) toolModel |> fst

let private selectLine model =
    Runtime.update (SelectTool Line) model |> fst

let private countBlackPixels y canvas =
    [ 0 .. (BitCanvas.Width - 1) ]
    |> List.filter (fun x -> BitCanvas.getPixel x y canvas = Black)
    |> List.length

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

        Vitest.test (
            "SetEraserBrushSize updates tool options",
            fun () ->
                let model = fst (Runtime.init ())
                let nextModel, cmd = Runtime.update (SetEraserBrushSize Brush8) model

                Vitest.expect(nextModel.ToolOptions.EraserBrushSize).toEqual (Brush8)
                Vitest.expect(cmd).toEqual (Cmd.none)
        )

        Vitest.test (
            "eraser at Brush1 clears black pixels on click",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 8 8 Black model.Canvas
                let eraserModel = selectEraser Brush1 model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 8; Y = 8 }, noModifiers)) eraserModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 8; Y = 8 }, noModifiers)) downModel

                Vitest.expect(BitCanvas.getPixel 8 8 upModel.Canvas).toEqual (White)
        )

        Vitest.test (
            "eraser at Brush8 clears an 8x8 block on click",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.fill Black model.Canvas
                let eraserModel = selectEraser Brush8 model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 10; Y = 10 }, noModifiers)) eraserModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 10; Y = 10 }, noModifiers)) downModel

                for y in 10..17 do
                    for x in 10..17 do
                        Vitest.expect(BitCanvas.getPixel x y upModel.Canvas).toEqual (White)
        )

        Vitest.test (
            "eraser drag leaves a continuous white trail",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.fill Black model.Canvas
                let eraserModel = selectEraser Brush1 model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 0; Y = 0 }, noModifiers)) eraserModel

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 5; Y = 5 }, noModifiers)) downModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 5; Y = 5 }, noModifiers)) moveModel

                for i in 0..5 do
                    Vitest.expect(BitCanvas.getPixel i i upModel.Canvas).toEqual (White)
        )

        Vitest.test (
            "changing eraser brush size applies on next stroke, not mid-stroke",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.fill Black model.Canvas
                let eraserModel = selectEraser Brush1 model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 20; Y = 20 }, noModifiers)) eraserModel

                let resizedModel, _ = Runtime.update (SetEraserBrushSize Brush8) downModel

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 21; Y = 20 }, noModifiers)) resizedModel

                let firstUpModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 21; Y = 20 }, noModifiers)) moveModel

                Vitest.expect(BitCanvas.getPixel 20 20 firstUpModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 21 20 firstUpModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 27 20 firstUpModel.Canvas).toEqual (Black)

                let nextDownModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 30; Y = 30 }, noModifiers)) firstUpModel

                let nextUpModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 30; Y = 30 }, noModifiers)) nextDownModel

                for y in 30..37 do
                    for x in 30..37 do
                        Vitest.expect(BitCanvas.getPixel x y nextUpModel.Canvas).toEqual (White)
        )

        Vitest.test (
            "eraser stroke commits as a single undo entry",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.fill Black model.Canvas
                let eraserModel = selectEraser Brush4 model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 15; Y = 15 }, noModifiers)) eraserModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 15; Y = 15 }, noModifiers)) downModel

                Vitest.expect(List.length upModel.History.UndoStack).toBe (1)

                let undoneModel, _ = Runtime.update Undo upModel

                for y in 15..18 do
                    for x in 15..18 do
                        Vitest.expect(BitCanvas.getPixel x y undoneModel.Canvas).toEqual (Black)
        )

        Vitest.test (
            "line preview XOR-inverts pixels over white and black regions while dragging",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 1 0 Black model.Canvas
                let lineModel = selectLine model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 0; Y = 0 }, noModifiers)) lineModel

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 2; Y = 0 }, noModifiers)) downModel

                Vitest.expect(BitCanvas.getPixel 0 0 moveModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 1 0 moveModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 2 0 moveModel.Canvas).toEqual (White)

                match moveModel.Mouse.StrokeCanvas with
                | Some previewCanvas ->
                    Vitest.expect(BitCanvas.getPixel 0 0 previewCanvas).toEqual (Black)
                    Vitest.expect(BitCanvas.getPixel 1 0 previewCanvas).toEqual (White)
                    Vitest.expect(BitCanvas.getPixel 2 0 previewCanvas).toEqual (Black)
                | None -> failwith "expected a line preview canvas"
        )

        Vitest.test (
            "line mouse-up commits one undo entry and produces inclusive horizontal span",
            fun () ->
                let model = fst (Runtime.init ())
                let lineModel = selectLine model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 0; Y = 100 }, noModifiers)) lineModel

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 511; Y = 100 }, noModifiers)) downModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 511; Y = 100 }, noModifiers)) moveModel

                Vitest.expect(BitCanvas.getPixel 0 100 upModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 511 100 upModel.Canvas).toEqual (Black)
                Vitest.expect(countBlackPixels 100 upModel.Canvas).toBe (512)
                Vitest.expect(List.length upModel.History.UndoStack).toBe (1)
        )

        Vitest.test (
            "undo restores pre-stroke canvas and redo restores committed stroke",
            fun () ->
                let model = fst (Runtime.init ())

                let strokeModel = clickStroke 4 4 model

                Vitest.expect(BitCanvas.getPixel 4 4 strokeModel.Canvas).toEqual (Black)
                Vitest.expect(List.length strokeModel.History.UndoStack).toBe (1)
                Vitest.expect(List.length strokeModel.History.RedoStack).toBe (0)

                let undoneModel, _ = Runtime.update Undo strokeModel

                Vitest.expect(BitCanvas.getPixel 4 4 undoneModel.Canvas).toEqual (White)
                Vitest.expect(List.length undoneModel.History.UndoStack).toBe (0)
                Vitest.expect(List.length undoneModel.History.RedoStack).toBe (1)

                let redoneModel, _ = Runtime.update Redo undoneModel

                Vitest.expect(BitCanvas.getPixel 4 4 redoneModel.Canvas).toEqual (Black)
                Vitest.expect(List.length redoneModel.History.UndoStack).toBe (1)
                Vitest.expect(List.length redoneModel.History.RedoStack).toBe (0)
        )

        Vitest.test (
            "drawing after undo clears redo stack",
            fun () ->
                let model = fst (Runtime.init ())
                let firstStrokeModel = clickStroke 1 1 model
                let undoneModel, _ = Runtime.update Undo firstStrokeModel

                Vitest.expect(List.length undoneModel.History.RedoStack).toBe (1)

                let secondStrokeModel = clickStroke 2 2 undoneModel
                let redoAttemptModel, _ = Runtime.update Redo secondStrokeModel

                Vitest.expect(List.length secondStrokeModel.History.RedoStack).toBe (0)
                Vitest.expect(redoAttemptModel.Canvas).toEqual (secondStrokeModel.Canvas)
                Vitest.expect(BitCanvas.getPixel 1 1 secondStrokeModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 2 2 secondStrokeModel.Canvas).toEqual (Black)
        )

        Vitest.test (
            "undo and redo are no-ops when history stacks are empty",
            fun () ->
                let model = fst (Runtime.init ())
                let undoModel, undoCmd = Runtime.update Undo model
                let redoModel, redoCmd = Runtime.update Redo model

                Vitest.expect(undoModel).toEqual (model)
                Vitest.expect(redoModel).toEqual (model)
                Vitest.expect(undoCmd).toEqual (Cmd.none)
                Vitest.expect(redoCmd).toEqual (Cmd.none)
        )

        Vitest.test (
            "undo and redo only affect canvas and history state",
            fun () ->
                let model = fst (Runtime.init ())
                let strokeModel = clickStroke 3 3 model
                let toolModel, _ = Runtime.update (SelectTool Line) strokeModel
                let zoomModel, _ = Runtime.update (SetZoom 8) toolModel

                let undoModel, _ = Runtime.update Undo zoomModel
                let redoModel, _ = Runtime.update Redo undoModel

                Vitest.expect(undoModel.Tool).toEqual (Line)
                Vitest.expect(undoModel.UI.Zoom).toBe (8)
                Vitest.expect(BitCanvas.getPixel 3 3 undoModel.Canvas).toEqual (White)

                Vitest.expect(redoModel.Tool).toEqual (Line)
                Vitest.expect(redoModel.UI.Zoom).toBe (8)
                Vitest.expect(BitCanvas.getPixel 3 3 redoModel.Canvas).toEqual (Black)
        )

        Vitest.test (
            "fifty sequential strokes can all be undone",
            fun () ->
                let mutable model = fst (Runtime.init ())

                for x in 0..49 do
                    model <- clickStroke x 0 model

                Vitest.expect(List.length model.History.UndoStack).toBe (50)

                for x in 0..49 do
                    Vitest.expect(BitCanvas.getPixel x 0 model.Canvas).toEqual (Black)

                for _ in 1..50 do
                    let undoneModel, _ = Runtime.update Undo model
                    model <- undoneModel

                for x in 0..49 do
                    Vitest.expect(BitCanvas.getPixel x 0 model.Canvas).toEqual (White)
        )
)
