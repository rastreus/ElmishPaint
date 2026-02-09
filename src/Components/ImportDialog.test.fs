module Tests.Components.ImportDialog

open App
open App.Canvas
open App.Components.ImportDialog
open Browser.Types
open Fable.Core.JsInterop
open Vitest

let private createPreview () =
    let scaledPixels = Array.create (BitCanvas.Width * BitCanvas.Height) 255.0
    let previewCanvas = BitCanvas.create ()
    BitCanvas.setPixel 0 0 Black previewCanvas

    {
        FileName = "sample.webp"
        ThresholdOffset = 0
        Brightness = 0
        ScaledPixels = scaledPixels
        PreviewCanvas = previewCanvas
    }

Vitest.describe (
    "ImportDialog",
    fun () ->
        Vitest.test (
            "renders file metadata sliders and action buttons",
            fun () ->
                let view = RTL.render (ImportDialog (createPreview ()) ignore)

                Vitest.expect(view.getByTestId ("import-dialog-overlay")).toBeInTheDocument ()
                Vitest.expect(view.getByTestId ("import-file-name")).toHaveTextContent ("sample.webp")
                Vitest.expect(view.getByTestId ("import-threshold-slider")).toBeInTheDocument ()
                Vitest.expect(view.getByTestId ("import-brightness-slider")).toBeInTheDocument ()
                Vitest.expect(view.getByTestId ("import-confirm")).toBeInTheDocument ()
                Vitest.expect(view.getByTestId ("import-cancel")).toBeInTheDocument ()
        )

        Vitest.test (
            "dispatches threshold and brightness messages from slider changes",
            fun () ->
                let mutable dispatched: Msg list = []
                let dispatch msg = dispatched <- dispatched @ [ msg ]
                let view = RTL.render (ImportDialog (createPreview ()) dispatch)

                RTL.fireEvent.change (
                    view.getByTestId ("import-threshold-slider"),
                    createObj [ "target" ==> createObj [ "value" ==> "16" ] ]
                )

                RTL.fireEvent.change (
                    view.getByTestId ("import-brightness-slider"),
                    createObj [ "target" ==> createObj [ "value" ==> "-12" ] ]
                )

                match dispatched with
                | [ SetImportThreshold 16; SetImportBrightness -12 ] -> ()
                | _ -> failwith "expected import slider dispatches"
        )

        Vitest.test (
            "dispatches confirm and cancel actions",
            fun () ->
                let mutable dispatched: Msg list = []
                let dispatch msg = dispatched <- dispatched @ [ msg ]
                let view = RTL.render (ImportDialog (createPreview ()) dispatch)

                RTL.fireEvent.click (view.getByTestId ("import-confirm"))
                RTL.fireEvent.click (view.getByTestId ("import-cancel"))

                match dispatched with
                | [ ConfirmImport; CancelImport ] -> ()
                | _ -> failwith "expected confirm and cancel dispatches"
        )

        Vitest.test (
            "renders preview canvas from BitCanvas image data",
            fun () ->
                let view = RTL.render (ImportDialog (createPreview ()) ignore)
                let canvas = view.getByTestId ("import-preview-canvas") :?> HTMLCanvasElement
                let imageData: ImageData = unbox canvas?__lastImageData

                Vitest.expect(imageData.width).toBe (BitCanvas.Width)
                Vitest.expect(imageData.height).toBe (BitCanvas.Height)
                Vitest.expect(imageData.data[0]).toBe (0uy)
                Vitest.expect(imageData.data[1]).toBe (0uy)
                Vitest.expect(imageData.data[2]).toBe (0uy)
        )
)
