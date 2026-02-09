namespace App.Canvas

open App
open Browser.Types

[<RequireQualifiedAccess>]
module Dithering =
    [<Literal>]
    let private BlackPoint = 0.0

    [<Literal>]
    let private WhitePoint = 255.0

    [<Literal>]
    let TargetWidth = BitCanvas.Width

    [<Literal>]
    let TargetHeight = BitCanvas.Height

    let private pixelIndex width x y = (y * width) + x

    let private validateDimensions width height =
        if width <= 0 then
            invalidArg (nameof width) "width must be positive."

        if height <= 0 then
            invalidArg (nameof height) "height must be positive."

    let private validatePixelCount argumentName width height (pixels: float array) =
        if pixels.Length <> (width * height) then
            invalidArg argumentName "pixel count does not match width * height."

    let private clamp minValue maxValue value =
        if value < minValue then minValue
        elif value > maxValue then maxValue
        else value

    let toGrayscale (imageData: ImageData) =
        let width = int imageData.width
        let height = int imageData.height
        validateDimensions width height

        let source = imageData.data
        let grayscale = Array.zeroCreate<float> (width * height)

        for y in 0 .. (height - 1) do
            for x in 0 .. (width - 1) do
                let offset = pixelIndex width x y * 4
                let red = float source[offset]
                let green = float source[offset + 1]
                let blue = float source[offset + 2]
                let alpha = float source[offset + 3] / WhitePoint
                let luminance = (red * 0.299) + (green * 0.587) + (blue * 0.114)
                let composited = (luminance * alpha) + (WhitePoint * (1.0 - alpha))
                grayscale[pixelIndex width x y] <- composited

        grayscale, width, height

    let scaleToFitCanvas (pixels: float array) sourceWidth sourceHeight =
        validateDimensions sourceWidth sourceHeight
        validatePixelCount (nameof pixels) sourceWidth sourceHeight pixels

        let targetPixels = Array.create (TargetWidth * TargetHeight) WhitePoint

        let scale =
            min (float TargetWidth / float sourceWidth) (float TargetHeight / float sourceHeight)

        let scaledWidth = max 1 (int (floor (float sourceWidth * scale)))
        let scaledHeight = max 1 (int (floor (float sourceHeight * scale)))
        let offsetX = (TargetWidth - scaledWidth) / 2
        let offsetY = (TargetHeight - scaledHeight) / 2

        for y in 0 .. (scaledHeight - 1) do
            let sourceY = min (sourceHeight - 1) ((y * sourceHeight) / scaledHeight)
            let targetY = y + offsetY

            for x in 0 .. (scaledWidth - 1) do
                let sourceX = min (sourceWidth - 1) ((x * sourceWidth) / scaledWidth)
                let targetX = x + offsetX
                let sourceValue = pixels[pixelIndex sourceWidth sourceX sourceY]
                targetPixels[pixelIndex TargetWidth targetX targetY] <- sourceValue

        targetPixels

    let atkinson (pixels: float array) width height thresholdOffset =
        validateDimensions width height
        validatePixelCount (nameof pixels) width height pixels

        let working = Array.copy pixels
        let threshold = 128.0 + float thresholdOffset
        let canvas = BitCanvas.create ()

        let inline addError x y delta =
            if x >= 0 && x < width && y >= 0 && y < height then
                let idx = pixelIndex width x y
                working[idx] <- working[idx] + delta

        for y in 0 .. (height - 1) do
            for x in 0 .. (width - 1) do
                let idx = pixelIndex width x y
                let oldPixel = clamp BlackPoint WhitePoint working[idx]

                let nextPixel, outputBit =
                    if oldPixel < threshold then
                        BlackPoint, Black
                    else
                        WhitePoint, White

                BitCanvas.setPixel x y outputBit canvas

                let errorPortion = (oldPixel - nextPixel) / 8.0
                addError (x + 1) y errorPortion
                addError (x + 2) y errorPortion
                addError (x - 1) (y + 1) errorPortion
                addError x (y + 1) errorPortion
                addError (x + 1) (y + 1) errorPortion
                addError x (y + 2) errorPortion

        canvas

    let ditherToCanvas (pixels: float array) sourceWidth sourceHeight thresholdOffset =
        let scaled = scaleToFitCanvas pixels sourceWidth sourceHeight
        atkinson scaled TargetWidth TargetHeight thresholdOffset

    let ditherImageData (imageData: ImageData) thresholdOffset =
        let grayscale, sourceWidth, sourceHeight = toGrayscale imageData
        ditherToCanvas grayscale sourceWidth sourceHeight thresholdOffset