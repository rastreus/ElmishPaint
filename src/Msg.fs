namespace App

open Browser.Types

type Msg =
    | SelectTool of Tool
    | SelectPattern of Pattern
    | CanvasMouseDown of position: Point * modifiers: Modifiers
    | CanvasMouseMove of position: Point * modifiers: Modifiers
    | CanvasMouseUp of position: Point * modifiers: Modifiers
    | Undo
    | Redo
    | ClearSelection
    | MoveSelection of delta: Point
    | StampSelection
    | ImportImage of file: File
    | ImportPreviewReady of preview: ImportPreview
    | ConfirmImport
    | CancelImport
    | ExportPNG of scale: ExportScale
    | SetZoom of zoom: int
    | ScrollCanvas of position: Point
    | KeyDown of key: string * modifiers: Modifiers
