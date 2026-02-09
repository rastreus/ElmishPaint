namespace App

open Elmish
open App.Canvas
open App.Tools

module Runtime =
    let private defaultModifiers = {
        Shift = false
        Ctrl = false
        Alt = false
        Meta = false
    }

    let private defaultPattern = {
        Id = "solid-black"
        Name = "Solid Black"
        Tile = Unchecked.defaultof<bool array array>
    }

    let init () : Model * Cmd<Msg> =
        {
            Canvas = BitCanvas.create ()
            Tool = Pencil
            ToolOptions = {
                EraserBrushSize = Brush1
                RectangleMode = Outline
            }
            Pattern = defaultPattern
            Mouse = {
                IsDown = false
                Start = None
                Last = None
                Current = None
                StrokeCanvas = None
                StrokeBit = None
                StrokeBrushSize = None
                Modifiers = defaultModifiers
            }
            Selection = None
            History = {
                UndoStack = []
                RedoStack = []
                MaxDepth = 50
            }
            UI = {
                Zoom = 1
                Scroll = { X = 0; Y = 0 }
                HoveredPixel = None
                IsBezelMode = false
            }
            ImportPreview = None
        },
        Cmd.none

    let private isSupportedZoom zoom =
        zoom = 1 || zoom = 2 || zoom = 4 || zoom = 8

    let update msg model : Model * Cmd<Msg> =
        match msg with
        | SelectTool tool -> { model with Tool = tool }, Cmd.none
        | SetEraserBrushSize brushSize ->
            {
                model with
                    ToolOptions = {
                        model.ToolOptions with
                            EraserBrushSize = brushSize
                    }
            },
            Cmd.none
        | SelectPattern _ -> model, Cmd.none
        | CanvasMouseDown(position, modifiers) ->
            let strokeCanvas, strokeBit, strokeBrushSize, committedCanvas, nextHistory, isDown =
                match model.Tool with
                | Pencil ->
                    let nextStrokeCanvas, nextStrokeBit = Pencil.beginStroke position model.Canvas
                    Some nextStrokeCanvas, Some nextStrokeBit, None, model.Canvas, model.History, true
                | Eraser ->
                    let brushSize = model.ToolOptions.EraserBrushSize
                    let nextStrokeCanvas = Eraser.beginStroke position brushSize model.Canvas
                    Some nextStrokeCanvas, None, Some brushSize, model.Canvas, model.History, true
                | Line ->
                    let nextStrokeCanvas = Line.buildPreview position position model.Canvas
                    Some nextStrokeCanvas, None, None, model.Canvas, model.History, true
                | Rectangle ->
                    let nextStrokeCanvas = Rectangle.buildOutlinePreview position position model.Canvas
                    Some nextStrokeCanvas, None, None, model.Canvas, model.History, true
                | FilledRectangle ->
                    let nextStrokeCanvas =
                        Rectangle.buildFilledPreview position position model.Pattern model.Canvas

                    Some nextStrokeCanvas, None, None, model.Canvas, model.History, true
                | FloodFill ->
                    let filledCanvas = FloodFill.fill position model.Pattern model.Canvas
                    None, None, None, filledCanvas, History.push model.Canvas model.History, false
                | _ -> None, None, None, model.Canvas, model.History, true

            let nextMouse = {
                IsDown = isDown
                Start = if isDown then Some position else None
                Last = model.Mouse.Current
                Current = Some position
                StrokeCanvas = strokeCanvas
                StrokeBit = strokeBit
                StrokeBrushSize = strokeBrushSize
                Modifiers = modifiers
            }

            {
                model with
                    Canvas = committedCanvas
                    History = nextHistory
                    Mouse = nextMouse
            },
            Cmd.none
        | CanvasMouseMove(position, modifiers) ->
            let nextStrokeCanvas, nextStrokeBit, nextStrokeBrushSize =
                if model.Mouse.IsDown then
                    match model.Tool with
                    | Pencil ->
                        match model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.Current with
                        | Some strokeCanvas, Some strokeBit, Some current ->
                            Pencil.drawSegment strokeBit current position strokeCanvas
                            Some strokeCanvas, Some strokeBit, model.Mouse.StrokeBrushSize
                        | _ -> model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                    | Eraser ->
                        match model.Mouse.StrokeCanvas, model.Mouse.StrokeBrushSize, model.Mouse.Current with
                        | Some strokeCanvas, Some strokeBrushSize, Some current ->
                            Eraser.drawSegment current position strokeBrushSize strokeCanvas
                            Some strokeCanvas, model.Mouse.StrokeBit, Some strokeBrushSize
                        | _ -> model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                    | Line ->
                        match model.Mouse.Start with
                        | Some start ->
                            let previewCanvas = Line.buildPreview start position model.Canvas
                            Some previewCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                        | None -> model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                    | Rectangle ->
                        match model.Mouse.Start with
                        | Some start ->
                            let previewCanvas = Rectangle.buildOutlinePreview start position model.Canvas

                            Some previewCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                        | None -> model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                    | FilledRectangle ->
                        match model.Mouse.Start with
                        | Some start ->
                            let previewCanvas =
                                Rectangle.buildFilledPreview start position model.Pattern model.Canvas

                            Some previewCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                        | None -> model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                    | _ -> model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize
                else
                    model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.StrokeBrushSize

            let nextMouse = {
                model.Mouse with
                    Last = model.Mouse.Current
                    Current = Some position
                    StrokeCanvas = nextStrokeCanvas
                    StrokeBit = nextStrokeBit
                    StrokeBrushSize = nextStrokeBrushSize
                    Modifiers = modifiers
            }

            { model with Mouse = nextMouse }, Cmd.none
        | CanvasMouseUp(position, modifiers) ->
            let committedCanvas, nextHistory =
                if model.Mouse.IsDown then
                    match model.Tool with
                    | Pencil ->
                        match model.Mouse.StrokeCanvas, model.Mouse.StrokeBit with
                        | Some strokeCanvas, Some strokeBit ->
                            match model.Mouse.Current with
                            | Some current -> Pencil.drawSegment strokeBit current position strokeCanvas
                            | None -> ()

                            strokeCanvas, History.push model.Canvas model.History
                        | _ -> model.Canvas, model.History
                    | Eraser ->
                        match model.Mouse.StrokeCanvas, model.Mouse.StrokeBrushSize with
                        | Some strokeCanvas, Some strokeBrushSize ->
                            match model.Mouse.Current with
                            | Some current -> Eraser.drawSegment current position strokeBrushSize strokeCanvas
                            | None -> ()

                            strokeCanvas, History.push model.Canvas model.History
                        | _ -> model.Canvas, model.History
                    | Line ->
                        match model.Mouse.Start with
                        | Some start ->
                            let committedCanvas = Line.commit start position model.Canvas
                            committedCanvas, History.push model.Canvas model.History
                        | None -> model.Canvas, model.History
                    | Rectangle ->
                        match model.Mouse.Start with
                        | Some start ->
                            let committedCanvas = Rectangle.commitOutline start position model.Canvas

                            committedCanvas, History.push model.Canvas model.History
                        | None -> model.Canvas, model.History
                    | FilledRectangle ->
                        match model.Mouse.Start with
                        | Some start ->
                            let committedCanvas =
                                Rectangle.commitFilled start position model.Pattern model.Canvas

                            committedCanvas, History.push model.Canvas model.History
                        | None -> model.Canvas, model.History
                    | _ -> model.Canvas, model.History
                else
                    model.Canvas, model.History

            let nextMouse = {
                model.Mouse with
                    IsDown = false
                    Last = model.Mouse.Current
                    Current = Some position
                    Start = None
                    StrokeCanvas = None
                    StrokeBit = None
                    StrokeBrushSize = None
                    Modifiers = modifiers
            }

            {
                model with
                    Canvas = committedCanvas
                    History = nextHistory
                    Mouse = nextMouse
            },
            Cmd.none
        | Undo ->
            let nextCanvas, nextHistory = History.undo model.Canvas model.History

            {
                model with
                    Canvas = nextCanvas
                    History = nextHistory
            },
            Cmd.none
        | Redo ->
            let nextCanvas, nextHistory = History.redo model.Canvas model.History

            {
                model with
                    Canvas = nextCanvas
                    History = nextHistory
            },
            Cmd.none
        | ClearSelection -> model, Cmd.none
        | MoveSelection _ -> model, Cmd.none
        | StampSelection -> model, Cmd.none
        | ImportImage _ -> model, Cmd.none
        | ImportPreviewReady _ -> model, Cmd.none
        | ConfirmImport -> model, Cmd.none
        | CancelImport -> model, Cmd.none
        | ExportPNG _ -> model, Cmd.none
        | SetZoom zoom ->
            if isSupportedZoom zoom then
                {
                    model with
                        UI = { model.UI with Zoom = zoom }
                },
                Cmd.none
            else
                model, Cmd.none
        | ScrollCanvas _ -> model, Cmd.none
        | KeyDown _ -> model, Cmd.none
