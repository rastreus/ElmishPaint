namespace App.Canvas

open App
open Fable.Core.JS

[<RequireQualifiedAccess>]
module BitCanvas =
    [<Literal>]
    let Width = 512

    [<Literal>]
    let Height = 342

    let private pixelCount = Width * Height
    let private byteCount = (pixelCount + 7) / 8

    let private inBounds x y = x >= 0 && x < Width && y >= 0 && y < Height

    let private pixelLocation x y =
        let pixelIndex = (y * Width) + x
        let byteIndex = pixelIndex >>> 3
        let bitMask = byte (1 <<< (pixelIndex &&& 7))
        byteIndex, bitMask

    let create () : App.BitCanvas =
        {
            Width = Width
            Height = Height
            Data = Constructors.Uint8Array.Create(byteCount)
        }

    let getPixel x y (canvas: App.BitCanvas) =
        if not (inBounds x y) then
            White
        else
            let byteIndex, bitMask = pixelLocation x y
            if (canvas.Data[byteIndex] &&& bitMask) <> 0uy then Black else White
