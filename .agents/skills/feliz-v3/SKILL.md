---
name: feliz-v3
description: "**REQUIRED** - Always activate before writing ANY Feliz, React, or Elmish UI code. This project uses Feliz v3 which has BREAKING CHANGES from v2. Training data contains v2 patterns that WILL cause compilation errors. Covers: component syntax, React.memo, React.lazy, useElmish, context providers, PascalCase APIs, and Fable 5 transpilation."
allowed-tools: Bash(dotnet *), Bash(pnpm *), Bash(npx *)
---

# Feliz v3 — Agent Reference Guide

> **CRITICAL**: This project uses Feliz v3, Fable 5, .NET 10, and React 19.
> Training data likely contains Feliz v2 patterns. Using v2 syntax WILL cause
> compilation errors. Read this file before writing ANY Feliz code.

## Stack Versions

| Package            | Version | Notes                        |
|--------------------|---------|------------------------------|
| Feliz              | ~> 3    | Breaking changes from v2     |
| Feliz.UseElmish    | ~> 4    | Breaking changes from v2     |
| Elmish             | ~> 5    | With Fable.Elmish ~> 5       |
| Fable.Elmish.React | ~> 5    |                              |
| Fable.Elmish.HMR   | ~> 8    |                              |
| Fable              | 5.x     | .NET 10 tool                 |
| React              | 19.x    | Required by Feliz v3         |
| FSharp.Core        | ~> 10   |                              |

## Breaking Changes from v2

### 1. React.memo — REQUIRES React.memoRender

**v2 (WRONG — will not compile):**
```fsharp
// DON'T DO THIS
let MyComponent = React.memo(fun () -> Html.div [ prop.text "hello" ])
// and then calling it directly
```

**v3 (CORRECT):**
```fsharp
let MemoFunction =
    React.memo<{| text: string |}>(fun props ->
        Html.div [ prop.text props.text ]
    )

[<ReactComponent>]
let Main () =
    React.memoRender(MemoFunction, {| text = "hello" |})
```

### 2. React.lazy' — REQUIRES React.lazyRender

**v2 (WRONG):**
```fsharp
// DON'T DO THIS — old syntax
let LazyComp = React.lazy'(fun () -> importDynamic "./MyComp")
```

**v3 (CORRECT):**
```fsharp
let LazyHello: LazyComponent<unit> =
    React.lazy'(fun () ->
        promise {
            do! Promise.sleep 2000
            return! JsInterop.importDynamic "./Counter"
        }
    )

[<ReactComponent>]
let SuspenseDemo() =
    Html.div [
        React.Suspense(
            [ React.lazyRender(LazyHello, ()) ],
            Html.div [ prop.text "Loading..." ]
        )
    ]
```

### 3. React.createContext — New Provider Syntax

**v2 (WRONG):**
```fsharp
// DON'T DO THIS
let ctx = React.createContext("default")
React.contextProvider(ctx, value, children)
```

**v3 (CORRECT):**
```fsharp
let CounterContext = React.createContext(None: (int * (int -> unit)) option)

[<ReactComponent>]
let CounterDisplay() =
    let ctx = React.useContext(CounterContext)
    match ctx with
    | Some(count, _) -> Html.p [ prop.text $"Current count: {count}" ]
    | None -> Html.p [ prop.text "No context available" ]

[<ReactComponent(true)>]
let UseContext() =
    let count, setCount = React.useState(0)
    CounterContext.Provider(
        (Some (count, setCount)),
        CounterDisplay()
    )
```

### 4. PascalCase Component Names

All built-in React wrapper components now use PascalCase:

| v2 (WRONG)              | v3 (CORRECT)             |
|--------------------------|--------------------------|
| `React.fragment`         | `React.Fragment`         |
| `React.keyedFragment`    | `React.KeyedFragment`    |
| `React.strictMode`       | `React.StrictMode`       |
| `React.suspense`         | `React.Suspense`         |
| `React.provider`         | `React.Provider`         |
| `React.consumer`         | `React.Consumer`         |

### 5. FsReact Namespace for Interop Helpers

These helpers moved to the `FsReact` namespace:

```fsharp
open FsReact

FsReact.createDisposable
FsReact.useDisposable
FsReact.useCancellationToken
```

## Correct useElmish Pattern (Feliz v3)

This is the canonical pattern for Elmish components in this project:

```fsharp
module App

open Fable.Core
open Feliz
open Feliz.UseElmish
open Elmish

type Msg =
    | Increment
    | Decrement

type State = { Count: int }

let init () = { Count = 0 }, Cmd.none

let update msg state =
    match msg with
    | Increment -> { state with Count = state.Count + 1 }, Cmd.none
    | Decrement -> { state with Count = state.Count - 1 }, Cmd.none

[<Erase; Mangle(false)>]
type Main =
    [<ReactComponent(true)>]
    static member Main() =
        let state, dispatch = React.useElmish(init, update, [| |])
        Html.div [
            Html.h1 state.Count
            Html.button [
                prop.text "Increment"
                prop.onClick (fun _ -> dispatch Increment)
            ]
            Html.button [
                prop.text "Decrement"
                prop.onClick (fun _ -> dispatch Decrement)
            ]
        ]
```

**Key points about useElmish in v3:**
- Import `Feliz.UseElmish` (not `Fable.React.UseElmish`)
- The `[<Erase; Mangle(false)>]` type wrapper is for export/HMR compatibility
- `[<ReactComponent(true)>]` on a static member means it's an "import-friendly" component
- Third argument `[| |]` is the dependency array (empty = init once)

## Standard Feliz Patterns (Unchanged in v3)

These patterns work the same in v2 and v3:

### Basic Component

```fsharp
[<ReactComponent>]
let Counter() =
    let (count, setCount) = React.useState(0)
    Html.div [
        Html.button [
            prop.onClick (fun _ -> setCount(count + 1))
            prop.text "Increment"
        ]
        Html.h1 count
    ]
```

### Hooks

```fsharp
// useState
let (value, setValue) = React.useState(initialValue)

// useEffect (runs on every render)
React.useEffect(fun () -> (* effect *) )

// useEffect with deps
React.useEffect((fun () -> (* effect *)), [| dep1; dep2 |])

// useEffect with cleanup
React.useEffectOnce(fun () ->
    (* setup *)
    React.createDisposable(fun () -> (* cleanup *))
)

// useRef
let inputRef = React.useRef(None)

// useCallback
let handler = React.useCallback(fun () -> (* handler *), [| dep |])

// useMemo
let expensive = React.useMemo(fun () -> computeExpensiveValue(), [| dep |])
```

### HTML DSL

```fsharp
Html.div [
    prop.className "container"
    prop.children [
        Html.h1 [ prop.text "Title" ]
        Html.p [ prop.text "Paragraph" ]
        Html.canvas [
            prop.id "my-canvas"
            prop.width 512
            prop.height 342
            prop.ref (fun el -> (* canvas ref callback *))
            prop.onMouseDown (fun ev ->
                let x = ev.clientX
                let y = ev.clientY
                (* handler *)
            )
        ]
    ]
]
```

### Inline Styles

```fsharp
Html.div [
    prop.style [
        style.display.flex
        style.width (length.px 512)
        style.height (length.px 342)
        style.backgroundColor "#ffffff"
        style.cursor.crosshair
        style.border (1, borderStyle.solid, "#000000")
        style.imageRendering.pixelated
    ]
]
```

### Conditional Rendering

```fsharp
Html.div [
    if model.ShowToolbar then
        Html.div [ prop.className "toolbar"; prop.children [ (* ... *) ] ]
    Html.div [ prop.className "canvas-area"; prop.children [ (* ... *) ] ]
]
```

## Fable Transpilation

This project uses Fable 5 with `.fs.jsx` output:

```bash
# Transpile command (from package.json)
dotnet fable src -e fs.jsx --verbose

# Dev server with watch
dotnet fable src -e fs.jsx --verbose --watch --runFast vite
```

**File naming**: `Components.fs` transpiles to `Components.fs.jsx`.
Reference transpiled files in imports using the `.fs.jsx` extension.

## Testing with Vitest

Tests use Vitest bindings (not Expecto):

```fsharp
open Vitest
open Vitest.JestDom
open Vitest.ReactTestingLibrary
open Vitest.UserEvent

describe "MyComponent" (fun () ->
    it "renders correctly" (fun () -> promise {
        let! result = RTL.render(MyComponent())
        let element = result.getByText("expected text")
        expect(element).toBeInTheDocument()
    })
)
```

Run tests with `pnpm test`.

## Documentation Reference

Full v3 docs: https://fable-hub.github.io/Feliz/
- Feliz syntax: https://fable-hub.github.io/Feliz/category/feliz
- React APIs: https://fable-hub.github.io/Feliz/category/react
- Hooks: https://fable-hub.github.io/Feliz/category/hooks
- Guides: https://fable-hub.github.io/Feliz/category/guides
- Upgrade guide: https://fable-hub.github.io/Feliz/api-docs/Upgrade
