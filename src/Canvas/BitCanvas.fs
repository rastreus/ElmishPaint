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

    let private inBounds x y =
        x >= 0 && x < Width && y >= 0 && y < Height

    let private pixelLocation x y =
        let pixelIndex = (y * Width) + x
        let byteIndex = pixelIndex >>> 3
        let bitMask = byte (1 <<< (pixelIndex &&& 7))
        byteIndex, bitMask

    let create () : App.BitCanvas = {
        Width = Width
        Height = Height
        Data = Constructors.Uint8Array.Create(byteCount)
    }

    let getPixel x y (canvas: App.BitCanvas) =
        if not (inBounds x y) then
            White
        else
            let byteIndex, bitMask = pixelLocation x y

            if (canvas.Data[byteIndex] &&& bitMask) <> 0uy then
                Black
            else
                White

    let setPixel x y bit (canvas: App.BitCanvas) =
        if inBounds x y then
            let byteIndex, bitMask = pixelLocation x y
            let current = canvas.Data[byteIndex]

            canvas.Data[byteIndex] <-
                match bit with
                | Black -> current ||| bitMask
                | White -> current &&& (byte (~~~(int bitMask)))

    let fill bit (canvas: App.BitCanvas) =
        let fillValue = if bit = Black then 255uy else 0uy
        canvas.Data.fill (fillValue) |> ignore

    let clear canvas = fill White canvas

    let clone (canvas: App.BitCanvas) : App.BitCanvas = {
        Width = canvas.Width
        Height = canvas.Height
        Data = Constructors.Uint8Array.Create(canvas.Data)
    }