namespace App

open Elmish
open App.Canvas

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

    let update msg model : Model * Cmd<Msg> =
        match msg with
        | SelectTool tool -> { model with Tool = tool }, Cmd.none
        | SelectPattern _ -> model, Cmd.none
        | CanvasMouseDown _ -> model, Cmd.none
        | CanvasMouseMove _ -> model, Cmd.none
        | CanvasMouseUp _ -> model, Cmd.none
        | Undo -> model, Cmd.none
        | Redo -> model, Cmd.none
        | ClearSelection -> model, Cmd.none
        | MoveSelection _ -> model, Cmd.none
        | StampSelection -> model, Cmd.none
        | ImportImage _ -> model, Cmd.none
        | ImportPreviewReady _ -> model, Cmd.none
        | ConfirmImport -> model, Cmd.none
        | CancelImport -> model, Cmd.none
        | ExportPNG _ -> model, Cmd.none
        | SetZoom _ -> model, Cmd.none
        | ScrollCanvas _ -> model, Cmd.none
        | KeyDown _ -> model, Cmd.none