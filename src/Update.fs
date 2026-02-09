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

    let private defaultPattern = Patterns.solidBlack

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

    let rec update msg model : Model * Cmd<Msg> =
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
        | SelectPattern pattern -> { model with Pattern = pattern }, Cmd.none
        | CanvasMouseDown(position, modifiers) ->
            let strokeCanvas, strokeBit, strokeBrushSize, committedCanvas, nextHistory, isDown, nextSelection =
                match model.Tool with
                | Pencil ->
                    let nextStrokeCanvas, nextStrokeBit = Pencil.beginStroke position model.Canvas
                    Some nextStrokeCanvas, Some nextStrokeBit, None, model.Canvas, model.History, true, model.Selection
                | Eraser ->
                    let brushSize = model.ToolOptions.EraserBrushSize
                    let nextStrokeCanvas = Eraser.beginStroke position brushSize model.Canvas
                    Some nextStrokeCanvas, None, Some brushSize, model.Canvas, model.History, true, model.Selection
                | Line ->
                    let nextStrokeCanvas = Line.buildPreview position position model.Canvas
                    Some nextStrokeCanvas, None, None, model.Canvas, model.History, true, model.Selection
                | Rectangle ->
                    let nextStrokeCanvas = Rectangle.buildOutlinePreview position position model.Canvas
                    Some nextStrokeCanvas, None, None, model.Canvas, model.History, true, model.Selection
                | FilledRectangle ->
                    let nextStrokeCanvas =
                        Rectangle.buildFilledPreview position position model.Pattern model.Canvas

                    Some nextStrokeCanvas, None, None, model.Canvas, model.History, true, model.Selection
                | FloodFill ->
                    let filledCanvas = FloodFill.fill position model.Pattern model.Canvas
                    None, None, None, filledCanvas, History.push model.Canvas model.History, false, model.Selection
                | Marquee ->
                    match model.Selection with
                    | Some selection when selection.FloatingPixels.IsSome ->
                        if Marquee.containsPoint position selection then
                            None, None, None, model.Canvas, model.History, false, model.Selection
                        else
                            let stampedCanvas = Marquee.stamp selection model.Canvas
                            None, None, None, stampedCanvas, model.History, false, None
                    | _ -> None, None, None, model.Canvas, model.History, true, model.Selection

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
                    Selection = nextSelection
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
            let committedCanvas, nextHistory, nextSelection =
                if model.Mouse.IsDown then
                    match model.Tool with
                    | Pencil ->
                        match model.Mouse.StrokeCanvas, model.Mouse.StrokeBit with
                        | Some strokeCanvas, Some strokeBit ->
                            match model.Mouse.Current with
                            | Some current -> Pencil.drawSegment strokeBit current position strokeCanvas
                            | None -> ()

                            strokeCanvas, History.push model.Canvas model.History, model.Selection
                        | _ -> model.Canvas, model.History, model.Selection
                    | Eraser ->
                        match model.Mouse.StrokeCanvas, model.Mouse.StrokeBrushSize with
                        | Some strokeCanvas, Some strokeBrushSize ->
                            match model.Mouse.Current with
                            | Some current -> Eraser.drawSegment current position strokeBrushSize strokeCanvas
                            | None -> ()

                            strokeCanvas, History.push model.Canvas model.History, model.Selection
                        | _ -> model.Canvas, model.History, model.Selection
                    | Line ->
                        match model.Mouse.Start with
                        | Some start ->
                            let committedCanvas = Line.commit start position model.Canvas
                            committedCanvas, History.push model.Canvas model.History, model.Selection
                        | None -> model.Canvas, model.History, model.Selection
                    | Rectangle ->
                        match model.Mouse.Start with
                        | Some start ->
                            let committedCanvas = Rectangle.commitOutline start position model.Canvas

                            committedCanvas, History.push model.Canvas model.History, model.Selection
                        | None -> model.Canvas, model.History, model.Selection
                    | FilledRectangle ->
                        match model.Mouse.Start with
                        | Some start ->
                            let committedCanvas =
                                Rectangle.commitFilled start position model.Pattern model.Canvas

                            committedCanvas, History.push model.Canvas model.History, model.Selection
                        | None -> model.Canvas, model.History, model.Selection
                    | FloodFill -> model.Canvas, model.History, model.Selection
                    | Marquee ->
                        match model.Mouse.Start with
                        | Some start ->
                            let nextSelection, liftedCanvas = Marquee.lift start position model.Canvas
                            liftedCanvas, History.push model.Canvas model.History, Some nextSelection
                        | None -> model.Canvas, model.History, model.Selection
                else
                    model.Canvas, model.History, model.Selection

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
                    Selection = nextSelection
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
        | ClearSelection ->
            match model.Selection with
            | Some _ -> { model with Selection = None }, Cmd.none
            | None -> model, Cmd.none
        | MoveSelection delta ->
            match model.Selection with
            | Some selection when selection.FloatingPixels.IsSome ->
                let movedSelection = Marquee.move delta selection

                {
                    model with
                        Selection = Some movedSelection
                },
                Cmd.none
            | _ -> model, Cmd.none
        | StampSelection ->
            match model.Selection with
            | Some selection when selection.FloatingPixels.IsSome ->
                let stampedCanvas = Marquee.stamp selection model.Canvas

                {
                    model with
                        Canvas = stampedCanvas
                        Selection = None
                },
                Cmd.none
            | _ -> model, Cmd.none
        | ImportImage _ -> model, Cmd.none
        | ImportPreviewReady _ -> model, Cmd.none
        | SetImportThreshold _ -> model, Cmd.none
        | SetImportBrightness _ -> model, Cmd.none
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
        | KeyDown(key, modifiers) ->
            let normalizedKey = key.ToLowerInvariant()
            let hasPrimaryModifier = modifiers.Ctrl || modifiers.Meta
            let dispatchShortcut msg = update msg model

            let moveSelection delta =
                match model.Selection with
                | Some selection when selection.FloatingPixels.IsSome ->
                    let movedSelection = Marquee.move delta selection

                    {
                        model with
                            Selection = Some movedSelection
                    },
                    Cmd.none
                | _ -> model, Cmd.none

            let cancelSelection () =
                match model.Selection with
                | Some selection when selection.FloatingPixels.IsSome ->
                    let cancelledCanvas = Marquee.cancel selection model.Canvas

                    {
                        model with
                            Canvas = cancelledCanvas
                            Selection = None
                    },
                    Cmd.none
                | _ -> model, Cmd.none

            if hasPrimaryModifier then
                match normalizedKey, modifiers.Shift with
                | "z", false -> dispatchShortcut Undo
                | "z", true -> dispatchShortcut Redo
                | "s", false -> dispatchShortcut (ExportPNG Scale1x)
                | "s", true -> dispatchShortcut (ExportPNG Scale2x)
                | _ -> model, Cmd.none
            else
                match normalizedKey with
                | "p" -> dispatchShortcut (SelectTool Pencil)
                | "e" -> dispatchShortcut (SelectTool Eraser)
                | "l" -> dispatchShortcut (SelectTool Line)
                | "r" -> dispatchShortcut (SelectTool Rectangle)
                | "f" -> dispatchShortcut (SelectTool FloodFill)
                | "m" -> dispatchShortcut (SelectTool Marquee)
                | "1" -> dispatchShortcut (SetZoom 1)
                | "2" -> dispatchShortcut (SetZoom 2)
                | "3" -> dispatchShortcut (SetZoom 4)
                | "4" -> dispatchShortcut (SetZoom 8)
                | "arrowup" -> moveSelection { X = 0; Y = -1 }
                | "arrowdown" -> moveSelection { X = 0; Y = 1 }
                | "arrowleft" -> moveSelection { X = -1; Y = 0 }
                | "arrowright" -> moveSelection { X = 1; Y = 0 }
                | "escape" -> cancelSelection ()
                | "delete"
                | "backspace" -> dispatchShortcut ClearSelection
                | _ -> model, Cmd.none
