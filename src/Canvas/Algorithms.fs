namespace App.Canvas

open App

[<RequireQualifiedAccess>]
module Algorithms =
    let bresenhamLine (startPoint: Point) (endPoint: Point) : Point list =
        let dx = abs (endPoint.X - startPoint.X)
        let sx = if startPoint.X < endPoint.X then 1 else -1
        let dy = -(abs (endPoint.Y - startPoint.Y))
        let sy = if startPoint.Y < endPoint.Y then 1 else -1

        let rec loop x y error points =
            let currentPoint = { X = x; Y = y }

            if x = endPoint.X && y = endPoint.Y then
                List.rev (currentPoint :: points)
            else
                let doubledError = 2 * error

                let nextX, errorAfterX = if doubledError >= dy then x + sx, error + dy else x, error

                let nextY, nextError =
                    if doubledError <= dx then
                        y + sy, errorAfterX + dx
                    else
                        y, errorAfterX

                loop nextX nextY nextError (currentPoint :: points)

        loop startPoint.X startPoint.Y (dx + dy) []