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

    let private drawPixelAt point canvas =
        let nextCanvas = BitCanvas.clone canvas
        BitCanvas.setPixel point.X point.Y Black nextCanvas
        nextCanvas

    let private isSupportedZoom zoom =
        zoom = 1 || zoom = 2 || zoom = 4 || zoom = 8

    let update msg model : Model * Cmd<Msg> =
        match msg with
        | SelectTool tool -> { model with Tool = tool }, Cmd.none
        | SelectPattern _ -> model, Cmd.none
        | CanvasMouseDown(position, modifiers) ->
            let nextCanvas =
                if model.Tool = Pencil then
                    drawPixelAt position model.Canvas
                else
                    model.Canvas

            let nextMouse = {
                IsDown = true
                Start = Some position
                Last = Some position
                Current = Some position
                Modifiers = modifiers
            }

            {
                model with
                    Canvas = nextCanvas
                    Mouse = nextMouse
            },
            Cmd.none
        | CanvasMouseMove(position, modifiers) ->
            let nextCanvas =
                if model.Mouse.IsDown && model.Tool = Pencil then
                    drawPixelAt position model.Canvas
                else
                    model.Canvas

            let nextMouse = {
                model.Mouse with
                    Last = model.Mouse.Current
                    Current = Some position
                    Modifiers = modifiers
            }

            {
                model with
                    Canvas = nextCanvas
                    Mouse = nextMouse
            },
            Cmd.none
        | CanvasMouseUp(position, modifiers) ->
            let nextMouse = {
                model.Mouse with
                    IsDown = false
                    Last = model.Mouse.Current
                    Current = Some position
                    Start = None
                    Modifiers = modifiers
            }

            { model with Mouse = nextMouse }, Cmd.none
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