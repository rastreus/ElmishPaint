module Tests.Tools.Line

open App
open App.Canvas
open Vitest

let private blankCanvas () = BitCanvas.create ()

let private countBlackPixels y canvas =
    [ 0 .. (BitCanvas.Width - 1) ]
    |> List.filter (fun x -> BitCanvas.getPixel x y canvas = Black)
    |> List.length

Vitest.describe (
    "Line",
    fun () ->
        Vitest.test (
            "commit draws inclusive horizontal line with 512 black pixels",
            fun () ->
                let canvas =
                    App.Tools.Line.commit { X = 0; Y = 100 } { X = 511; Y = 100 } (blankCanvas ())

                Vitest.expect(BitCanvas.getPixel 0 100 canvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 511 100 canvas).toEqual (Black)
                Vitest.expect(countBlackPixels 100 canvas).toBe (512)
                Vitest.expect(BitCanvas.getPixel 0 99 canvas).toEqual (White)
        )

        Vitest.test (
            "commit draws gap-free 45-degree line",
            fun () ->
                let canvas =
                    App.Tools.Line.commit { X = 10; Y = 10 } { X = 20; Y = 20 } (blankCanvas ())

                for i in 0..10 do
                    Vitest.expect(BitCanvas.getPixel (10 + i) (10 + i) canvas).toEqual (Black)
        )

        Vitest.test (
            "buildPreview XOR-inverts line pixels over white and black backgrounds",
            fun () ->
                let canvas = blankCanvas ()
                BitCanvas.setPixel 1 0 Black canvas

                let previewCanvas =
                    App.Tools.Line.buildPreview { X = 0; Y = 0 } { X = 2; Y = 0 } canvas

                Vitest.expect(BitCanvas.getPixel 0 0 previewCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 1 0 previewCanvas).toEqual (White)
                Vitest.expect(BitCanvas.getPixel 2 0 previewCanvas).toEqual (Black)
        )
)