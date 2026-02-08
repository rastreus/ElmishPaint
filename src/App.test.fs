module Tests.App

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

                do!
                    RTL.act (fun () -> promise {
                        RTL.fireEvent.click (selectLine)
                    })

                Vitest.expect(activeTool).toHaveTextContent ("Active tool: Line")
            }
        )
)
