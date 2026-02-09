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
        Tile = Unchecked.defaultof<bool array2d>
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
                    ToolOptions = { model.ToolOptions with EraserBrushSize = brushSize }
            },
            Cmd.none
        | SelectPattern _ -> model, Cmd.none
        | CanvasMouseDown(position, modifiers) ->
            let strokeCanvas, strokeBit =
                if model.Tool = Pencil then
                    let nextStrokeCanvas, nextStrokeBit = Pencil.beginStroke position model.Canvas
                    Some nextStrokeCanvas, Some nextStrokeBit
                else
                    None, None

            let nextMouse = {
                IsDown = true
                Start = Some position
                Last = Some position
                Current = Some position
                StrokeCanvas = strokeCanvas
                StrokeBit = strokeBit
                Modifiers = modifiers
            }

            { model with Mouse = nextMouse }, Cmd.none
        | CanvasMouseMove(position, modifiers) ->
            let nextStrokeCanvas, nextStrokeBit =
                if model.Mouse.IsDown && model.Tool = Pencil then
                    match model.Mouse.StrokeCanvas, model.Mouse.StrokeBit, model.Mouse.Current with
                    | Some strokeCanvas, Some strokeBit, Some current ->
                        Pencil.drawSegment strokeBit current position strokeCanvas
                        Some strokeCanvas, Some strokeBit
                    | _ -> model.Mouse.StrokeCanvas, model.Mouse.StrokeBit
                else
                    model.Mouse.StrokeCanvas, model.Mouse.StrokeBit

            let nextMouse = {
                model.Mouse with
                    Last = model.Mouse.Current
                    Current = Some position
                    StrokeCanvas = nextStrokeCanvas
                    StrokeBit = nextStrokeBit
                    Modifiers = modifiers
            }

            { model with Mouse = nextMouse }, Cmd.none
        | CanvasMouseUp(position, modifiers) ->
            let committedCanvas, nextHistory =
                if model.Mouse.IsDown && model.Tool = Pencil then
                    match model.Mouse.StrokeCanvas, model.Mouse.StrokeBit with
                    | Some strokeCanvas, Some strokeBit ->
                        match model.Mouse.Current with
                        | Some current -> Pencil.drawSegment strokeBit current position strokeCanvas
                        | None -> ()

                        strokeCanvas, History.push model.Canvas model.History
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
