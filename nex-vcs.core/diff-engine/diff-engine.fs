namespace Nex.Core.DiffEngine

open System.Collections
open System.Text.RegularExpressions

/// Represents one diff item.
type DiffItem =
    { StartA: int
      StartB: int
      deletedA: int
      insertedB: int }

/// Represents the result of the Shortest Middle Snake search.
type SMSRD = { x: int; y: int }

/// Encapsulates the diff data for one file/version.
type DiffData(data: int[]) =
    member val Data = data with get
    member val Modified = Array.create (data.Length + 2) false with get, set
    member this.Length = data.Length

module Diff =

    /// Converts a text into an array of integers by assigning a unique code
    /// to each unique line. Options allow trimming, collapsing spaces, and ignoring case.
    let private diffCodes (aText: string) (h: Hashtable) (trimSpace: bool) (ignoreSpace: bool) (ignoreCase: bool) =
        // Normalize line endings.
        let aText = aText.Replace("\r", "")
        let lines = aText.Split('\n')
        let codes = Array.zeroCreate lines.Length
        let mutable lastUsedCode = h.Count

        for i in 0 .. lines.Length - 1 do
            let mutable s = lines.[i]

            if trimSpace then
                s <- s.Trim()

            if ignoreSpace then
                s <- Regex.Replace(s, "\\s+", " ")

            if ignoreCase then
                s <- s.ToLower()

            if not (h.ContainsKey(s)) then
                lastUsedCode <- lastUsedCode + 1
                h.Add(s, lastUsedCode)
                codes.[i] <- lastUsedCode
            else
                codes.[i] <- h.[s] :?> int

        codes

    /// Optimizes a DiffData by shifting modified flags for better readability.
    let optimize (data: DiffData) =
        let mutable startPos = 0

        while startPos < data.Length do
            while startPos < data.Length && not data.Modified.[startPos] do
                startPos <- startPos + 1

            let mutable endPos = startPos

            while endPos < data.Length && data.Modified.[endPos] do
                endPos <- endPos + 1

            if endPos < data.Length && data.Data.[startPos] = data.Data.[endPos] then
                data.Modified.[startPos] <- false
                data.Modified.[endPos] <- true
            else
                startPos <- endPos

    /// Implements the Shortest Middle Snake (SMS) algorithm.
    let private sms
        (dataA: DiffData)
        (lowerA: int)
        (upperA: int)
        (dataB: DiffData)
        (lowerB: int)
        (upperB: int)
        (downVector: int[])
        (upVector: int[])
        : SMSRD =
        let max = dataA.Length + dataB.Length + 1
        let downK = lowerA - lowerB
        let upK = upperA - upperB
        let delta = (upperA - lowerA) - (upperB - lowerB)
        let oddDelta = (delta &&& 1) <> 0
        let downOffset = max - downK
        let upOffset = max - upK
        let maxD = ((upperA - lowerA + upperB - lowerB) / 2) + 1

        downVector.[downOffset + downK + 1] <- lowerA
        upVector.[upOffset + upK - 1] <- upperA

        let mutable d = 0
        let mutable found = false
        let mutable result = Unchecked.defaultof<SMSRD>

        while d <= maxD && not found do
            // Extend forward path.
            for k in [ downK - d .. 2 .. downK + d ] do
                let x =
                    if k = downK - d then
                        downVector.[downOffset + k + 1]
                    else
                        let candidate = downVector.[downOffset + k - 1] + 1

                        if k < downK + d && downVector.[downOffset + k + 1] >= candidate then
                            downVector.[downOffset + k + 1]
                        else
                            candidate

                let mutable xLocal = x
                let mutable y = xLocal - k

                while xLocal < upperA && y < upperB && dataA.Data.[xLocal] = dataB.Data.[y] do
                    xLocal <- xLocal + 1
                    y <- y + 1

                downVector.[downOffset + k] <- xLocal

                if oddDelta && k > upK - d && k < upK + d then
                    if upVector.[upOffset + k] <= downVector.[downOffset + k] then
                        result <-
                            { x = downVector.[downOffset + k]
                              y = downVector.[downOffset + k] - k }

                        found <- true

            if not found then
                // Extend reverse path.
                for k in [ upK - d .. 2 .. upK + d ] do
                    let x =
                        if k = upK + d then
                            upVector.[upOffset + k - 1]
                        else
                            let candidate = upVector.[upOffset + k + 1] - 1

                            if k > upK - d && upVector.[upOffset + k - 1] < candidate then
                                upVector.[upOffset + k - 1]
                            else
                                candidate

                    let mutable xLocal = x
                    let mutable y = xLocal - k

                    while xLocal > lowerA && y > lowerB && dataA.Data.[xLocal - 1] = dataB.Data.[y - 1] do
                        xLocal <- xLocal - 1
                        y <- y - 1

                    upVector.[upOffset + k] <- xLocal

                    if (not oddDelta) && k >= downK - d && k <= downK + d then
                        if upVector.[upOffset + k] <= downVector.[downOffset + k] then
                            result <-
                                { x = downVector.[downOffset + k]
                                  y = downVector.[downOffset + k] - k }

                            found <- true

            d <- d + 1

        if found then
            result
        else
            failwith "Algorithm error: SMS not found."

    /// The recursive Longest Common Subsequence (LCS) algorithm.
    let rec lcs
        (dataA: DiffData)
        (lowerA: int)
        (upperA: int)
        (dataB: DiffData)
        (lowerB: int)
        (upperB: int)
        (downVector: int[])
        (upVector: int[])
        =
        // Work with mutable local variables.
        let mutable lA = lowerA
        let mutable lB = lowerB
        let mutable uA = upperA
        let mutable uB = upperB

        while lA < uA && lB < uB && dataA.Data.[lA] = dataB.Data.[lB] do
            lA <- lA + 1
            lB <- lB + 1

        while lA < uA && lB < uB && dataA.Data.[uA - 1] = dataB.Data.[uB - 1] do
            uA <- uA - 1
            uB <- uB - 1

        if lA = uA then
            for i in lB .. uB - 1 do
                dataB.Modified.[i] <- true
        elif lB = uB then
            for i in lA .. uA - 1 do
                dataA.Modified.[i] <- true
        else
            let smsData = sms dataA lA uA dataB lB uB downVector upVector
            lcs dataA lA smsData.x dataB lB smsData.y downVector upVector
            lcs dataA smsData.x uA dataB smsData.y uB downVector upVector

    /// Scans through the DiffData buffers to produce a list of differences.
    let createDiffs (dataA: DiffData) (dataB: DiffData) : DiffItem list =
        let mutable lineA = 0
        let mutable lineB = 0
        let diffs = System.Collections.Generic.List<DiffItem>()

        while lineA < dataA.Length || lineB < dataB.Length do
            if
                lineA < dataA.Length
                && lineB < dataB.Length
                && not dataA.Modified.[lineA]
                && not dataB.Modified.[lineB]
            then
                lineA <- lineA + 1
                lineB <- lineB + 1
            else
                let startA = lineA
                let startB = lineB

                while lineA < dataA.Length && (lineB >= dataB.Length || dataA.Modified.[lineA]) do
                    lineA <- lineA + 1

                while lineB < dataB.Length && (lineA >= dataA.Length || dataB.Modified.[lineB]) do
                    lineB <- lineB + 1

                if startA < lineA || startB < lineB then
                    diffs.Add(
                        { StartA = startA
                          StartB = startB
                          deletedA = lineA - startA
                          insertedB = lineB - startB }
                    )

        diffs |> Seq.toList

    /// Public function: computes the diff between two texts.
    let diffTextOpts
        (textA: string)
        (textB: string)
        (trimSpace: bool)
        (ignoreSpace: bool)
        (ignoreCase: bool)
        : DiffItem list =
        let h = new Hashtable(textA.Length + textB.Length)
        let codesA = diffCodes textA h trimSpace ignoreSpace ignoreCase
        let codesB = diffCodes textB h trimSpace ignoreSpace ignoreCase
        h.Clear() |> ignore
        let dataA = new DiffData(codesA)
        let dataB = new DiffData(codesB)
        let max = dataA.Length + dataB.Length + 1
        let downVector = Array.zeroCreate (2 * max + 2)
        let upVector = Array.zeroCreate (2 * max + 2)
        lcs dataA 0 dataA.Length dataB 0 dataB.Length downVector upVector
        optimize dataA
        optimize dataB
        createDiffs dataA dataB

    let diffText (textA: string) (textB: string) : DiffItem list =
        diffTextOpts textA textB false false false
