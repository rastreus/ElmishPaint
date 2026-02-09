namespace App.Tools

open App
open App.Canvas

[<RequireQualifiedAccess>]
module Rectangle =
    let private normalizedBounds startPoint endPoint =
        let left = min startPoint.X endPoint.X
        let right = max startPoint.X endPoint.X
        let top = min startPoint.Y endPoint.Y
        let bottom = max startPoint.Y endPoint.Y
        left, top, right, bottom

    let private samplePattern pattern x y =
        let patternId = pattern.Id.ToLowerInvariant()

        if patternId = "solid-white" then
            White
        elif
            patternId = "checkerboard-50"
            || patternId = "dither-50"
            || patternId.Contains("checker")
        then
            if ((x + y) &&& 1) = 0 then Black else White
        else
            Black

    let private drawOutline startPoint endPoint targetCanvas =
        let left, top, right, bottom = normalizedBounds startPoint endPoint

        for y in top..bottom do
            for x in left..right do
                if x = left || x = right || y = top || y = bottom then
                    BitCanvas.setPixel x y Black targetCanvas

    let private drawFilled startPoint endPoint pattern targetCanvas =
        let left, top, right, bottom = normalizedBounds startPoint endPoint

        for y in top..bottom do
            for x in left..right do
                BitCanvas.setPixel x y (samplePattern pattern x y) targetCanvas

    let buildOutlinePreview startPoint endPoint canvas : App.BitCanvas =
        let previewCanvas = BitCanvas.clone canvas
        drawOutline startPoint endPoint previewCanvas
        previewCanvas

    let commitOutline startPoint endPoint canvas : App.BitCanvas =
        let committedCanvas = BitCanvas.clone canvas
        drawOutline startPoint endPoint committedCanvas
        committedCanvas

    let buildFilledPreview startPoint endPoint pattern canvas : App.BitCanvas =
        let previewCanvas = BitCanvas.clone canvas
        drawFilled startPoint endPoint pattern previewCanvas
        previewCanvas

    let commitFilled startPoint endPoint pattern canvas : App.BitCanvas =
        let committedCanvas = BitCanvas.clone canvas
        drawFilled startPoint endPoint pattern committedCanvas
        committedCanvas