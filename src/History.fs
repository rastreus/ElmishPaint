namespace App

open App.Canvas

[<RequireQualifiedAccess>]
module History =
    let private truncate maxDepth stack =
        if maxDepth <= 0 then
            []
        else
            stack |> List.truncate maxDepth

    let push (previousCanvas: BitCanvas) history = {
        history with
            UndoStack = BitCanvas.clone previousCanvas :: history.UndoStack |> truncate history.MaxDepth
            RedoStack = []
    }

    let undo (currentCanvas: BitCanvas) history : BitCanvas * HistoryState =
        match history.UndoStack with
        | previousCanvas :: remainingUndo ->
            BitCanvas.clone previousCanvas,
            {
                history with
                    UndoStack = remainingUndo
                    RedoStack = BitCanvas.clone currentCanvas :: history.RedoStack |> truncate history.MaxDepth
            }
        | [] -> currentCanvas, history

    let redo (currentCanvas: BitCanvas) history : BitCanvas * HistoryState =
        match history.RedoStack with
        | nextCanvas :: remainingRedo ->
            BitCanvas.clone nextCanvas,
            {
                history with
                    UndoStack = BitCanvas.clone currentCanvas :: history.UndoStack |> truncate history.MaxDepth
                    RedoStack = remainingRedo
            }
        | [] -> currentCanvas, history

    let canUndo history = not history.UndoStack.IsEmpty

    let canRedo history = not history.RedoStack.IsEmpty