module Tests.Tools.FloodFill

open App
open App.Canvas
open Vitest

let private blankCanvas () = BitCanvas.create ()

let private patternWithId id = {
    Id = id
    Name = id
    Tile = Unchecked.defaultof<bool array array>
}

let private drawBoxBorder left top right bottom canvas =
    for x in left..right do
        BitCanvas.setPixel x top Black canvas
        BitCanvas.setPixel x bottom Black canvas

    for y in top..bottom do
        BitCanvas.setPixel left y Black canvas
        BitCanvas.setPixel right y Black canvas

Vitest.describe (
    "FloodFill",
    fun () ->
        Vitest.test (
            "fill keeps enclosed boundaries and only fills the target region",
            fun () ->
                let canvas = blankCanvas ()
                drawBoxBorder 0 0 4 4 canvas

                let filledCanvas =
                    App.Tools.FloodFill.fill { X = 2; Y = 2 } (patternWithId "solid-black") canvas

                Vitest.expect(BitCanvas.getPixel 2 2 filledCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 1 1 filledCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 4 2 filledCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 5 2 filledCanvas).toEqual (White)
        )

        Vitest.test (
            "fill applies checkerboard pattern using global coordinates",
            fun () ->
                let canvas = blankCanvas ()
                drawBoxBorder 0 0 5 5 canvas

                let filledCanvas =
                    App.Tools.FloodFill.fill { X = 2; Y = 2 } (patternWithId "checkerboard-50") canvas

                for y in 1..4 do
                    for x in 1..4 do
                        let expected = if ((x + y) &&& 1) = 0 then Black else White
                        Vitest.expect(BitCanvas.getPixel x y filledCanvas).toEqual (expected)

                Vitest.expect(BitCanvas.getPixel 6 6 filledCanvas).toEqual (White)
        )

        Vitest.test (
            "fill handles full blank canvas without crash and fills all pixels",
            fun () ->
                let canvas = blankCanvas ()

                let filledCanvas =
                    App.Tools.FloodFill.fill { X = 0; Y = 0 } (patternWithId "solid-black") canvas

                for y in 0 .. (BitCanvas.Height - 1) do
                    for x in 0 .. (BitCanvas.Width - 1) do
                        Vitest.expect(BitCanvas.getPixel x y filledCanvas).toEqual (Black)
        )

        Vitest.test (
            "fill changes an isolated single pixel",
            fun () ->
                let canvas = blankCanvas ()
                BitCanvas.setPixel 10 10 Black canvas

                let filledCanvas =
                    App.Tools.FloodFill.fill { X = 10; Y = 10 } (patternWithId "solid-white") canvas

                Vitest.expect(BitCanvas.getPixel 10 10 filledCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 9 10 filledCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 10 9 filledCanvas).toEqual (White)
        )
)