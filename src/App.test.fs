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
)