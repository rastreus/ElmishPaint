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

        Vitest.test (
            "setPixel writes black and white values at addressed coordinates",
            fun () ->
                let canvas = BitCanvas.create ()

                BitCanvas.setPixel 0 0 Black canvas
                BitCanvas.setPixel 511 341 Black canvas
                BitCanvas.setPixel 257 129 Black canvas
                BitCanvas.setPixel 257 129 White canvas

                Vitest.expect(BitCanvas.getPixel 0 0 canvas).toEqual(Black)
                Vitest.expect(BitCanvas.getPixel 511 341 canvas).toEqual(Black)
                Vitest.expect(BitCanvas.getPixel 257 129 canvas).toEqual(White)
        )

        Vitest.test (
            "setPixel out of bounds is a no-op",
            fun () ->
                let canvas = BitCanvas.create ()

                BitCanvas.setPixel 10 10 Black canvas
                BitCanvas.setPixel -1 10 White canvas
                BitCanvas.setPixel 10 -1 White canvas
                BitCanvas.setPixel BitCanvas.Width 10 White canvas
                BitCanvas.setPixel 10 BitCanvas.Height White canvas

                Vitest.expect(BitCanvas.getPixel 10 10 canvas).toEqual(Black)
        )

        Vitest.test (
            "fill and clear update every pixel",
            fun () ->
                let canvas = BitCanvas.create ()

                BitCanvas.fill Black canvas
                Vitest.expect(BitCanvas.getPixel 0 0 canvas).toEqual(Black)
                Vitest.expect(BitCanvas.getPixel 511 341 canvas).toEqual(Black)

                BitCanvas.clear canvas
                Vitest.expect(BitCanvas.getPixel 0 0 canvas).toEqual(White)
                Vitest.expect(BitCanvas.getPixel 511 341 canvas).toEqual(White)
        )

        Vitest.test (
            "clone produces deep copy",
            fun () ->
                let original = BitCanvas.create ()
                BitCanvas.setPixel 20 20 Black original

                let copy = BitCanvas.clone original
                BitCanvas.setPixel 20 20 White copy
                BitCanvas.setPixel 40 40 Black copy

                Vitest.expect(BitCanvas.getPixel 20 20 original).toEqual(Black)
                Vitest.expect(BitCanvas.getPixel 20 20 copy).toEqual(White)
                Vitest.expect(BitCanvas.getPixel 40 40 original).toEqual(White)
                Vitest.expect(BitCanvas.getPixel 40 40 copy).toEqual(Black)
        )
)
