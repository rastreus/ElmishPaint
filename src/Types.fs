namespace App

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

type Pattern =
    { Id: string
      Name: string
      Tile: bool array2d }

type Modifiers =
    { Shift: bool
      Ctrl: bool
      Alt: bool
      Meta: bool }
