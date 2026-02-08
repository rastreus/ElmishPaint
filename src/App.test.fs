module Tests.App

open Browser.Types
open Fable.Core.JsInterop
open Vitest
open Feliz

Vitest.describe (
    "App scaffold",
    fun () ->
        Vitest.test (
            "renders hello heading with tailwind classes",
            fun () -> promise {
                let ele = RTL.render (App.Components.App())
                let title = ele.getByTestId ("hello-title")

                Vitest.expect(title).toBeInTheDocument ()
                Vitest.expect(title).toHaveTextContent ("Hello ElmishPaint")
                Vitest.expect(title).toHaveClass ("text-4xl")
            }
        )

        Vitest.test (
            "dispatching SelectTool updates rendered active tool",
            fun () -> promise {
                let ele = RTL.render (App.Components.App())
                let activeTool = ele.getByTestId ("active-tool")
                let selectLine = ele.getByTestId ("select-line")

                Vitest.expect(activeTool).toHaveTextContent ("Active tool: Pencil")

                do! RTL.act (fun () -> promise { RTL.fireEvent.click (selectLine) })

                Vitest.expect(activeTool).toHaveTextContent ("Active tool: Line")
            }
        )

        Vitest.test (
            "canvas mousedown updates pixel and rerenders image data",
            fun () -> promise {
                let ele = RTL.render (App.Components.App())
                let canvas = ele.getByTestId ("paint-canvas") :?> HTMLCanvasElement
                let before: ImageData = unbox canvas?__lastImageData
                Vitest.expect(before.data[0]).toBe (255uy)

                do!
                    RTL.act (fun () -> promise {
                        RTL.fireEvent.custom ("mouseDown", canvas, createObj [ "clientX" ==> 0; "clientY" ==> 0 ])
                    })

                let after: ImageData = unbox canvas?__lastImageData
                Vitest.expect(after.data[0]).toBe (0uy)
            }
        )
)