module App.Components.ImportDialog

open App
open App.Canvas
open Browser.Types
open Feliz

let private parseSliderValue (ev: Event) =
    let target = ev.target :?> HTMLInputElement
    int target.value

let private sliderClass =
    "h-2 w-full cursor-pointer appearance-none rounded-lg bg-zinc-200"

[<ReactComponent>]
let ImportDialog (preview: ImportPreview) (dispatch: Msg -> unit) =
    let previewCanvasRef = React.useRef<HTMLCanvasElement option> (None)

    React.useEffect (fun () ->
        match previewCanvasRef.current with
        | None -> ()
        | Some canvas ->
            match canvas.getContext ("2d") with
            | null -> ()
            | rawContext ->
                let context = rawContext :?> CanvasRenderingContext2D
                context.imageSmoothingEnabled <- false
                context.putImageData (BitCanvas.toImageData 1 preview.PreviewCanvas, 0.0, 0.0)
    )

    Html.div [
        prop.testId "import-dialog-overlay"
        prop.className "fixed inset-0 z-40 flex items-center justify-center bg-black/60 p-4"
        prop.children [
            Html.section [
                prop.testId "import-dialog"
                prop.className "w-full max-w-5xl rounded border border-zinc-900 bg-stone-100 p-4 shadow-xl"
                prop.children [
                    Html.div [
                        prop.className "mb-3 flex items-end justify-between"
                        prop.children [
                            Html.h2 [
                                prop.className "text-lg font-semibold text-zinc-900"
                                prop.text "Import Image Preview"
                            ]
                            Html.p [
                                prop.testId "import-file-name"
                                prop.className "text-sm text-zinc-600"
                                prop.text preview.FileName
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "grid gap-4 xl:grid-cols-[minmax(0,_1fr)_22rem]"
                        prop.children [
                            Html.div [
                                prop.className "overflow-auto rounded border border-zinc-900 bg-white p-2"
                                prop.children [
                                    Html.canvas [
                                        prop.testId "import-preview-canvas"
                                        prop.ref previewCanvasRef
                                        prop.width BitCanvas.Width
                                        prop.height BitCanvas.Height
                                        prop.style [
                                            style.width (length.percent 100)
                                            style.custom ("height", "auto")
                                            style.custom ("imageRendering", "pixelated")
                                        ]
                                    ]
                                ]
                            ]
                            Html.div [
                                prop.className "space-y-4 rounded border border-zinc-300 bg-white p-3"
                                prop.children [
                                    Html.div [
                                        prop.className "space-y-2"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex items-center justify-between"
                                                prop.children [
                                                    Html.label [
                                                        prop.htmlFor "import-threshold-slider"
                                                        prop.className "text-sm font-medium text-zinc-700"
                                                        prop.text "Threshold"
                                                    ]
                                                    Html.span [
                                                        prop.testId "import-threshold-value"
                                                        prop.className "text-sm text-zinc-600"
                                                        prop.text (string preview.ThresholdOffset)
                                                    ]
                                                ]
                                            ]
                                            Html.input [
                                                prop.id "import-threshold-slider"
                                                prop.testId "import-threshold-slider"
                                                prop.type'.range
                                                prop.min -128
                                                prop.max 127
                                                prop.step 1
                                                prop.value preview.ThresholdOffset
                                                prop.className sliderClass
                                                prop.onChange (fun ev ->
                                                    dispatch (SetImportThreshold(parseSliderValue ev))
                                                )
                                            ]
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "space-y-2"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex items-center justify-between"
                                                prop.children [
                                                    Html.label [
                                                        prop.htmlFor "import-brightness-slider"
                                                        prop.className "text-sm font-medium text-zinc-700"
                                                        prop.text "Brightness"
                                                    ]
                                                    Html.span [
                                                        prop.testId "import-brightness-value"
                                                        prop.className "text-sm text-zinc-600"
                                                        prop.text (string preview.Brightness)
                                                    ]
                                                ]
                                            ]
                                            Html.input [
                                                prop.id "import-brightness-slider"
                                                prop.testId "import-brightness-slider"
                                                prop.type'.range
                                                prop.min -128
                                                prop.max 127
                                                prop.step 1
                                                prop.value preview.Brightness
                                                prop.className sliderClass
                                                prop.onChange (fun ev ->
                                                    dispatch (SetImportBrightness(parseSliderValue ev))
                                                )
                                            ]
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "flex items-center justify-end gap-2 pt-2"
                                        prop.children [
                                            Html.button [
                                                prop.testId "import-cancel"
                                                prop.type'.button
                                                prop.className
                                                    "rounded border border-zinc-400 bg-white px-3 py-1 text-sm font-medium text-zinc-900"
                                                prop.text "Cancel"
                                                prop.onClick (fun _ -> dispatch CancelImport)
                                            ]
                                            Html.button [
                                                prop.testId "import-confirm"
                                                prop.type'.button
                                                prop.className
                                                    "rounded border border-zinc-900 bg-zinc-900 px-3 py-1 text-sm font-semibold text-white"
                                                prop.text "Confirm Import"
                                                prop.onClick (fun _ -> dispatch ConfirmImport)
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]