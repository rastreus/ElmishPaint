module Tests.Tools.Marquee

open App
open App.Canvas
open App.Tools
open Vitest

let private createCanvasWithPixels pixels =
    let canvas = BitCanvas.create ()

    for (x, y, bit) in pixels do
        BitCanvas.setPixel x y bit canvas

    canvas

Vitest.describe (
    "Marquee",
    fun () ->
        Vitest.test (
            "lift captures selection pixels and clears source region",
            fun () ->
                let canvas = createCanvasWithPixels [ (1, 1, Black); (2, 2, Black); (4, 4, Black) ]

                let selection, liftedCanvas = Marquee.lift { X = 1; Y = 1 } { X = 2; Y = 2 } canvas

                Vitest.expect(selection.BoundsStart).toEqual ({ X = 1; Y = 1 })
                Vitest.expect(selection.BoundsEnd).toEqual ({ X = 2; Y = 2 })
                Vitest.expect(selection.Offset).toEqual ({ X = 0; Y = 0 })

                Vitest.expect(BitCanvas.getPixel 1 1 liftedCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 2 2 liftedCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 4 4 liftedCanvas).toEqual (Black)

                match selection.FloatingPixels with
                | Some floating ->
                    Vitest.expect(BitCanvas.getPixel 1 1 floating).toEqual (Black)
                    Vitest.expect(BitCanvas.getPixel 2 2 floating).toEqual (Black)
                    Vitest.expect(BitCanvas.getPixel 1 2 floating).toEqual (White)
                | None -> failwith "expected floating selection pixels"
        )

        Vitest.test (
            "move updates selection offset",
            fun () ->
                let canvas = createCanvasWithPixels [ (10, 10, Black) ]
                let selection, _ = Marquee.lift { X = 10; Y = 10 } { X = 10; Y = 10 } canvas
                let movedSelection = Marquee.move { X = 2; Y = -3 } selection

                Vitest.expect(movedSelection.Offset).toEqual ({ X = 2; Y = -3 })
        )

        Vitest.test (
            "compose and stamp place floating pixels at moved position",
            fun () ->
                let canvas = createCanvasWithPixels [ (8, 8, Black) ]
                let selection, liftedCanvas = Marquee.lift { X = 8; Y = 8 } { X = 8; Y = 8 } canvas
                let movedSelection = Marquee.move { X = 3; Y = 1 } selection

                let composedCanvas = Marquee.compose movedSelection liftedCanvas
                let stampedCanvas = Marquee.stamp movedSelection liftedCanvas

                Vitest.expect(BitCanvas.getPixel 8 8 composedCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 11 9 composedCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 8 8 stampedCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 11 9 stampedCanvas).toEqual (Black)
        )

        Vitest.test (
            "stamp clips off-canvas destination pixels",
            fun () ->
                let canvas = createCanvasWithPixels [ (0, 0, Black) ]
                let selection, liftedCanvas = Marquee.lift { X = 0; Y = 0 } { X = 0; Y = 0 } canvas
                let movedSelection = Marquee.move { X = -1; Y = 0 } selection
                let stampedCanvas = Marquee.stamp movedSelection liftedCanvas

                Vitest.expect(BitCanvas.getPixel 0 0 stampedCanvas).toEqual (White)
        )

        Vitest.test (
            "cancel restores original placement regardless of moved offset",
            fun () ->
                let canvas = createCanvasWithPixels [ (20, 20, Black) ]

                let selection, liftedCanvas =
                    Marquee.lift { X = 20; Y = 20 } { X = 20; Y = 20 } canvas

                let movedSelection = Marquee.move { X = 5; Y = 0 } selection
                let cancelledCanvas = Marquee.cancel movedSelection liftedCanvas

                Vitest.expect(BitCanvas.getPixel 20 20 cancelledCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 25 20 cancelledCanvas).toEqual (White)
        )
)