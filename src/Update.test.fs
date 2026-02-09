module Tests.Update

open App
open App.Canvas
open Browser.Types
open Elmish
open Fable.Core.JsInterop
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

let private selectRectangle model =
    Runtime.update (SelectTool Rectangle) model |> fst

let private selectFilledRectangle model =
    Runtime.update (SelectTool FilledRectangle) model |> fst

let private selectFloodFill model =
    Runtime.update (SelectTool FloodFill) model |> fst

let private selectMarquee model =
    Runtime.update (SelectTool Marquee) model |> fst

let private patternWithId id = Patterns.fromId id

let private selectPattern patternId model =
    Runtime.update (SelectPattern(patternWithId patternId)) model |> fst

let private countBlackPixels y canvas =
    [ 0 .. (BitCanvas.Width - 1) ]
    |> List.filter (fun x -> BitCanvas.getPixel x y canvas = Black)
    |> List.length

let private countAllBlackPixels canvas =
    let mutable count = 0

    for y in 0 .. (BitCanvas.Height - 1) do
        for x in 0 .. (BitCanvas.Width - 1) do
            if BitCanvas.getPixel x y canvas = Black then
                count <- count + 1

    count

let private importPreview fileName thresholdOffset brightness scaledPixels =
    let adjustedPixels =
        scaledPixels
        |> Array.map (fun value ->
            let shifted = value + float brightness

            if shifted < 0.0 then 0.0
            elif shifted > 255.0 then 255.0
            else shifted
        )

    {
        FileName = fileName
        ThresholdOffset = thresholdOffset
        Brightness = brightness
        ScaledPixels = scaledPixels
        PreviewCanvas = Dithering.atkinson adjustedPixels BitCanvas.Width BitCanvas.Height thresholdOffset
    }

let private createFile name mimeType : File =
    emitJsExpr (name, mimeType) "new File(['x'], $0, { type: $1 })"

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
            "SelectPattern changes active pattern and returns Cmd.none",
            fun () ->
                let model = fst (Runtime.init ())
                let nextPattern = patternWithId "checkerboard-50"
                let nextModel, cmd = Runtime.update (SelectPattern nextPattern) model

                Vitest.expect(nextModel.Pattern.Id).toEqual ("checkerboard-50")
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
            "ExportPNG preserves model and emits one command effect",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 12 6 Black model.Canvas

                let exportedModel, exportedCmd = Runtime.update (ExportPNG Scale1x) model

                Vitest.expect(exportedModel).toEqual (model)
                Vitest.expect(List.length exportedCmd).toBe (1)
        )

        Vitest.test (
            "KeyDown maps tool zoom history and export shortcuts",
            fun () ->
                let model = fst (Runtime.init ())
                let primaryModifiers = { noModifiers with Ctrl = true }
                let primaryShiftModifiers = { primaryModifiers with Shift = true }

                let lineModel, _ = Runtime.update (KeyDown("l", noModifiers)) model
                let rectangleModel, _ = Runtime.update (KeyDown("r", noModifiers)) lineModel
                let floodFillModel, _ = Runtime.update (KeyDown("f", noModifiers)) rectangleModel
                let marqueeModel, _ = Runtime.update (KeyDown("m", noModifiers)) floodFillModel
                let eraserModel, _ = Runtime.update (KeyDown("e", noModifiers)) marqueeModel
                let pencilModel, _ = Runtime.update (KeyDown("p", noModifiers)) eraserModel

                Vitest.expect(lineModel.Tool).toEqual (Line)
                Vitest.expect(rectangleModel.Tool).toEqual (Rectangle)
                Vitest.expect(floodFillModel.Tool).toEqual (FloodFill)
                Vitest.expect(marqueeModel.Tool).toEqual (Marquee)
                Vitest.expect(eraserModel.Tool).toEqual (Eraser)
                Vitest.expect(pencilModel.Tool).toEqual (Pencil)

                let zoomOneModel, _ = Runtime.update (SetZoom 8) pencilModel
                let zoomTwoModel, _ = Runtime.update (KeyDown("2", noModifiers)) zoomOneModel
                let zoomThreeModel, _ = Runtime.update (KeyDown("3", noModifiers)) zoomTwoModel
                let zoomFourModel, _ = Runtime.update (KeyDown("4", noModifiers)) zoomThreeModel
                let resetZoomModel, _ = Runtime.update (KeyDown("1", noModifiers)) zoomFourModel

                Vitest.expect(zoomTwoModel.UI.Zoom).toBe (2)
                Vitest.expect(zoomThreeModel.UI.Zoom).toBe (4)
                Vitest.expect(zoomFourModel.UI.Zoom).toBe (8)
                Vitest.expect(resetZoomModel.UI.Zoom).toBe (1)

                let strokeModel = clickStroke 31 10 model
                Vitest.expect(BitCanvas.getPixel 31 10 strokeModel.Canvas).toEqual (Black)

                let undoneModel, _ = Runtime.update (KeyDown("z", primaryModifiers)) strokeModel

                let redoneModel, _ =
                    Runtime.update (KeyDown("z", primaryShiftModifiers)) undoneModel

                Vitest.expect(BitCanvas.getPixel 31 10 undoneModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 31 10 redoneModel.Canvas).toEqual (Black)

                let exportOneShortcutModel, exportOneShortcutCmd =
                    Runtime.update (KeyDown("s", primaryModifiers)) model

                let exportTwoShortcutModel, exportTwoShortcutCmd =
                    Runtime.update (KeyDown("s", primaryShiftModifiers)) model

                let directExportOneModel, _ = Runtime.update (ExportPNG Scale1x) model
                let directExportTwoModel, _ = Runtime.update (ExportPNG Scale2x) model

                Vitest.expect(exportOneShortcutModel).toEqual (directExportOneModel)
                Vitest.expect(exportTwoShortcutModel).toEqual (directExportTwoModel)
                Vitest.expect(List.length exportOneShortcutCmd).toBe (1)
                Vitest.expect(List.length exportTwoShortcutCmd).toBe (1)
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
            "rectangle tool shows drag preview and keeps model canvas unchanged until mouse up",
            fun () ->
                let model = fst (Runtime.init ())
                let rectangleModel = selectRectangle model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 10; Y = 10 }, noModifiers)) rectangleModel

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 12; Y = 12 }, noModifiers)) downModel

                Vitest.expect(BitCanvas.getPixel 10 10 moveModel.Canvas).toEqual (White)

                match moveModel.Mouse.StrokeCanvas with
                | Some previewCanvas ->
                    Vitest.expect(BitCanvas.getPixel 10 10 previewCanvas).toEqual (Black)
                    Vitest.expect(BitCanvas.getPixel 12 12 previewCanvas).toEqual (Black)
                    Vitest.expect(BitCanvas.getPixel 11 11 previewCanvas).toEqual (White)
                | None -> failwith "expected a rectangle preview canvas"
        )

        Vitest.test (
            "outline rectangle commit creates one undo entry",
            fun () ->
                let model = fst (Runtime.init ())
                let rectangleModel = selectRectangle model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 10; Y = 10 }, noModifiers)) rectangleModel

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 12; Y = 12 }, noModifiers)) downModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 12; Y = 12 }, noModifiers)) moveModel

                Vitest.expect(BitCanvas.getPixel 10 10 upModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 12 12 upModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 11 11 upModel.Canvas).toEqual (White)
                Vitest.expect(List.length upModel.History.UndoStack).toBe (1)
        )

        Vitest.test (
            "filled rectangle commits checkerboard fill on mouse up as a single undo entry",
            fun () ->
                let model = fst (Runtime.init ())
                let patternedModel = selectPattern "checkerboard-50" model
                let filledRectangleModel = selectFilledRectangle patternedModel

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 0; Y = 0 }, noModifiers)) filledRectangleModel

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 3; Y = 3 }, noModifiers)) downModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 3; Y = 3 }, noModifiers)) moveModel

                for y in 0..3 do
                    for x in 0..3 do
                        let expected = if ((x + y) &&& 1) = 0 then Black else White
                        Vitest.expect(BitCanvas.getPixel x y upModel.Canvas).toEqual (expected)

                Vitest.expect(List.length upModel.History.UndoStack).toBe (1)
        )

        Vitest.test (
            "flood fill commits on mouse down and adds a single undo entry",
            fun () ->
                let model = fst (Runtime.init ())
                let floodFillModel = selectFloodFill model

                for x in 0..4 do
                    BitCanvas.setPixel x 0 Black floodFillModel.Canvas
                    BitCanvas.setPixel x 4 Black floodFillModel.Canvas

                for y in 0..4 do
                    BitCanvas.setPixel 0 y Black floodFillModel.Canvas
                    BitCanvas.setPixel 4 y Black floodFillModel.Canvas

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 2; Y = 2 }, noModifiers)) floodFillModel

                Vitest.expect(BitCanvas.getPixel 2 2 downModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 1 1 downModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 5 2 downModel.Canvas).toEqual (White)
                Vitest.expect(List.length downModel.History.UndoStack).toBe (1)
                Vitest.expect(downModel.Mouse.IsDown).toBe (false)

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 2; Y = 2 }, noModifiers)) downModel

                Vitest.expect(upModel.Canvas).toEqual (downModel.Canvas)
                Vitest.expect(List.length upModel.History.UndoStack).toBe (1)
        )

        Vitest.test (
            "flood fill uses selected pattern sampling",
            fun () ->
                let model = fst (Runtime.init ())
                let patternedModel = selectPattern "checkerboard-50" model
                let floodFillModel = selectFloodFill patternedModel

                let filledModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 0; Y = 0 }, noModifiers)) floodFillModel

                Vitest.expect(BitCanvas.getPixel 0 0 filledModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 1 0 filledModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 0 1 filledModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 1 1 filledModel.Canvas).toEqual (Black)
        )

        Vitest.test (
            "marquee drag lifts selected pixels and clears source region",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 10 10 Black model.Canvas
                let marqueeModel = selectMarquee model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 10; Y = 10 }, noModifiers)) marqueeModel

                let moveModel, _ =
                    Runtime.update (CanvasMouseMove({ X = 10; Y = 10 }, noModifiers)) downModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 10; Y = 10 }, noModifiers)) moveModel

                Vitest.expect(BitCanvas.getPixel 10 10 upModel.Canvas).toEqual (White)
                Vitest.expect(List.length upModel.History.UndoStack).toBe (1)

                match upModel.Selection with
                | Some selection ->
                    Vitest.expect(selection.Offset).toEqual ({ X = 0; Y = 0 })

                    match selection.FloatingPixels with
                    | Some floatingPixels -> Vitest.expect(BitCanvas.getPixel 10 10 floatingPixels).toEqual (Black)
                    | None -> failwith "expected floating marquee pixels"
                | None -> failwith "expected active marquee selection"
        )

        Vitest.test (
            "MoveSelection shifts marquee offset by one pixel per message",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 12 12 Black model.Canvas
                let marqueeModel = selectMarquee model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 12; Y = 12 }, noModifiers)) marqueeModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 12; Y = 12 }, noModifiers)) downModel

                let movedModel, _ = Runtime.update (MoveSelection { X = 1; Y = 0 }) upModel

                match movedModel.Selection with
                | Some selection -> Vitest.expect(selection.Offset).toEqual ({ X = 1; Y = 0 })
                | None -> failwith "expected moved marquee selection"
        )

        Vitest.test (
            "arrow KeyDown moves marquee selection by one pixel per press",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 14 14 Black model.Canvas
                let marqueeModel = selectMarquee model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 14; Y = 14 }, noModifiers)) marqueeModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 14; Y = 14 }, noModifiers)) downModel

                let rightModel, _ = Runtime.update (KeyDown("ArrowRight", noModifiers)) upModel
                let downKeyModel, _ = Runtime.update (KeyDown("ArrowDown", noModifiers)) rightModel

                match downKeyModel.Selection with
                | Some selection -> Vitest.expect(selection.Offset).toEqual ({ X = 1; Y = 1 })
                | None -> failwith "expected marquee selection after arrow movement"
        )

        Vitest.test (
            "StampSelection merges floating pixels at moved offset and clears active selection",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 16 16 Black model.Canvas
                let marqueeModel = selectMarquee model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 16; Y = 16 }, noModifiers)) marqueeModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 16; Y = 16 }, noModifiers)) downModel

                let movedModel, _ = Runtime.update (MoveSelection { X = 1; Y = 0 }) upModel
                let stampedModel, _ = Runtime.update StampSelection movedModel

                Vitest.expect(stampedModel.Selection).toEqual (None)
                Vitest.expect(BitCanvas.getPixel 16 16 stampedModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 17 16 stampedModel.Canvas).toEqual (Black)
        )

        Vitest.test (
            "clicking outside marquee stamps and clears selection",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 20 20 Black model.Canvas
                let marqueeModel = selectMarquee model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 20; Y = 20 }, noModifiers)) marqueeModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 20; Y = 20 }, noModifiers)) downModel

                let movedModel, _ = Runtime.update (MoveSelection { X = 2; Y = 0 }) upModel

                let stampedByClickModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 100; Y = 100 }, noModifiers)) movedModel

                Vitest.expect(stampedByClickModel.Selection).toEqual (None)
                Vitest.expect(stampedByClickModel.Mouse.IsDown).toBe (false)
                Vitest.expect(BitCanvas.getPixel 22 20 stampedByClickModel.Canvas).toEqual (Black)
        )

        Vitest.test (
            "escape cancels marquee move and stamps at original position",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 24 24 Black model.Canvas
                let marqueeModel = selectMarquee model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 24; Y = 24 }, noModifiers)) marqueeModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 24; Y = 24 }, noModifiers)) downModel

                let movedModel, _ = Runtime.update (MoveSelection { X = 3; Y = 0 }) upModel

                let escapedModel, _ = Runtime.update (KeyDown("Escape", noModifiers)) movedModel

                Vitest.expect(escapedModel.Selection).toEqual (None)
                Vitest.expect(BitCanvas.getPixel 24 24 escapedModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 27 24 escapedModel.Canvas).toEqual (White)
        )

        Vitest.test (
            "delete clears lifted marquee without stamping moved pixels",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 28 28 Black model.Canvas
                let marqueeModel = selectMarquee model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 28; Y = 28 }, noModifiers)) marqueeModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 28; Y = 28 }, noModifiers)) downModel

                let movedModel, _ = Runtime.update (MoveSelection { X = 4; Y = 0 }) upModel
                let clearedModel, _ = Runtime.update (KeyDown("Delete", noModifiers)) movedModel

                Vitest.expect(clearedModel.Selection).toEqual (None)
                Vitest.expect(BitCanvas.getPixel 28 28 clearedModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 32 28 clearedModel.Canvas).toEqual (White)
        )

        Vitest.test (
            "full marquee select move stamp cycle uses one undo entry",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 40 40 Black model.Canvas
                let marqueeModel = selectMarquee model

                let downModel, _ =
                    Runtime.update (CanvasMouseDown({ X = 40; Y = 40 }, noModifiers)) marqueeModel

                let upModel, _ =
                    Runtime.update (CanvasMouseUp({ X = 40; Y = 40 }, noModifiers)) downModel

                let movedModel, _ = Runtime.update (MoveSelection { X = 1; Y = 0 }) upModel
                let stampedModel, _ = Runtime.update StampSelection movedModel

                Vitest.expect(List.length stampedModel.History.UndoStack).toBe (1)
                Vitest.expect(BitCanvas.getPixel 41 40 stampedModel.Canvas).toEqual (Black)

                let undoneModel, _ = Runtime.update Undo stampedModel

                Vitest.expect(BitCanvas.getPixel 40 40 undoneModel.Canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 41 40 undoneModel.Canvas).toEqual (White)
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

        Vitest.test (
            "ImportPreviewReady stores preview and ConfirmImport commits as one undo entry",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 2 2 Black model.Canvas
                let scaledPixels = Array.create (BitCanvas.Width * BitCanvas.Height) 255.0
                let previewCanvas = BitCanvas.create ()
                BitCanvas.setPixel 9 7 Black previewCanvas

                let preview = {
                    importPreview "sample.png" 0 0 scaledPixels with
                        PreviewCanvas = previewCanvas
                }

                let readyModel, _ = Runtime.update (ImportPreviewReady preview) model

                match readyModel.ImportPreview with
                | Some activePreview -> Vitest.expect(activePreview.FileName).toEqual ("sample.png")
                | None -> failwith "expected active import preview"

                let confirmedModel, _ = Runtime.update ConfirmImport readyModel

                Vitest.expect(confirmedModel.ImportPreview).toEqual (None)
                Vitest.expect(BitCanvas.getPixel 9 7 confirmedModel.Canvas).toEqual (Black)
                Vitest.expect(List.length confirmedModel.History.UndoStack).toBe (1)

                let undoneModel, _ = Runtime.update Undo confirmedModel
                Vitest.expect(BitCanvas.getPixel 9 7 undoneModel.Canvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 2 2 undoneModel.Canvas).toEqual (Black)
        )

        Vitest.test (
            "CancelImport clears preview without mutating canvas",
            fun () ->
                let model = fst (Runtime.init ())
                BitCanvas.setPixel 11 11 Black model.Canvas
                let scaledPixels = Array.create (BitCanvas.Width * BitCanvas.Height) 255.0
                let preview = importPreview "cancel.png" 0 0 scaledPixels

                let readyModel, _ = Runtime.update (ImportPreviewReady preview) model
                let cancelledModel, _ = Runtime.update CancelImport readyModel

                Vitest.expect(cancelledModel.ImportPreview).toEqual (None)
                Vitest.expect(BitCanvas.getPixel 11 11 cancelledModel.Canvas).toEqual (Black)
                Vitest.expect(List.length cancelledModel.History.UndoStack).toBe (0)
        )

        Vitest.test (
            "SetImportThreshold and SetImportBrightness visibly change dithered preview",
            fun () ->
                let model = fst (Runtime.init ())

                let scaledPixels =
                    Array.init
                        (BitCanvas.Width * BitCanvas.Height)
                        (fun index -> if (index &&& 1) = 0 then 120.0 else 140.0)

                let preview = importPreview "levels.png" 0 0 scaledPixels
                let readyModel, _ = Runtime.update (ImportPreviewReady preview) model

                let initialCount =
                    match readyModel.ImportPreview with
                    | Some activePreview -> countAllBlackPixels activePreview.PreviewCanvas
                    | None -> failwith "expected initial preview"

                let thresholdModel, _ = Runtime.update (SetImportThreshold 16) readyModel

                let thresholdCount =
                    match thresholdModel.ImportPreview with
                    | Some activePreview ->
                        Vitest.expect(activePreview.ThresholdOffset).toBe (16)
                        countAllBlackPixels activePreview.PreviewCanvas
                    | None -> failwith "expected threshold preview"

                let brightenedModel, _ = Runtime.update (SetImportBrightness 20) thresholdModel

                let brightenedCount =
                    match brightenedModel.ImportPreview with
                    | Some activePreview ->
                        Vitest.expect(activePreview.Brightness).toBe (20)
                        countAllBlackPixels activePreview.PreviewCanvas
                    | None -> failwith "expected brightness preview"

                Vitest.expect(thresholdCount).toBeGreaterThan (initialCount)
                Vitest.expect(brightenedCount).toBeLessThan (thresholdCount)
        )

        Vitest.test (
            "ImportImage ignores unsupported file types",
            fun () ->
                let model = fst (Runtime.init ())
                let unsupportedFile = createFile "notes.txt" "text/plain"
                let nextModel, cmd = Runtime.update (ImportImage unsupportedFile) model

                Vitest.expect(nextModel).toEqual (model)
                Vitest.expect(cmd).toEqual (Cmd.none)
        )
)
