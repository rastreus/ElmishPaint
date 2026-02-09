module Tests.Components.StatusBar

open App
open App.Components.StatusBar
open Vitest

let private defaultModel () = fst (Runtime.init ())

Vitest.describe (
    "StatusBar",
    fun () ->
        Vitest.test (
            "shows placeholder coordinates when mouse has not moved",
            fun () ->
                let view = RTL.render (StatusBar (defaultModel ()))
                let coordinates = view.getByTestId ("status-coordinates")

                Vitest.expect(coordinates).toHaveTextContent ("X: -- Y: --")
        )

        Vitest.test (
            "shows the current mouse coordinates",
            fun () ->
                let model = defaultModel ()

                let modelWithPoint = {
                    model with
                        Mouse = {
                            model.Mouse with
                                Current = Some { X = 42; Y = 21 }
                        }
                }

                let view = RTL.render (StatusBar modelWithPoint)
                let coordinates = view.getByTestId ("status-coordinates")

                Vitest.expect(coordinates).toHaveTextContent ("X: 42 Y: 21")
        )
)
