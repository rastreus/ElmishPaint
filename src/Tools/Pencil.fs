namespace App.Tools

open App
open App.Canvas

[<RequireQualifiedAccess>]
module Pencil =
    let private toggledBit bit =
        match bit with
        | White -> Black
        | Black -> White

    let beginStroke point canvas : App.BitCanvas * Bit =
        let strokeBit = BitCanvas.getPixel point.X point.Y canvas |> toggledBit
        let strokeCanvas = BitCanvas.clone canvas
        BitCanvas.setPixel point.X point.Y strokeBit strokeCanvas
        strokeCanvas, strokeBit

    let drawSegment strokeBit startPoint endPoint strokeCanvas =
        for point in Algorithms.bresenhamLine startPoint endPoint do
            BitCanvas.setPixel point.X point.Y strokeBit strokeCanvas
