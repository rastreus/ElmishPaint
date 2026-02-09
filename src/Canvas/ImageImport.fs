namespace App.Canvas

open System
open App
open Browser.Dom
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop

[<RequireQualifiedAccess>]
module ImageImport =
    [<Literal>]
    let private MinSetting = -128

    [<Literal>]
    let private MaxSetting = 127

    let private supportedMimeTypes =
        set [ "image/png"; "image/jpeg"; "image/gif"; "image/webp" ]

    let private supportedExtensions = set [ ".png"; ".jpg"; ".jpeg"; ".gif"; ".webp" ]

    let private clampSetting value =
        if value < MinSetting then MinSetting
        elif value > MaxSetting then MaxSetting
        else value

    let private clampByte value =
        if value < 0.0 then 0.0
        elif value > 255.0 then 255.0
        else value

    let private getFileName (file: File) : string = emitJsExpr file "($0.name || '')"

    let private getMimeType (file: File) : string =
        emitJsExpr file "($0.type || '').toLowerCase()"

    let private fileExtension (fileName: string) =
        let dotIndex = fileName.LastIndexOf '.'

        if dotIndex >= 0 then
            fileName.Substring(dotIndex).ToLowerInvariant()
        else
            ""

    let isSupportedFile (file: File) =
        let fileName = getFileName file
        let extension = fileExtension fileName
        let mimeType = getMimeType file
        supportedMimeTypes.Contains mimeType || supportedExtensions.Contains extension

    let private ditherScaledPixels (scaledPixels: float array) thresholdOffset brightness =
        if scaledPixels.Length <> (BitCanvas.Width * BitCanvas.Height) then
            invalidArg (nameof scaledPixels) "scaledPixels must match the target canvas size."

        let adjustedPixels =
            scaledPixels |> Array.map (fun value -> clampByte (value + float brightness))

        Dithering.atkinson adjustedPixels BitCanvas.Width BitCanvas.Height thresholdOffset

    let buildPreview fileName thresholdOffset brightness (scaledPixels: float array) =
        let nextThreshold = clampSetting thresholdOffset
        let nextBrightness = clampSetting brightness

        {
            FileName = fileName
            ThresholdOffset = nextThreshold
            Brightness = nextBrightness
            ScaledPixels = scaledPixels
            PreviewCanvas = ditherScaledPixels scaledPixels nextThreshold nextBrightness
        }

    let fromImageData fileName (imageData: ImageData) =
        let grayscalePixels, sourceWidth, sourceHeight = Dithering.toGrayscale imageData

        let scaledPixels =
            Dithering.scaleToFitCanvas grayscalePixels sourceWidth sourceHeight

        buildPreview fileName 0 0 scaledPixels

    let withThreshold thresholdOffset preview =
        buildPreview preview.FileName thresholdOffset preview.Brightness preview.ScaledPixels

    let withBrightness brightness preview =
        buildPreview preview.FileName preview.ThresholdOffset brightness preview.ScaledPixels

    let private createImageBitmap (file: File) : JS.Promise<obj> = emitJsExpr file "createImageBitmap($0)"

    let private nextTick () : JS.Promise<unit> =
        emitJsExpr () "new Promise(resolve => setTimeout(resolve, 0))"

    let private getBitmapWidth (bitmap: obj) : int =
        emitJsExpr bitmap "Math.max(1, ($0.width|0))"

    let private getBitmapHeight (bitmap: obj) : int =
        emitJsExpr bitmap "Math.max(1, ($0.height|0))"

    let private closeBitmap (bitmap: obj) : unit =
        emitJsStatement bitmap "if ($0 && $0.close) { $0.close(); }"

    let private renderBitmapToImageData (bitmap: obj) =
        let bitmapWidth = getBitmapWidth bitmap
        let bitmapHeight = getBitmapHeight bitmap
        let targetWidth = BitCanvas.Width
        let targetHeight = BitCanvas.Height

        let targetCanvas = document.createElement ("canvas") :?> HTMLCanvasElement
        targetCanvas.width <- targetWidth
        targetCanvas.height <- targetHeight

        match targetCanvas.getContext ("2d") with
        | null -> failwith "Failed to create 2D context for import preview."
        | rawContext ->
            let context = rawContext :?> CanvasRenderingContext2D
            emitJsStatement context "$0.fillStyle = '#ffffff'"
            context.fillRect (0.0, 0.0, float targetWidth, float targetHeight)

            let scale =
                min (float targetWidth / float bitmapWidth) (float targetHeight / float bitmapHeight)

            let drawWidth = float bitmapWidth * scale
            let drawHeight = float bitmapHeight * scale
            let offsetX = (float targetWidth - drawWidth) / 2.0
            let offsetY = (float targetHeight - drawHeight) / 2.0

            emitJsStatement
                (context, bitmap, offsetX, offsetY, drawWidth, drawHeight)
                "$0.drawImage($1, $2, $3, $4, $5)"

            context.getImageData (0.0, 0.0, float targetWidth, float targetHeight)

    let loadFromFile (file: File) = promise {
        do! nextTick ()

        let! bitmap = createImageBitmap file

        try
            let imageData = renderBitmapToImageData bitmap
            do! nextTick ()
            return fromImageData (getFileName file) imageData
        finally
            closeBitmap bitmap
    }