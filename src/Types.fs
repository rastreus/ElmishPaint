namespace App

open Fable.Core.JS

type Bit =
    | White
    | Black

type Tool =
    | Pencil
    | Eraser
    | Line
    | Rectangle
    | FilledRectangle
    | FloodFill
    | Marquee

type Pattern = {
    Id: string
    Name: string
    Tile: bool array2d
}

type Modifiers = {
    Shift: bool
    Ctrl: bool
    Alt: bool
    Meta: bool
}

type Point = { X: int; Y: int }

type BitCanvas = {
    Width: int
    Height: int
    Data: Uint8Array
}

type BrushSize =
    | Brush1
    | Brush2
    | Brush4
    | Brush8

type RectangleMode =
    | Outline
    | Filled

type ToolOptions = {
    EraserBrushSize: BrushSize
    RectangleMode: RectangleMode
}

type MouseState = {
    IsDown: bool
    Start: Point option
    Last: Point option
    Current: Point option
    Modifiers: Modifiers
}

type Selection = {
    BoundsStart: Point
    BoundsEnd: Point
    FloatingPixels: BitCanvas option
    Offset: Point
}

type HistoryState = {
    UndoStack: BitCanvas list
    RedoStack: BitCanvas list
    MaxDepth: int
}

type UIState = {
    Zoom: int
    Scroll: Point
    HoveredPixel: Point option
    IsBezelMode: bool
}

type ImportPreview = {
    FileName: string
    ThresholdOffset: int
    Brightness: int
    PreviewCanvas: BitCanvas
}

type ExportScale =
    | Scale1x
    | Scale2x
    | Scale4x