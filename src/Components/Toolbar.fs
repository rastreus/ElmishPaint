module App.Components.Toolbar

open App
open Browser.Types
open Feliz

let private toolButtonClass isActive =
    if isActive then
        "rounded border border-zinc-900 bg-zinc-900 px-2 py-1 text-xs font-semibold uppercase tracking-wide text-white"
    else
        "rounded border border-zinc-400 bg-white px-2 py-1 text-xs font-semibold uppercase tracking-wide text-zinc-900"

let private optionButtonClass isActive =
    if isActive then
        "rounded border border-zinc-900 bg-zinc-900 px-2 py-1 text-xs font-semibold text-white"
    else
        "rounded border border-zinc-400 bg-white px-2 py-1 text-xs font-semibold text-zinc-900"

let private dispatchImport (dispatch: Msg -> unit) (ev: Event) =
    let target = ev.target :?> HTMLInputElement

    if not (isNull target.files) then
        let selectedFile = target.files.item (0)

        if not (isNull selectedFile) then
            dispatch (ImportImage selectedFile)

[<ReactComponent>]
let Toolbar (model: Model) (dispatch: Msg -> unit) =
    let fileInputRef = React.useRef<HTMLInputElement option> (None)
    let canUndo = History.canUndo model.History
    let canRedo = History.canRedo model.History

    Html.section [
        prop.testId "toolbar-root"
        prop.className "flex flex-wrap items-center gap-2 rounded border border-zinc-300 bg-zinc-50 p-2"
        prop.children [
            Html.div [
                prop.className "flex flex-wrap items-center gap-1"
                prop.children [
                    for tool, label, testId in
                        [|
                            Pencil, "Pencil", "toolbar-tool-pencil"
                            Eraser, "Eraser", "toolbar-tool-eraser"
                            Line, "Line", "toolbar-tool-line"
                            Rectangle, "Rect", "toolbar-tool-rectangle"
                            FilledRectangle, "Fill", "toolbar-tool-filled-rectangle"
                            FloodFill, "Fill Area", "toolbar-tool-flood-fill"
                            Marquee, "Marquee", "toolbar-tool-marquee"
                        |] do
                        let isActive = model.Tool = tool

                        Html.button [
                            prop.key testId
                            prop.testId testId
                            prop.type'.button
                            prop.custom ("aria-pressed", if isActive then "true" else "false")
                            prop.className (toolButtonClass isActive)
                            prop.text label
                            prop.onClick (fun _ -> dispatch (SelectTool tool))
                        ]
                ]
            ]
            if model.Tool = Eraser then
                Html.div [
                    prop.testId "eraser-brush-controls"
                    prop.className "flex items-center gap-1"
                    prop.children [
                        Html.span [ prop.className "text-xs font-medium text-zinc-700"; prop.text "Eraser" ]
                        for brushSize, label, testId in
                            [|
                                Brush1, "1x1", "eraser-brush-1"
                                Brush2, "2x2", "eraser-brush-2"
                                Brush4, "4x4", "eraser-brush-4"
                                Brush8, "8x8", "eraser-brush-8"
                            |] do
                            let isActive = model.ToolOptions.EraserBrushSize = brushSize

                            Html.button [
                                prop.key testId
                                prop.testId testId
                                prop.type'.button
                                prop.className (optionButtonClass isActive)
                                prop.text label
                                prop.onClick (fun _ -> dispatch (SetEraserBrushSize brushSize))
                            ]
                    ]
                ]
            Html.div [
                prop.className "flex items-center gap-1"
                prop.children [
                    for zoom in [| 1; 2; 4; 8 |] do
                        let isActive = model.UI.Zoom = zoom

                        Html.button [
                            prop.key $"zoom-{zoom}"
                            prop.testId $"toolbar-zoom-{zoom}"
                            prop.type'.button
                            prop.className (optionButtonClass isActive)
                            prop.text $"{zoom}x"
                            prop.onClick (fun _ -> dispatch (SetZoom zoom))
                        ]
                ]
            ]
            Html.div [
                prop.className "flex items-center gap-1"
                prop.children [
                    Html.button [
                        prop.testId "toolbar-undo"
                        prop.type'.button
                        prop.className (optionButtonClass false)
                        prop.text "Undo"
                        prop.disabled (not canUndo)
                        prop.onClick (fun _ -> dispatch Undo)
                    ]
                    Html.button [
                        prop.testId "toolbar-redo"
                        prop.type'.button
                        prop.className (optionButtonClass false)
                        prop.text "Redo"
                        prop.disabled (not canRedo)
                        prop.onClick (fun _ -> dispatch Redo)
                    ]
                ]
            ]
            Html.div [
                prop.className "flex items-center gap-1"
                prop.children [
                    Html.button [
                        prop.testId "toolbar-import-button"
                        prop.type'.button
                        prop.className (optionButtonClass false)
                        prop.text "Import"
                        prop.onClick (fun _ ->
                            match fileInputRef.current with
                            | Some input -> input.click ()
                            | None -> ())
                    ]
                    Html.input [
                        prop.testId "toolbar-import-input"
                        prop.ref fileInputRef
                        prop.type'.file
                        prop.accept "image/*"
                        prop.className "sr-only"
                        prop.onChange (dispatchImport dispatch)
                    ]
                    Html.button [
                        prop.testId "toolbar-export-1x"
                        prop.type'.button
                        prop.className (optionButtonClass false)
                        prop.text "Export 1x"
                        prop.onClick (fun _ -> dispatch (ExportPNG Scale1x))
                    ]
                    Html.button [
                        prop.testId "toolbar-export-2x"
                        prop.type'.button
                        prop.className (optionButtonClass false)
                        prop.text "Export 2x"
                        prop.onClick (fun _ -> dispatch (ExportPNG Scale2x))
                    ]
                    Html.button [
                        prop.testId "toolbar-export-4x"
                        prop.type'.button
                        prop.className (optionButtonClass false)
                        prop.text "Export 4x"
                        prop.onClick (fun _ -> dispatch (ExportPNG Scale4x))
                    ]
                ]
            ]
        ]
    ]
