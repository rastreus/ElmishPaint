namespace App.Tools

open System.Collections.Generic
open App
open App.Canvas

[<RequireQualifiedAccess>]
module FloodFill =
    let private inBounds x y =
        x >= 0 && x < BitCanvas.Width && y >= 0 && y < BitCanvas.Height

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

    let fill startPoint pattern canvas : App.BitCanvas =
        let filledCanvas = BitCanvas.clone canvas

        if not (inBounds startPoint.X startPoint.Y) then
            filledCanvas
        else
            let targetBit = BitCanvas.getPixel startPoint.X startPoint.Y canvas
            let stack = Stack<Point>()
            let visited = Array.zeroCreate<bool> (BitCanvas.Width * BitCanvas.Height)

            stack.Push(startPoint)

            while stack.Count > 0 do
                let point = stack.Pop()

                if inBounds point.X point.Y then
                    let index = point.Y * BitCanvas.Width + point.X

                    if not visited[index] then
                        visited[index] <- true

                        if BitCanvas.getPixel point.X point.Y canvas = targetBit then
                            BitCanvas.setPixel point.X point.Y (samplePattern pattern point.X point.Y) filledCanvas
                            stack.Push({ X = point.X + 1; Y = point.Y })
                            stack.Push({ X = point.X - 1; Y = point.Y })
                            stack.Push({ X = point.X; Y = point.Y + 1 })
                            stack.Push({ X = point.X; Y = point.Y - 1 })

            filledCanvas
