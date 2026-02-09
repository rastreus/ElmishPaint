module Tests.Tools.Eraser

open App
open App.Canvas
open Vitest

let private blackCanvas () =
    let canvas = BitCanvas.create ()
    BitCanvas.fill Black canvas
    canvas

let private expectSquareCleared topLeftX topLeftY size canvas =
    for y in topLeftY .. (topLeftY + size - 1) do
        for x in topLeftX .. (topLeftX + size - 1) do
            Vitest.expect(BitCanvas.getPixel x y canvas).toEqual (White)

Vitest.describe (
    "Eraser",
    fun () ->
        Vitest.test (
            "beginStroke with Brush1 clears one pixel",
            fun () ->
                let strokeCanvas =
                    App.Tools.Eraser.beginStroke { X = 10; Y = 10 } Brush1 (blackCanvas ())

                Vitest.expect(BitCanvas.getPixel 10 10 strokeCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 11 10 strokeCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 10 11 strokeCanvas).toEqual (Black)
        )

        Vitest.test (
            "beginStroke with Brush2 clears a 2x2 block",
            fun () ->
                let strokeCanvas =
                    App.Tools.Eraser.beginStroke { X = 20; Y = 20 } Brush2 (blackCanvas ())

                expectSquareCleared 20 20 2 strokeCanvas
                Vitest.expect(BitCanvas.getPixel 22 20 strokeCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 20 22 strokeCanvas).toEqual (Black)
        )

        Vitest.test (
            "beginStroke with Brush4 clears a 4x4 block",
            fun () ->
                let strokeCanvas =
                    App.Tools.Eraser.beginStroke { X = 30; Y = 30 } Brush4 (blackCanvas ())

                expectSquareCleared 30 30 4 strokeCanvas
                Vitest.expect(BitCanvas.getPixel 34 30 strokeCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 30 34 strokeCanvas).toEqual (Black)
        )

        Vitest.test (
            "beginStroke with Brush8 clears an 8x8 block",
            fun () ->
                let strokeCanvas =
                    App.Tools.Eraser.beginStroke { X = 40; Y = 40 } Brush8 (blackCanvas ())

                expectSquareCleared 40 40 8 strokeCanvas
                Vitest.expect(BitCanvas.getPixel 48 40 strokeCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 40 48 strokeCanvas).toEqual (Black)
        )

        Vitest.test (
            "drawSegment clears a continuous trail with interpolation",
            fun () ->
                let strokeCanvas =
                    App.Tools.Eraser.beginStroke { X = 0; Y = 0 } Brush1 (blackCanvas ())

                App.Tools.Eraser.drawSegment { X = 0; Y = 0 } { X = 5; Y = 5 } Brush1 strokeCanvas

                for i in 0..5 do
                    Vitest.expect(BitCanvas.getPixel i i strokeCanvas).toEqual (White)
        )
)