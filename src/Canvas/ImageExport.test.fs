module Tests.Canvas.ImageExport

open App
open App.Canvas
open Browser.Types
open Fable.Core.JsInterop
open Vitest

let private withExportSpies (run: unit -> unit) =
    emitJsStatement
        ()
        """
window.__exportTest = { clicks: 0, links: [], canvas: null };
window.__exportOriginalToDataURL = HTMLCanvasElement.prototype.toDataURL;
window.__exportOriginalClick = HTMLAnchorElement.prototype.click;
HTMLCanvasElement.prototype.toDataURL = function() {
  window.__exportTest.canvas = this;
  return 'data:image/png;base64,TEST';
};
HTMLAnchorElement.prototype.click = function() {
  window.__exportTest.clicks += 1;
  window.__exportTest.links.push({
    download: this.getAttribute('download') || '',
    href: this.href || ''
  });
};
"""

    try
        run ()
    finally
        emitJsStatement
            ()
            """
if (window.__exportOriginalToDataURL) {
  HTMLCanvasElement.prototype.toDataURL = window.__exportOriginalToDataURL;
}
if (window.__exportOriginalClick) {
  HTMLAnchorElement.prototype.click = window.__exportOriginalClick;
}
delete window.__exportOriginalToDataURL;
delete window.__exportOriginalClick;
delete window.__exportTest;
"""

let private capturedClicks () : int = emitJsExpr () "(window.__exportTest?.clicks ?? 0)"

let private capturedDownloadAt index : string =
    emitJsExpr index "(window.__exportTest?.links?.[$0]?.download ?? '')"

let private capturedHrefAt index : string =
    emitJsExpr index "(window.__exportTest?.links?.[$0]?.href ?? '')"

let private capturedCanvas () : HTMLCanvasElement = emitJsExpr () "(window.__exportTest?.canvas ?? null)"

let private capturedImageData () =
    let canvas = capturedCanvas ()

    if isNull canvas then
        failwith "expected export canvas capture"

    let hasImageData: bool = emitJsExpr canvas "!!$0.__lastImageData"

    if not hasImageData then
        failwith "expected putImageData to run during export"

    unbox<ImageData> canvas?__lastImageData

let private pixelChannel x y (imageData: ImageData) =
    let width = int imageData.width
    int imageData.data[((y * width) + x) * 4]

let private assertMonochrome (imageData: ImageData) =
    let data = imageData.data
    let mutable isValid = true
    let mutable index = 0

    while isValid && index <= data.Length - 4 do
        let red = int data[index]
        let green = int data[index + 1]
        let blue = int data[index + 2]
        let alpha = int data[index + 3]

        isValid <-
            red = green
            && green = blue
            && alpha = 255
            && (red = 0 || red = 255)

        index <- index + 4

    Vitest.expect(isValid).toBeTruthy ()

Vitest.describe (
    "ImageExport",
    fun () ->
        Vitest.test (
            "Scale1x exports 512x342 png download and preserves black and white pixels",
            fun () ->
                let canvas = BitCanvas.create ()
                BitCanvas.setPixel 0 0 Black canvas

                withExportSpies (fun () ->
                    ImageExport.exportPng Scale1x canvas

                    let imageData = capturedImageData ()

                    Vitest.expect(int imageData.width).toBe (512)
                    Vitest.expect(int imageData.height).toBe (342)
                    Vitest.expect(capturedClicks ()).toBe (1)
                    Vitest.expect(capturedDownloadAt 0).toEqual ("elmishpaint-1x.png")
                    Vitest.expect(capturedHrefAt 0).toContain ("data:image/png")
                    Vitest.expect(pixelChannel 0 0 imageData).toBe (0)
                    Vitest.expect(pixelChannel 1 0 imageData).toBe (255)
                    assertMonochrome imageData
                )
        )

        Vitest.test (
            "Scale2x exports 1024x684 png with 2x2 pixel blocks",
            fun () ->
                let canvas = BitCanvas.create ()
                BitCanvas.setPixel 1 1 Black canvas

                withExportSpies (fun () ->
                    ImageExport.exportPng Scale2x canvas

                    let imageData = capturedImageData ()

                    Vitest.expect(int imageData.width).toBe (1024)
                    Vitest.expect(int imageData.height).toBe (684)
                    Vitest.expect(capturedDownloadAt 0).toEqual ("elmishpaint-2x.png")
                    Vitest.expect(pixelChannel 2 2 imageData).toBe (0)
                    Vitest.expect(pixelChannel 3 2 imageData).toBe (0)
                    Vitest.expect(pixelChannel 2 3 imageData).toBe (0)
                    Vitest.expect(pixelChannel 3 3 imageData).toBe (0)
                    Vitest.expect(pixelChannel 4 2 imageData).toBe (255)
                    assertMonochrome imageData
                )
        )

        Vitest.test (
            "Scale4x exports 2048x1368 png download",
            fun () ->
                let canvas = BitCanvas.create ()
                BitCanvas.setPixel 3 2 Black canvas

                withExportSpies (fun () ->
                    ImageExport.exportPng Scale4x canvas

                    let imageData = capturedImageData ()

                    Vitest.expect(int imageData.width).toBe (2048)
                    Vitest.expect(int imageData.height).toBe (1368)
                    Vitest.expect(capturedClicks ()).toBe (1)
                    Vitest.expect(capturedDownloadAt 0).toEqual ("elmishpaint-4x.png")
                    Vitest.expect(capturedHrefAt 0).toContain ("data:image/png")
                    assertMonochrome imageData
                )
        )
)
