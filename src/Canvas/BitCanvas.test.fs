module Tests.Canvas.BitCanvas

open App
open App.Canvas
open Vitest

Vitest.describe (
    "BitCanvas create/getPixel",
    fun () ->
        Vitest.test (
            "create initializes a 512x342 white canvas",
            fun () ->
                let canvas = BitCanvas.create ()
                let expectedByteCount = ((BitCanvas.Width * BitCanvas.Height) + 7) / 8

                Vitest.expect(canvas.Width).toBe(BitCanvas.Width)
                Vitest.expect(canvas.Height).toBe(BitCanvas.Height)
                Vitest.expect(canvas.Data.length).toBe(expectedByteCount)

                let mutable blackPixels = 0

                for y in 0 .. (BitCanvas.Height - 1) do
                    for x in 0 .. (BitCanvas.Width - 1) do
                        if BitCanvas.getPixel x y canvas = Black then
                            blackPixels <- blackPixels + 1

                Vitest.expect(blackPixels).toBe(0)
        )

        Vitest.test (
            "getPixel returns white when coordinates are out of bounds",
            fun () ->
                let canvas = BitCanvas.create ()

                Vitest.expect(BitCanvas.getPixel -1 0 canvas).toEqual(White)
                Vitest.expect(BitCanvas.getPixel 0 -1 canvas).toEqual(White)
                Vitest.expect(BitCanvas.getPixel BitCanvas.Width 0 canvas).toEqual(White)
                Vitest.expect(BitCanvas.getPixel 0 BitCanvas.Height canvas).toEqual(White)
        )
)
