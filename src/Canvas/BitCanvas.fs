namespace App.Canvas

open App
open Browser
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

    let toImageData scale (canvas: App.BitCanvas) =
        if scale < 1 then
            invalidArg "scale" "Scale must be at least 1."

        let outputWidth = canvas.Width * scale
        let outputHeight = canvas.Height * scale

        let outputData: byte array =
            Microsoft.FSharp.Collections.Array.zeroCreate (outputWidth * outputHeight * 4)

        for y in 0 .. (canvas.Height - 1) do
            for x in 0 .. (canvas.Width - 1) do
                let channel = if getPixel x y canvas = Black then 0uy else 255uy
                let outputY = y * scale
                let outputX = x * scale

                for dy in 0 .. (scale - 1) do
                    for dx in 0 .. (scale - 1) do
                        let baseIndex = (((outputY + dy) * outputWidth) + outputX + dx) * 4
                        outputData[baseIndex] <- channel
                        outputData[baseIndex + 1] <- channel
                        outputData[baseIndex + 2] <- channel
                        outputData[baseIndex + 3] <- 255uy

        Dom.ImageData.Create(outputData, float outputWidth, float outputHeight)

    let fromImageData (imageData: Types.ImageData) =
        let canvas = create ()
        let sourceWidth = int imageData.width
        let sourceHeight = int imageData.height
        let copyWidth = min canvas.Width sourceWidth
        let copyHeight = min canvas.Height sourceHeight
        let pixels = imageData.data

        for y in 0 .. (copyHeight - 1) do
            for x in 0 .. (copyWidth - 1) do
                let baseIndex = ((y * sourceWidth) + x) * 4
                let red = int pixels[baseIndex]
                let green = int pixels[baseIndex + 1]
                let blue = int pixels[baseIndex + 2]
                let alpha = int pixels[baseIndex + 3]
                let luminance = ((red * 299) + (green * 587) + (blue * 114) + 500) / 1000
                let overWhite = ((luminance * alpha) + (255 * (255 - alpha))) / 255
                let bit = if overWhite < 128 then Black else White
                setPixel x y bit canvas

        canvas

    let clear canvas = fill White canvas

    let clone (canvas: App.BitCanvas) : App.BitCanvas = {
        Width = canvas.Width
        Height = canvas.Height
        Data = Constructors.Uint8Array.Create(canvas.Data)
    }
