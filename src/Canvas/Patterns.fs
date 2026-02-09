namespace App.Canvas

open App

[<RequireQualifiedAccess>]
module Patterns =
    let private tileSize = 8

    let private row (bits: string) =
        if bits.Length <> tileSize then
            invalidArg (nameof bits) "pattern rows must be 8 bits wide"

        bits |> Seq.map (fun bit -> bit = '1') |> Seq.toArray

    let private tile (rows: string array) =
        if rows.Length <> tileSize then
            invalidArg (nameof rows) "patterns must have exactly 8 rows"

        rows |> Array.map row

    let private create id name rows = {
        Id = id
        Name = name
        Tile = tile rows
    }

    let solidBlack =
        create "solid-black" "Solid Black" [|
            "11111111"
            "11111111"
            "11111111"
            "11111111"
            "11111111"
            "11111111"
            "11111111"
            "11111111"
        |]

    let solidWhite =
        create "solid-white" "Solid White" [|
            "00000000"
            "00000000"
            "00000000"
            "00000000"
            "00000000"
            "00000000"
            "00000000"
            "00000000"
        |]

    let checkerboard50 =
        create "checkerboard-50" "Checkerboard 50%" [|
            "10101010"
            "01010101"
            "10101010"
            "01010101"
            "10101010"
            "01010101"
            "10101010"
            "01010101"
        |]

    let dot25 =
        create "dot-25" "Dot 25%" [|
            "10101010"
            "00000000"
            "10101010"
            "00000000"
            "10101010"
            "00000000"
            "10101010"
            "00000000"
        |]

    let dot75 =
        create "dot-75" "Dot 75%" [|
            "01010101"
            "11111111"
            "01010101"
            "11111111"
            "01010101"
            "11111111"
            "01010101"
            "11111111"
        |]

    let horizontalStripes =
        create "horizontal-stripes" "Horizontal Stripes" [|
            "11111111"
            "00000000"
            "11111111"
            "00000000"
            "11111111"
            "00000000"
            "11111111"
            "00000000"
        |]

    let verticalStripes =
        create "vertical-stripes" "Vertical Stripes" [|
            "10101010"
            "10101010"
            "10101010"
            "10101010"
            "10101010"
            "10101010"
            "10101010"
            "10101010"
        |]

    let diagonalStripesLeft =
        create "diagonal-stripes-left" "Diagonal Stripes Left" [|
            "00010001"
            "00100010"
            "01000100"
            "10001000"
            "00010001"
            "00100010"
            "01000100"
            "10001000"
        |]

    let diagonalStripesRight =
        create "diagonal-stripes-right" "Diagonal Stripes Right" [|
            "10001000"
            "01000100"
            "00100010"
            "00010001"
            "10001000"
            "01000100"
            "00100010"
            "00010001"
        |]

    let crosshatch =
        create "crosshatch" "Crosshatch" [|
            "11111111"
            "10101010"
            "11111111"
            "10101010"
            "11111111"
            "10101010"
            "11111111"
            "10101010"
        |]

    let brick =
        create "brick" "Brick" [|
            "11111111"
            "10001000"
            "10001000"
            "11111111"
            "00100010"
            "00100010"
            "11111111"
            "10001000"
        |]

    let polkaDot =
        create "polka-dot" "Polka Dot" [|
            "00100100"
            "01111110"
            "11111111"
            "01111110"
            "00100100"
            "00000000"
            "00100100"
            "00011000"
        |]

    let all = [|
        solidBlack
        solidWhite
        checkerboard50
        dot25
        dot75
        horizontalStripes
        verticalStripes
        diagonalStripesLeft
        diagonalStripesRight
        crosshatch
        brick
        polkaDot
    |]

    let private canonicalizeId (id: string) = id.Trim().ToLowerInvariant()

    let private byId =
        all
        |> Array.map (fun pattern -> canonicalizeId pattern.Id, pattern)
        |> Map.ofArray

    let tryFindById id =
        let canonicalId = canonicalizeId id

        match Map.tryFind canonicalId byId with
        | Some pattern -> Some pattern
        | None when canonicalId = "dither-50" -> Some checkerboard50
        | None -> None

    let fromId id =
        match tryFindById id with
        | Some pattern -> pattern
        | None -> solidBlack

    let private wrapToTile value =
        let wrapped = value % tileSize
        if wrapped < 0 then wrapped + tileSize else wrapped

    let sample pattern x y =
        let tileX = wrapToTile x
        let tileY = wrapToTile y
        pattern.Tile[tileY][tileX]

    let sampleBit pattern x y =
        if sample pattern x y then Black else White