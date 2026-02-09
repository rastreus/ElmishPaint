module Tests.Canvas.Dithering

open App
open App.Canvas
open Browser
open Fable.Core.JsInterop
open Vitest

let private createImageData (pixels: Fable.Core.JS.Uint8ClampedArray) (width: int) (height: int) =
    emitJsExpr (pixels, width, height) "new ImageData($0, $1, $2)"

let private regionToBits width height (canvas: App.BitCanvas) =
    [|
        for y in 0 .. (height - 1) ->
            [|
                for x in 0 .. (width - 1) do
                    BitCanvas.getPixel x y canvas
            |]
    |]

let private countBlackPixels width height (canvas: App.BitCanvas) =
    let mutable count = 0

    for y in 0 .. (height - 1) do
        for x in 0 .. (width - 1) do
            if BitCanvas.getPixel x y canvas = Black then
                count <- count + 1

    count

let private floydSteinbergBits (grayscale: float array) width height thresholdOffset =
    let working = Array.copy grayscale
    let output = Array.create (width * height) White
    let threshold = 128.0 + float thresholdOffset

    let inline index x y = (y * width) + x

    let inline diffuse x y weight quantizationError =
        if x >= 0 && x < width && y >= 0 && y < height then
            let pixelIndex = index x y
            working[pixelIndex] <- working[pixelIndex] + (quantizationError * weight)

    for y in 0 .. (height - 1) do
        for x in 0 .. (width - 1) do
            let pixelIndex = index x y
            let oldPixel = working[pixelIndex]
            let nextPixel, outputBit = if oldPixel < threshold then 0.0, Black else 255.0, White

            output[pixelIndex] <- outputBit

            let quantizationError = oldPixel - nextPixel
            diffuse (x + 1) y (7.0 / 16.0) quantizationError
            diffuse (x - 1) (y + 1) (3.0 / 16.0) quantizationError
            diffuse x (y + 1) (5.0 / 16.0) quantizationError
            diffuse (x + 1) (y + 1) (1.0 / 16.0) quantizationError

    output

Vitest.describe (
    "Dithering",
    fun () ->
        Vitest.test (
            "atkinson matches known 3x3 output and differs from floyd-steinberg",
            fun () ->
                let grayscale = [| 64.0; 160.0; 160.0; 160.0; 127.0; 96.0; 0.0; 64.0; 96.0 |]

                let canvas = Dithering.atkinson grayscale 3 3 0
                let actualBits = regionToBits 3 3 canvas

                let expectedAtkinson = [|
                    [| Black; White; White |]
                    [| White; Black; Black |]
                    [| Black; Black; Black |]
                |]

                Vitest.expect(actualBits).toEqual (expectedAtkinson)

                let floydBits = floydSteinbergBits grayscale 3 3 0
                Vitest.expect(floydBits[(2 * 3) + 2]).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 2 2 canvas).toEqual (Black)
        )

        Vitest.test (
            "threshold offset changes resulting black pixel count",
            fun () ->
                let grayscale = [|
                    120.0
                    124.0
                    128.0
                    132.0
                    136.0
                    140.0
                    144.0
                    148.0
                    152.0
                    156.0
                    160.0
                    164.0
                |]

                let lowOffset = Dithering.atkinson grayscale 12 1 -20
                let highOffset = Dithering.atkinson grayscale 12 1 20
                let lowOffsetBlackPixels = countBlackPixels 12 1 lowOffset
                let highOffsetBlackPixels = countBlackPixels 12 1 highOffset

                Vitest.expect(highOffsetBlackPixels).toBeGreaterThan (lowOffsetBlackPixels)
        )

        Vitest.test (
            "atkinson always returns a valid bitcanvas",
            fun () ->
                let canvas = Dithering.atkinson [| 0.0; 255.0; 128.0; 64.0 |] 2 2 0
                let expectedByteCount = ((BitCanvas.Width * BitCanvas.Height) + 7) / 8

                Vitest.expect(canvas.Width).toBe (BitCanvas.Width)
                Vitest.expect(canvas.Height).toBe (BitCanvas.Height)
                Vitest.expect(canvas.Data.length).toBe (expectedByteCount)
        )

        Vitest.test (
            "toGrayscale converts RGBA pixels using luminance and alpha-over-white",
            fun () ->
                let pixels =
                    Fable.Core.JS.Constructors.Uint8ClampedArray.Create [|
                        255uy
                        0uy
                        0uy
                        255uy
                        0uy
                        0uy
                        0uy
                        128uy
                    |]

                let imageData = createImageData pixels 2 1
                let grayscale, width, height = Dithering.toGrayscale imageData

                Vitest.expect(width).toBe (2)
                Vitest.expect(height).toBe (1)
                Vitest.expect(grayscale.Length).toBe (2)
                Vitest.expect(grayscale[0]).toBeGreaterThan (76.0)
                Vitest.expect(grayscale[0]).toBeLessThan (77.0)
                Vitest.expect(grayscale[1]).toBeGreaterThan (126.0)
                Vitest.expect(grayscale[1]).toBeLessThan (128.5)
        )

        Vitest.test (
            "scaleToFitCanvas scales with preserved aspect ratio and vertical letterboxing",
            fun () ->
                let source = [| 0.0; 255.0 |]
                let scaled = Dithering.scaleToFitCanvas source 2 1

                let pixel x y = scaled[(y * BitCanvas.Width) + x]

                Vitest.expect(scaled.Length).toBe (BitCanvas.Width * BitCanvas.Height)
                Vitest.expect(pixel 0 0).toBe (255.0)
                Vitest.expect(pixel 0 42).toBe (255.0)
                Vitest.expect(pixel 0 43).toBe (0.0)
                Vitest.expect(pixel 255 43).toBe (0.0)
                Vitest.expect(pixel 256 43).toBe (255.0)
                Vitest.expect(pixel 511 43).toBe (255.0)
                Vitest.expect(pixel 0 299).toBe (255.0)
        )
)
