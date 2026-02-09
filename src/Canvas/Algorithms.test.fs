module Tests.Canvas.Algorithms

open App
open App.Canvas
open Vitest

Vitest.describe (
    "Algorithms.bresenhamLine",
    fun () ->
        Vitest.test (
            "returns inclusive points for horizontal lines",
            fun () ->
                let points = Algorithms.bresenhamLine { X = 0; Y = 0 } { X = 3; Y = 0 }

                Vitest.expect(points).toEqual ([ { X = 0; Y = 0 }; { X = 1; Y = 0 }; { X = 2; Y = 0 }; { X = 3; Y = 0 } ])
        )

        Vitest.test (
            "returns inclusive points for vertical lines",
            fun () ->
                let points = Algorithms.bresenhamLine { X = 2; Y = 1 } { X = 2; Y = 4 }

                Vitest.expect(points).toEqual ([ { X = 2; Y = 1 }; { X = 2; Y = 2 }; { X = 2; Y = 3 }; { X = 2; Y = 4 } ])
        )

        Vitest.test (
            "returns gap-free points for shallow slopes",
            fun () ->
                let points = Algorithms.bresenhamLine { X = 0; Y = 0 } { X = 3; Y = 1 }

                Vitest.expect(points).toEqual ([ { X = 0; Y = 0 }; { X = 1; Y = 0 }; { X = 2; Y = 1 }; { X = 3; Y = 1 } ])
        )

        Vitest.test (
            "returns gap-free points for steep slopes",
            fun () ->
                let points = Algorithms.bresenhamLine { X = 0; Y = 0 } { X = 1; Y = 3 }

                Vitest.expect(points).toEqual ([ { X = 0; Y = 0 }; { X = 0; Y = 1 }; { X = 1; Y = 2 }; { X = 1; Y = 3 } ])
        )
)
