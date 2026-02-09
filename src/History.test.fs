module Tests.History

open App
open App.Canvas
open Vitest

let private createHistory maxDepth undoStack redoStack = {
    UndoStack = undoStack
    RedoStack = redoStack
    MaxDepth = maxDepth
}

let private canvasWithBlackPixel x y =
    let canvas = BitCanvas.create ()
    BitCanvas.setPixel x y Black canvas
    canvas

Vitest.describe (
    "History module",
    fun () ->
        Vitest.test (
            "push adds cloned canvas to undo stack, clears redo, and updates canUndo/canRedo",
            fun () ->
                let previousCanvas = canvasWithBlackPixel 1 1

                let history =
                    createHistory 50 [] [ canvasWithBlackPixel 2 2 ]
                    |> History.push previousCanvas

                BitCanvas.setPixel 1 1 White previousCanvas

                match history.UndoStack with
                | [ snapshot ] ->
                    Vitest.expect(BitCanvas.getPixel 1 1 snapshot).toEqual (Black)
                    Vitest.expect(History.canUndo history).toBeTruthy ()
                    Vitest.expect(History.canRedo history).toBeFalsy ()
                | _ -> failwith "expected one snapshot in undo stack"
        )

        Vitest.test (
            "undo restores previous canvas and moves current canvas to redo stack",
            fun () ->
                let currentCanvas = canvasWithBlackPixel 5 5
                let previousCanvas = canvasWithBlackPixel 4 4
                let history = createHistory 50 [ previousCanvas ] []
                let undoneCanvas, nextHistory = History.undo currentCanvas history

                Vitest.expect(BitCanvas.getPixel 4 4 undoneCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 5 5 undoneCanvas).toEqual (White)
                Vitest.expect(nextHistory.UndoStack).toEqual ([])

                match nextHistory.RedoStack with
                | [ redoSnapshot ] ->
                    Vitest.expect(BitCanvas.getPixel 5 5 redoSnapshot).toEqual (Black)
                    Vitest.expect(BitCanvas.getPixel 4 4 redoSnapshot).toEqual (White)
                | _ -> failwith "expected one snapshot in redo stack"
        )

        Vitest.test (
            "redo restores next canvas and moves current canvas to undo stack",
            fun () ->
                let currentCanvas = canvasWithBlackPixel 3 3
                let redoCanvas = canvasWithBlackPixel 7 7
                let history = createHistory 50 [] [ redoCanvas ]
                let redoneCanvas, nextHistory = History.redo currentCanvas history

                Vitest.expect(BitCanvas.getPixel 7 7 redoneCanvas).toEqual (Black)
                Vitest.expect(BitCanvas.getPixel 3 3 redoneCanvas).toEqual (White)
                Vitest.expect(nextHistory.RedoStack).toEqual ([])

                match nextHistory.UndoStack with
                | [ undoSnapshot ] ->
                    Vitest.expect(BitCanvas.getPixel 3 3 undoSnapshot).toEqual (Black)
                    Vitest.expect(BitCanvas.getPixel 7 7 undoSnapshot).toEqual (White)
                | _ -> failwith "expected one snapshot in undo stack"
        )

        Vitest.test (
            "push and transfer operations respect max depth",
            fun () ->
                let history = createHistory 2 [] []
                let canvas1 = canvasWithBlackPixel 0 0
                let canvas2 = canvasWithBlackPixel 1 0
                let canvas3 = canvasWithBlackPixel 2 0

                let pushed =
                    history
                    |> History.push canvas1
                    |> History.push canvas2
                    |> History.push canvas3

                Vitest.expect(List.length pushed.UndoStack).toBe (2)

                let currentCanvas = canvasWithBlackPixel 9 9
                let undoCanvas, afterUndo = History.undo currentCanvas pushed
                let _, afterSecondUndo = History.undo undoCanvas afterUndo
                let _, afterThirdUndo = History.undo undoCanvas afterSecondUndo

                Vitest.expect(List.length afterThirdUndo.RedoStack).toBe (2)
        )

        Vitest.test (
            "undo and redo are no-ops when their stacks are empty",
            fun () ->
                let canvas = canvasWithBlackPixel 8 8
                let history = createHistory 50 [] []
                let undoneCanvas, undoneHistory = History.undo canvas history
                let redoneCanvas, redoneHistory = History.redo canvas history

                Vitest.expect(undoneCanvas).toEqual (canvas)
                Vitest.expect(undoneHistory).toEqual (history)
                Vitest.expect(redoneCanvas).toEqual (canvas)
                Vitest.expect(redoneHistory).toEqual (history)
        )
)
