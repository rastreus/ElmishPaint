module Tests.Components.PatternPalette

open App
open App.Canvas
open App.Components.PatternPalette
open Vitest

Vitest.describe (
    "PatternPalette",
    fun () ->
        Vitest.test (
            "renders one swatch for each built-in pattern",
            fun () ->
                let view = RTL.render (PatternPalette Patterns.solidBlack ignore)

                let swatches =
                    Patterns.all
                    |> Array.map (fun pattern ->
                        let swatch = view.getByTestId $"pattern-swatch-{pattern.Id}"
                        Vitest.expect(swatch).toBeInTheDocument ()
                        swatch
                    )

                Vitest.expect(swatches.Length).toBe (Patterns.all.Length)
        )

        Vitest.test (
            "highlights the active pattern swatch",
            fun () ->
                let view = RTL.render (PatternPalette Patterns.dot25 ignore)
                let activeSwatch = view.getByTestId "pattern-swatch-dot-25"
                let inactiveSwatch = view.getByTestId "pattern-swatch-solid-black"

                Vitest.expect(activeSwatch).toHaveClass ("ring-2")
                Vitest.expect(inactiveSwatch).toHaveClass ("border-zinc-400")
        )

        Vitest.test (
            "dispatches SelectPattern when a swatch is clicked",
            fun () ->
                let mutable selectedPatternId: string option = None

                let dispatch msg =
                    match msg with
                    | SelectPattern pattern -> selectedPatternId <- Some pattern.Id
                    | _ -> ()

                let view = RTL.render (PatternPalette Patterns.solidBlack dispatch)
                let swatch = view.getByTestId "pattern-swatch-brick"
                RTL.fireEvent.click (swatch)

                Vitest.expect(selectedPatternId).toEqual (Some "brick")
        )
)