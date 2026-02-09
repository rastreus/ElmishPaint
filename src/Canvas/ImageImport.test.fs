module Tests.Canvas.ImageImport

open App
open App.Canvas
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Vitest

let private createFile name mimeType : File =
    emitJsExpr (name, mimeType) "new File(['x'], $0, { type: $1 })"

let private createImageData (pixels: Fable.Core.JS.Uint8ClampedArray) (width: int) (height: int) =
    emitJsExpr (pixels, width, height) "new ImageData($0, $1, $2)"

let private countBlackPixels canvas =
    let mutable blackPixels = 0

    for y in 0 .. (BitCanvas.Height - 1) do
        for x in 0 .. (BitCanvas.Width - 1) do
            if BitCanvas.getPixel x y canvas = Black then
                blackPixels <- blackPixels + 1

    blackPixels

Vitest.describe (
    "ImageImport",
    fun () ->
        Vitest.test (
            "accepts required image formats from MIME or extension",
            fun () ->
                let png = createFile "photo.png" "image/png"
                let jpeg = createFile "photo.jpeg" "image/jpeg"
                let gif = createFile "photo.gif" "image/gif"
                let webp = createFile "photo.webp" "image/webp"
                let extensionFallback = createFile "photo.JPG" ""
                let unsupported = createFile "notes.txt" "text/plain"

                Vitest.expect(ImageImport.isSupportedFile png).toBeTruthy ()
                Vitest.expect(ImageImport.isSupportedFile jpeg).toBeTruthy ()
                Vitest.expect(ImageImport.isSupportedFile gif).toBeTruthy ()
                Vitest.expect(ImageImport.isSupportedFile webp).toBeTruthy ()
                Vitest.expect(ImageImport.isSupportedFile extensionFallback).toBeTruthy ()
                Vitest.expect(ImageImport.isSupportedFile unsupported).toBeFalsy ()
        )

        Vitest.test (
            "fromImageData scales to fit canvas and preserves aspect ratio with letterboxing",
            fun () ->
                let sourceWidth = 4
                let sourceHeight = 2

                let sourcePixels =
                    Fable.Core.JS.Constructors.Uint8ClampedArray.Create(sourceWidth * sourceHeight * 4)

                for i in 0 .. (sourceWidth * sourceHeight - 1) do
                    let offset = i * 4
                    sourcePixels[offset] <- 0uy
                    sourcePixels[offset + 1] <- 0uy
                    sourcePixels[offset + 2] <- 0uy
                    sourcePixels[offset + 3] <- 255uy

                let imageData = createImageData sourcePixels sourceWidth sourceHeight
                let preview = ImageImport.fromImageData "wide.png" imageData

                Vitest.expect(BitCanvas.getPixel 256 171 preview.PreviewCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 256 20 preview.PreviewCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 256 330 preview.PreviewCanvas).toEqual (White)
        )

        Vitest.test (
            "threshold and brightness adjustments change black pixel density",
            fun () ->
                let scaledPixels = [|
                    for index in 0 .. ((BitCanvas.Width * BitCanvas.Height) - 1) do
                        if (index &&& 1) = 0 then 120.0 else 140.0
                |]

                let preview = ImageImport.buildPreview "levels.png" 0 0 scaledPixels
                let thresholdPreview = ImageImport.withThreshold 16 preview
                let brighterPreview = ImageImport.withBrightness 20 thresholdPreview

                let initialBlackPixels = countBlackPixels preview.PreviewCanvas
                let thresholdBlackPixels = countBlackPixels thresholdPreview.PreviewCanvas
                let brighterBlackPixels = countBlackPixels brighterPreview.PreviewCanvas

                Vitest.expect(thresholdBlackPixels).toBeGreaterThan (initialBlackPixels)
                Vitest.expect(brighterBlackPixels).toBeLessThan (thresholdBlackPixels)
        )
)