namespace App

open Fable.Core
open Feliz
open Feliz.UseElmish
open Browser.Dom
open Browser.Types
open App.Components.CanvasView
open App.Components.ImportDialog
open App.Components.PatternPalette
open App.Components.Toolbar
open App.Components.StatusBar

module private KeyboardShortcuts =
    let toModifiers (ev: KeyboardEvent) = {
        Shift = ev.shiftKey
        Ctrl = ev.ctrlKey
        Alt = ev.altKey
        Meta = ev.metaKey
    }

    let isPrimaryModifierActive (ev: KeyboardEvent) = ev.ctrlKey || ev.metaKey

    let shouldHandleDocumentShortcut () =
        let activeElement = document.activeElement

        if isNull activeElement then
            true
        else
            let tagName = activeElement.tagName.ToLowerInvariant()
            let isTextInput = tagName = "input" || tagName = "textarea" || tagName = "select"
            let isEditable = (activeElement :?> HTMLElement).isContentEditable
            not (isTextInput || isEditable)

    let shouldPreventDefault (ev: KeyboardEvent) =
        let key = ev.key.ToLowerInvariant()
        isPrimaryModifierActive ev && (key = "z" || key = "s" || key = "i")

    let tryClickImportInput () =
        let input = document.querySelector ("[data-testid='toolbar-import-input']")

        if not (isNull input) then
            (input :?> HTMLInputElement).click ()

[<Erase; Mangle(false)>]
type AppRoot =

    [<ReactComponent(true)>]
    static member App() =
        let model, dispatch = React.useElmish (Runtime.init, Runtime.update, [||])

        React.useEffectOnce (fun () ->
            let handleKeyDown (ev: Event) =
                let keyEvent = ev :?> KeyboardEvent

                if KeyboardShortcuts.shouldHandleDocumentShortcut () then
                    if KeyboardShortcuts.shouldPreventDefault keyEvent then
                        keyEvent.preventDefault ()

                    dispatch (KeyDown(keyEvent.key, KeyboardShortcuts.toModifiers keyEvent))

                    if
                        KeyboardShortcuts.isPrimaryModifierActive keyEvent
                        && keyEvent.key.ToLowerInvariant() = "i"
                    then
                        KeyboardShortcuts.tryClickImportInput ()

            document.addEventListener ("keydown", handleKeyDown)
            fun () -> document.removeEventListener ("keydown", handleKeyDown)
        )

        Html.main [
            prop.className "min-h-screen bg-stone-100 px-4 py-6"
            prop.children [
                Html.div [
                    prop.className "mx-auto flex w-full max-w-[1280px] flex-col gap-3"
                    prop.children [
                        Html.h1 [
                            prop.testId "hello-title"
                            prop.className "text-4xl font-bold tracking-tight text-zinc-900"
                            prop.text "Hello ElmishPaint"
                        ]
                        Toolbar model dispatch
                        Html.div [
                            prop.className "grid grid-cols-1 gap-3 xl:grid-cols-[18rem_minmax(0,_1fr)]"
                            prop.children [
                                Html.aside [
                                    prop.className "rounded border border-zinc-300 bg-white p-3"
                                    prop.children [
                                        Html.h2 [
                                            prop.className
                                                "mb-2 text-sm font-semibold uppercase tracking-wide text-zinc-700"
                                            prop.text "Patterns"
                                        ]
                                        PatternPalette model.Pattern dispatch
                                    ]
                                ]
                                Html.section [
                                    prop.className "rounded border border-zinc-300 bg-white p-2"
                                    prop.children [ CanvasView model dispatch ]
                                ]
                            ]
                        ]
                        StatusBar model
                        Html.section [
                            prop.className "flex items-center gap-4 text-xs text-zinc-600"
                            prop.children [
                                Html.p [ prop.testId "active-tool"; prop.text $"Active tool: {model.Tool}" ]
                                Html.p [
                                    prop.testId "active-pattern"
                                    prop.text $"Active pattern: {model.Pattern.Name}"
                                ]
                            ]
                        ]
                    ]
                ]
                match model.ImportPreview with
                | Some preview -> ImportDialog preview dispatch
                | None -> React.Fragment []
            ]
        ]