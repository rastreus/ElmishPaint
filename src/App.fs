namespace App

open Feliz

type Components =

    [<ReactComponent>]
    static member App() =
        Html.main [
            prop.className "min-h-screen bg-stone-100 grid place-items-center"
            prop.children [
                Html.h1 [
                    prop.testId "hello-title"
                    prop.className "text-4xl font-bold tracking-tight text-zinc-900"
                    prop.text "Hello ElmishPaint"
                ]
            ]
        ]