namespace App.Tools

open App
open App.Canvas

[<RequireQualifiedAccess>]
module Eraser =
    let private brushDimension brushSize =
        match brushSize with
        | Brush1 -> 1
        | Brush2 -> 2
        | Brush4 -> 4
        | Brush8 -> 8

    let private applyBrush point brushSize strokeCanvas =
        let size = brushDimension brushSize

        for dy in 0 .. (size - 1) do
            for dx in 0 .. (size - 1) do
                BitCanvas.setPixel (point.X + dx) (point.Y + dy) White strokeCanvas

    let beginStroke point brushSize canvas : App.BitCanvas =
        let strokeCanvas = BitCanvas.clone canvas
        applyBrush point brushSize strokeCanvas
        strokeCanvas

    let drawSegment startPoint endPoint brushSize strokeCanvas =
        for point in Algorithms.bresenhamLine startPoint endPoint do
            applyBrush point brushSize strokeCanvas