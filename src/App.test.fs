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
                let ele = RTL.render (App.AppRoot.App())
                let title = ele.getByTestId ("hello-title")

                Vitest.expect(title).toBeInTheDocument ()
                Vitest.expect(title).toHaveTextContent ("Hello ElmishPaint")
                Vitest.expect(title).toHaveClass ("text-4xl")
            }
        )

        Vitest.test (
            "dispatching SelectTool updates rendered active tool",
            fun () -> promise {
                let ele = RTL.render (App.AppRoot.App())
                let activeTool = ele.getByTestId ("active-tool")
                let selectLine = ele.getByTestId ("toolbar-tool-line")

                Vitest.expect(activeTool).toHaveTextContent ("Active tool: Pencil")

                do! RTL.act (fun () -> promise { RTL.fireEvent.click (selectLine) })

                Vitest.expect(activeTool).toHaveTextContent ("Active tool: Line")
            }
        )

        Vitest.test (
            "clicking a pattern swatch updates rendered active pattern",
            fun () -> promise {
                let ele = RTL.render (App.AppRoot.App())
                let activePattern = ele.getByTestId ("active-pattern")
                let checkerSwatch = ele.getByTestId ("pattern-swatch-checkerboard-50")

                Vitest.expect(activePattern).toHaveTextContent ("Active pattern: Solid Black")

                do! RTL.act (fun () -> promise { RTL.fireEvent.click (checkerSwatch) })

                Vitest.expect(activePattern).toHaveTextContent ("Active pattern: Checkerboard 50%")
            }
        )

        Vitest.test (
            "canvas mousedown updates pixel and rerenders image data",
            fun () -> promise {
                let ele = RTL.render (App.AppRoot.App())
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

        Vitest.test (
            "renders toolbar controls and status bar",
            fun () ->
                let ele = RTL.render (App.AppRoot.App())

                ele.getByTestId ("toolbar-tool-pencil") |> ignore
                ele.getByTestId ("toolbar-tool-eraser") |> ignore
                ele.getByTestId ("toolbar-tool-line") |> ignore
                ele.getByTestId ("toolbar-tool-rectangle") |> ignore
                ele.getByTestId ("toolbar-tool-filled-rectangle") |> ignore
                ele.getByTestId ("toolbar-tool-flood-fill") |> ignore
                ele.getByTestId ("toolbar-tool-marquee") |> ignore
                ele.getByTestId ("toolbar-zoom-1") |> ignore
                ele.getByTestId ("toolbar-import-button") |> ignore
                ele.getByTestId ("toolbar-export-1x") |> ignore
                ele.getByTestId ("status-coordinates") |> ignore
        )

        Vitest.test (
            "zoom controls change canvas dimensions through app state",
            fun () -> promise {
                let ele = RTL.render (App.AppRoot.App())
                let canvas = ele.getByTestId ("paint-canvas")
                let zoomFour = ele.getByTestId ("toolbar-zoom-4")

                Vitest.expect(canvas).toHaveAttribute ("width", "512")
                Vitest.expect(canvas).toHaveAttribute ("height", "342")

                do! RTL.act (fun () -> promise { RTL.fireEvent.click (zoomFour) })

                Vitest.expect(canvas).toHaveAttribute ("width", "2048")
                Vitest.expect(canvas).toHaveAttribute ("height", "1368")
            }
        )

        Vitest.test (
            "status bar coordinates update from canvas mouse move",
            fun () -> promise {
                let ele = RTL.render (App.AppRoot.App())
                let canvas = ele.getByTestId ("paint-canvas")
                let coordinates = ele.getByTestId ("status-coordinates")

                Vitest.expect(coordinates).toHaveTextContent ("X: -- Y: --")

                do!
                    RTL.act (fun () -> promise {
                        RTL.fireEvent.custom ("mouseMove", canvas, createObj [ "clientX" ==> 17; "clientY" ==> 9 ])
                    })

                Vitest.expect(coordinates).toHaveTextContent ("X: 17 Y: 9")
            }
        )

        Vitest.test (
            "undo and redo buttons enable and disable from history changes",
            fun () -> promise {
                let ele = RTL.render (App.AppRoot.App())
                let canvas = ele.getByTestId ("paint-canvas")
                let undoButton = ele.getByTestId ("toolbar-undo")
                let redoButton = ele.getByTestId ("toolbar-redo")

                Vitest.expect(undoButton).toBeDisabled ()
                Vitest.expect(redoButton).toBeDisabled ()

                do!
                    RTL.act (fun () -> promise {
                        RTL.fireEvent.custom ("mouseDown", canvas, createObj [ "clientX" ==> 0; "clientY" ==> 0 ])
                        RTL.fireEvent.custom ("mouseUp", canvas, createObj [ "clientX" ==> 0; "clientY" ==> 0 ])
                    })

                Vitest.expect(undoButton).toBeEnabled ()
                Vitest.expect(redoButton).toBeDisabled ()

                do! RTL.act (fun () -> promise { RTL.fireEvent.click (undoButton) })

                Vitest.expect(redoButton).toBeEnabled ()
            }
        )
)
