module Tests.Components.Toolbar

open App
open App.Canvas
open App.Components.Toolbar
open Fable.Core.JsInterop
open Vitest

let private defaultModel () = fst (Runtime.init ())

let private withTool tool model = { model with Tool = tool }

let private withHistory undoStack redoStack model = {
    model with
        History = {
            model.History with
                UndoStack = undoStack
                RedoStack = redoStack
        }
}

Vitest.describe (
    "Toolbar",
    fun () ->
        Vitest.test (
            "renders all tool buttons and marks active tool",
            fun () ->
                let view = RTL.render (Toolbar (defaultModel ()) ignore)

                let pencilButton = view.getByTestId "toolbar-tool-pencil"
                view.getByTestId "toolbar-tool-eraser" |> ignore
                view.getByTestId "toolbar-tool-line" |> ignore
                view.getByTestId "toolbar-tool-rectangle" |> ignore
                view.getByTestId "toolbar-tool-filled-rectangle" |> ignore
                view.getByTestId "toolbar-tool-flood-fill" |> ignore
                view.getByTestId "toolbar-tool-marquee" |> ignore

                Vitest.expect(pencilButton).toHaveAttribute ("aria-pressed", "true")
        )

        Vitest.test (
            "shows keyboard shortcut tooltips for discoverability",
            fun () ->
                let view = RTL.render (Toolbar (defaultModel ()) ignore)

                Vitest.expect(view.getByTestId ("toolbar-tool-pencil")).toHaveAttribute ("title", "Pencil (P)")
                Vitest.expect(view.getByTestId ("toolbar-tool-eraser")).toHaveAttribute ("title", "Eraser (E)")
                Vitest.expect(view.getByTestId ("toolbar-tool-line")).toHaveAttribute ("title", "Line (L)")
                Vitest.expect(view.getByTestId ("toolbar-tool-rectangle")).toHaveAttribute ("title", "Rect (R)")
                Vitest.expect(view.getByTestId ("toolbar-tool-flood-fill")).toHaveAttribute ("title", "Fill Area (F)")
                Vitest.expect(view.getByTestId ("toolbar-tool-marquee")).toHaveAttribute ("title", "Marquee (M)")

                Vitest.expect(view.getByTestId ("toolbar-zoom-1")).toHaveAttribute ("title", "Zoom 1x (1)")
                Vitest.expect(view.getByTestId ("toolbar-zoom-2")).toHaveAttribute ("title", "Zoom 2x (2)")
                Vitest.expect(view.getByTestId ("toolbar-zoom-4")).toHaveAttribute ("title", "Zoom 4x (3)")
                Vitest.expect(view.getByTestId ("toolbar-zoom-8")).toHaveAttribute ("title", "Zoom 8x (4)")

                Vitest.expect(view.getByTestId ("toolbar-undo")).toHaveAttribute ("title", "Undo (Ctrl/Cmd+Z)")
                Vitest.expect(view.getByTestId ("toolbar-redo")).toHaveAttribute ("title", "Redo (Ctrl/Cmd+Shift+Z)")
                Vitest.expect(view.getByTestId ("toolbar-import-button")).toHaveAttribute ("title", "Import (Ctrl/Cmd+I)")
                Vitest.expect(view.getByTestId ("toolbar-export-1x")).toHaveAttribute ("title", "Export 1x (Ctrl/Cmd+S)")
                Vitest.expect(view.getByTestId ("toolbar-export-2x")).toHaveAttribute ("title", "Export 2x (Ctrl/Cmd+Shift+S)")
        )

        Vitest.test (
            "shows eraser brush controls only when eraser tool is active",
            fun () ->
                let pencilView = RTL.render (Toolbar (defaultModel ()) ignore)
                Vitest.expect(pencilView.queryByTestId ("eraser-brush-controls")).toBeNull ()

                let eraserModel = defaultModel () |> withTool Eraser
                let eraserView = RTL.render (Toolbar eraserModel ignore)
                Vitest.expect(eraserView.getByTestId ("eraser-brush-controls")).toBeInTheDocument ()
        )

        Vitest.test (
            "dispatches SelectTool and SetZoom from tool and zoom buttons",
            fun () ->
                let mutable dispatched: Msg list = []

                let dispatch msg = dispatched <- dispatched @ [ msg ]

                let view = RTL.render (Toolbar (defaultModel ()) dispatch)
                RTL.fireEvent.click (view.getByTestId ("toolbar-tool-line"))
                RTL.fireEvent.click (view.getByTestId ("toolbar-zoom-4"))

                match dispatched with
                | [ SelectTool Line; SetZoom 4 ] -> ()
                | _ -> failwith "expected SelectTool Line and SetZoom 4 messages"
        )

        Vitest.test (
            "disables undo and redo buttons based on history stack state",
            fun () ->
                let initialView = RTL.render (Toolbar (defaultModel ()) ignore)
                Vitest.expect(initialView.getByTestId ("toolbar-undo")).toBeDisabled ()
                Vitest.expect(initialView.getByTestId ("toolbar-redo")).toBeDisabled ()
                initialView.unmount ()

                let undoOnlyModel = defaultModel () |> withHistory [ BitCanvas.create () ] []

                let undoOnlyView = RTL.render (Toolbar undoOnlyModel ignore)
                Vitest.expect(undoOnlyView.getByTestId ("toolbar-undo")).toBeEnabled ()
                Vitest.expect(undoOnlyView.getByTestId ("toolbar-redo")).toBeDisabled ()
                undoOnlyView.unmount ()

                let undoRedoModel =
                    defaultModel () |> withHistory [ BitCanvas.create () ] [ BitCanvas.create () ]

                let undoRedoView = RTL.render (Toolbar undoRedoModel ignore)
                Vitest.expect(undoRedoView.getByTestId ("toolbar-undo")).toBeEnabled ()
                Vitest.expect(undoRedoView.getByTestId ("toolbar-redo")).toBeEnabled ()
        )

        Vitest.test (
            "exposes import and export actions and dispatches export scale",
            fun () ->
                let mutable dispatched: Msg list = []

                let dispatch msg = dispatched <- dispatched @ [ msg ]

                let view = RTL.render (Toolbar (defaultModel ()) dispatch)

                Vitest.expect(view.getByTestId ("toolbar-import-input")).toBeInTheDocument ()
                Vitest.expect(view.getByTestId ("toolbar-export-1x")).toBeInTheDocument ()
                Vitest.expect(view.getByTestId ("toolbar-export-2x")).toBeInTheDocument ()
                Vitest.expect(view.getByTestId ("toolbar-export-4x")).toBeInTheDocument ()

                RTL.fireEvent.click (view.getByTestId ("toolbar-export-2x"))

                match dispatched with
                | [ ExportPNG Scale2x ] -> ()
                | _ -> failwith "expected ExportPNG Scale2x from export button"
        )
)
