module Tests.Tools.Rectangle

open App
open App.Canvas
open Vitest

let private blankCanvas () = BitCanvas.create ()

let private patternWithId id = {
    Id = id
    Name = id
    Tile = Unchecked.defaultof<bool array array>
}

let private countBlackInBounds left top right bottom canvas =
    [
        for y in top..bottom do
            for x in left..right do
                if BitCanvas.getPixel x y canvas = Black then
                    yield 1
    ]
    |> List.length

Vitest.describe (
    "Rectangle",
    fun () ->
        Vitest.test (
            "commitOutline draws a hollow 41x41 rectangle from (10,10) to (50,50)",
            fun () ->
                let canvas =
                    App.Tools.Rectangle.commitOutline { X = 10; Y = 10 } { X = 50; Y = 50 } (blankCanvas ())

                Vitest.expect(BitCanvas.getPixel 10 10 canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 50 10 canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 10 50 canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 50 50 canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 30 30 canvas).toEqual (White)
                Vitest.expect(countBlackInBounds 10 10 50 50 canvas).toBe (160)
        )

        Vitest.test (
            "commitFilled with solid-black fills entire rectangle",
            fun () ->
                let canvas =
                    App.Tools.Rectangle.commitFilled
                        { X = 3; Y = 4 }
                        { X = 5; Y = 6 }
                        (patternWithId "solid-black")
                        (blankCanvas ())

                for y in 4..6 do
                    for x in 3..5 do
                        Vitest.expect(BitCanvas.getPixel x y canvas).toEqual (Black)
        )

        Vitest.test (
            "commitFilled with checkerboard pattern fills using global coordinates",
            fun () ->
                let canvas =
                    App.Tools.Rectangle.commitFilled
                        { X = 0; Y = 0 }
                        { X = 3; Y = 3 }
                        (patternWithId "checkerboard-50")
                        (blankCanvas ())

                for y in 0..3 do
                    for x in 0..3 do
                        let expected = if ((x + y) &&& 1) = 0 then Black else White
                        Vitest.expect(BitCanvas.getPixel x y canvas).toEqual (expected)
        )
)