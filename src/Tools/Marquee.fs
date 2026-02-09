namespace App.Tools

open App
open App.Canvas

[<RequireQualifiedAccess>]
module Marquee =
    let normalizeBounds startPoint endPoint =
        let left = min startPoint.X endPoint.X
        let right = max startPoint.X endPoint.X
        let top = min startPoint.Y endPoint.Y
        let bottom = max startPoint.Y endPoint.Y
        left, top, right, bottom

    let private copySelectionPixels left top right bottom sourceCanvas targetCanvas =
        for y in top..bottom do
            for x in left..right do
                BitCanvas.setPixel x y (BitCanvas.getPixel x y sourceCanvas) targetCanvas

    let private drawSelectionPixels left top right bottom offsetX offsetY floatingPixels targetCanvas =
        for y in top..bottom do
            for x in left..right do
                let targetX = x + offsetX
                let targetY = y + offsetY
                BitCanvas.setPixel targetX targetY (BitCanvas.getPixel x y floatingPixels) targetCanvas

    let private clearBounds left top right bottom targetCanvas =
        for y in top..bottom do
            for x in left..right do
                BitCanvas.setPixel x y White targetCanvas

    let lift startPoint endPoint sourceCanvas : Selection * App.BitCanvas =
        let left, top, right, bottom = normalizeBounds startPoint endPoint
        let floatingPixels = BitCanvas.create ()
        copySelectionPixels left top right bottom sourceCanvas floatingPixels

        let liftedCanvas = BitCanvas.clone sourceCanvas
        clearBounds left top right bottom liftedCanvas

        {
            BoundsStart = { X = left; Y = top }
            BoundsEnd = { X = right; Y = bottom }
            FloatingPixels = Some floatingPixels
            Offset = { X = 0; Y = 0 }
        },
        liftedCanvas

    let move delta selection = {
        selection with
            Offset = {
                X = selection.Offset.X + delta.X
                Y = selection.Offset.Y + delta.Y
            }
    }

    let boundsWithOffset selection =
        let left, top, right, bottom = normalizeBounds selection.BoundsStart selection.BoundsEnd
        left + selection.Offset.X, top + selection.Offset.Y, right + selection.Offset.X, bottom + selection.Offset.Y

    let containsPoint point selection =
        let left, top, right, bottom = boundsWithOffset selection
        point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom

    let compose selection baseCanvas : App.BitCanvas =
        match selection.FloatingPixels with
        | None -> BitCanvas.clone baseCanvas
        | Some floatingPixels ->
            let composedCanvas = BitCanvas.clone baseCanvas
            let left, top, right, bottom = normalizeBounds selection.BoundsStart selection.BoundsEnd

            drawSelectionPixels left top right bottom selection.Offset.X selection.Offset.Y floatingPixels composedCanvas
            composedCanvas

    let stamp selection baseCanvas : App.BitCanvas = compose selection baseCanvas

    let cancel selection baseCanvas : App.BitCanvas =
        let resetSelection = { selection with Offset = { X = 0; Y = 0 } }
        stamp resetSelection baseCanvas
