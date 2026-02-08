namespace App

type Model = {
    Canvas: BitCanvas
    Tool: Tool
    ToolOptions: ToolOptions
    Pattern: Pattern
    Mouse: MouseState
    Selection: Selection option
    History: HistoryState
    UI: UIState
    ImportPreview: ImportPreview option
}