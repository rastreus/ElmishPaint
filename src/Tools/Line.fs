namespace App.Tools

open App
open App.Canvas

[<RequireQualifiedAccess>]
module Line =
    let private toggledBit bit =
        match bit with
        | White -> Black
        | Black -> White

    let private drawPreviewLine startPoint endPoint previewCanvas =
        for point in Algorithms.bresenhamLine startPoint endPoint do
            let nextBit = BitCanvas.getPixel point.X point.Y previewCanvas |> toggledBit
            BitCanvas.setPixel point.X point.Y nextBit previewCanvas

    let private drawCommittedLine startPoint endPoint committedCanvas =
        for point in Algorithms.bresenhamLine startPoint endPoint do
            BitCanvas.setPixel point.X point.Y Black committedCanvas

    let buildPreview startPoint endPoint canvas : App.BitCanvas =
        let previewCanvas = BitCanvas.clone canvas
        drawPreviewLine startPoint endPoint previewCanvas
        previewCanvas

    let commit startPoint endPoint canvas : App.BitCanvas =
        let committedCanvas = BitCanvas.clone canvas
        drawCommittedLine startPoint endPoint committedCanvas
        committedCanvas