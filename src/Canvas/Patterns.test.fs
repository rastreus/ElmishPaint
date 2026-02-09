module Tests.Canvas.Patterns

open App
open App.Canvas
open Vitest

let private rowToBits (row: bool array) =
    row
    |> Array.map (fun bit -> if bit then '1' else '0')
    |> System.String

let private tileToRows (tile: bool array array) = tile |> Array.map rowToBits

let private expectedTiles = [|
    "solid-black",
    [|
        "11111111"
        "11111111"
        "11111111"
        "11111111"
        "11111111"
        "11111111"
        "11111111"
        "11111111"
    |]
    "solid-white",
    [|
        "00000000"
        "00000000"
        "00000000"
        "00000000"
        "00000000"
        "00000000"
        "00000000"
        "00000000"
    |]
    "checkerboard-50",
    [|
        "10101010"
        "01010101"
        "10101010"
        "01010101"
        "10101010"
        "01010101"
        "10101010"
        "01010101"
    |]
    "dot-25",
    [|
        "10101010"
        "00000000"
        "10101010"
        "00000000"
        "10101010"
        "00000000"
        "10101010"
        "00000000"
    |]
    "dot-75",
    [|
        "01010101"
        "11111111"
        "01010101"
        "11111111"
        "01010101"
        "11111111"
        "01010101"
        "11111111"
    |]
    "horizontal-stripes",
    [|
        "11111111"
        "00000000"
        "11111111"
        "00000000"
        "11111111"
        "00000000"
        "11111111"
        "00000000"
    |]
    "vertical-stripes",
    [|
        "10101010"
        "10101010"
        "10101010"
        "10101010"
        "10101010"
        "10101010"
        "10101010"
        "10101010"
    |]
    "diagonal-stripes-left",
    [|
        "00010001"
        "00100010"
        "01000100"
        "10001000"
        "00010001"
        "00100010"
        "01000100"
        "10001000"
    |]
    "diagonal-stripes-right",
    [|
        "10001000"
        "01000100"
        "00100010"
        "00010001"
        "10001000"
        "01000100"
        "00100010"
        "00010001"
    |]
    "crosshatch",
    [|
        "11111111"
        "10101010"
        "11111111"
        "10101010"
        "11111111"
        "10101010"
        "11111111"
        "10101010"
    |]
    "brick",
    [|
        "11111111"
        "10001000"
        "10001000"
        "11111111"
        "00100010"
        "00100010"
        "11111111"
        "10001000"
    |]
    "polka-dot",
    [|
        "00100100"
        "01111110"
        "11111111"
        "01111110"
        "00100100"
        "00000000"
        "00100100"
        "00011000"
    |]
|]

Vitest.describe (
    "Patterns",
    fun () ->
        Vitest.test (
            "defines 12 built-in 8x8 pattern tiles",
            fun () ->
                Vitest.expect(Patterns.all.Length).toBe (12)

                let ids =
                    Patterns.all
                    |> Array.map (fun pattern -> pattern.Id)
                    |> Set.ofArray

                Vitest.expect(ids.Count).toBe (12)

                for pattern in Patterns.all do
                    Vitest.expect(pattern.Tile.Length).toBe (8)

                    for row in pattern.Tile do
                        Vitest.expect(row.Length).toBe (8)
        )

        Vitest.test (
            "built-in pattern bitmaps match expected 8x8 tiles",
            fun () ->
                for patternId, expectedRows in expectedTiles do
                    let actualRows = patternId |> Patterns.fromId |> fun pattern -> tileToRows pattern.Tile
                    Vitest.expect(actualRows).toEqual (expectedRows)
        )

        Vitest.test (
            "sample wraps any global coordinates to tile boundaries",
            fun () ->
                let checker = Patterns.fromId "checkerboard-50"

                Vitest.expect(Patterns.sample checker 0 0).toBe (true)
                Vitest.expect(Patterns.sample checker 7 0).toBe (false)
                Vitest.expect(Patterns.sample checker 0 7).toBe (false)
                Vitest.expect(Patterns.sample checker 8 0).toBe (true)
                Vitest.expect(Patterns.sample checker 0 8).toBe (true)
                Vitest.expect(Patterns.sample checker -1 0).toBe (false)
                Vitest.expect(Patterns.sample checker 0 -1).toBe (false)
                Vitest.expect(Patterns.sampleBit checker 0 0).toEqual (Black)
                Vitest.expect(Patterns.sampleBit checker 1 0).toEqual (White)
        )

        Vitest.test (
            "sampling is globally aligned so moving a shape shifts sampled content",
            fun () ->
                let checker = Patterns.fromId "checkerboard-50"
                let original = Patterns.sample checker 10 10
                let shiftedRight = Patterns.sample checker 11 10
                let shiftedDown = Patterns.sample checker 10 11

                Vitest.expect(original).toBe (true)
                Vitest.expect(shiftedRight).toBe (false)
                Vitest.expect(shiftedDown).toBe (false)
        )
)
