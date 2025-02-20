namespace Nex.Core

(*
 --------------------------------------------------------------------------------------------
 This implementation of the Myers Diff algorithm has been ported from the 
 C# version provided by Matthias Hertel under the BSD-3-Clause License
 https://github.com/mathertel/Diff. Thank you for a clear and understandable implementation!
 --------------------------------------------------------------------------------------------
 *)

open System.Collections
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Nex.Core.Types


/// Represents the result of the Shortest Middle Snake search.
type SMSRT = { x: int; y: int }

/// Encapsulates the diff data for one file/version.
type DiffData(data: int[]) =
    member val Data = data with get
    member val Modified = Array.create (data.Length + 2) false with get, set
    member this.Length = data.Length

/// <summary>
/// Represents the options set for the Diffing algorithm
/// </summary>
type private DiffOpts =
    { TrimSpace: bool
      IgnoreSpace: bool
      IgnoreCase: bool }

[<assembly: InternalsVisibleTo("nex-vcs.tests")>]
do ()

module internal DiffEngine =

    /// Converts a text into an array of integers by assigning a unique code
    /// to each unique line. Options allow trimming, collapsing spaces, and ignoring case.
    let private diffCodes (aText: string) (h: Hashtable) (opts: DiffOpts) =
        // Normalise line endings.
        let aText = aText.Replace("\r", "")
        let lines = aText.Split('\n')
        let codes = Array.zeroCreate lines.Length
        let mutable lastUsedCode = h.Count

        for i in 0 .. lines.Length - 1 do
            let mutable s = lines[i]

            if opts.TrimSpace then
                s <- s.Trim()

            if opts.IgnoreSpace then
                s <- Regex.Replace(s, "\\s+", " ")

            if opts.IgnoreCase then
                s <- s.ToLower()

            if not (h.ContainsKey(s)) then
                lastUsedCode <- lastUsedCode + 1
                h.Add(s, lastUsedCode)
                codes[i] <- lastUsedCode
            else
                codes[i] <- h[s] :?> int

        codes

    /// Optimises a DiffData by shifting modified flags for better readability.
    let private optimise (data: DiffData) =
        let mutable startPos = 0

        while startPos < data.Length do
            while startPos < data.Length && not data.Modified[startPos] do
                startPos <- startPos + 1

            let mutable endPos = startPos

            while endPos < data.Length && data.Modified[endPos] do
                endPos <- endPos + 1

            if endPos < data.Length && data.Data[startPos] = data.Data[endPos] then
                data.Modified[startPos] <- false
                data.Modified[endPos] <- true
            else
                startPos <- endPos

    /// <summary>
    /// An implementation of the shortest middle snake algo
    /// </summary>
    /// <remarks>
    /// This code uses mutability to improve performance
    /// </remarks>
    /// <param name="dataA"></param>
    /// <param name="lowerA"></param>
    /// <param name="upperA"></param>
    /// <param name="dataB"></param>
    /// <param name="lowerB"></param>
    /// <param name="upperB"></param>
    /// <param name="downVector"></param>
    /// <param name="upVector"></param>
    let private sms
        (dataA: DiffData)
        (lowerA: int)
        (upperA: int)
        (dataB: DiffData)
        (lowerB: int)
        (upperB: int)
        (downVector: int[])
        (upVector: int[])
        : SMSRT =
        let max = dataA.Length + dataB.Length + 1
        let downK = lowerA - lowerB
        let upK = upperA - upperB
        let delta = (upperA - lowerA) - (upperB - lowerB)
        let oddDelta = (delta &&& 1) <> 0
        let downOffset = max - downK
        let upOffset = max - upK
        let maxD = ((upperA - lowerA + upperB - lowerB) / 2) + 1

        downVector[downOffset + downK + 1] <- lowerA
        upVector[upOffset + upK - 1] <- upperA

        let mutable d = 0
        let mutable found = false
        let mutable result = Unchecked.defaultof<SMSRT>

        while d <= maxD && not found do
            // Extend forward path.
            for k in [ downK - d .. 2 .. downK + d ] do
                let x =
                    if k = downK - d then
                        downVector[downOffset + k + 1]
                    else
                        let candidate = downVector[downOffset + k - 1] + 1

                        if k < downK + d && downVector[downOffset + k + 1] >= candidate then
                            downVector[downOffset + k + 1]
                        else
                            candidate

                let mutable xLocal = x
                let mutable y = xLocal - k

                while xLocal < upperA && y < upperB && dataA.Data[xLocal] = dataB.Data[y] do
                    xLocal <- xLocal + 1
                    y <- y + 1

                downVector[downOffset + k] <- xLocal

                if oddDelta && k > upK - d && k < upK + d then
                    if upVector[upOffset + k] <= downVector[downOffset + k] then
                        result <-
                            { x = downVector[downOffset + k]
                              y = downVector[downOffset + k] - k }

                        found <- true

            if not found then
                // Extend reverse path.
                for k in [ upK - d .. 2 .. upK + d ] do
                    let x =
                        if k = upK + d then
                            upVector[upOffset + k - 1]
                        else
                            let candidate = upVector[upOffset + k + 1] - 1

                            if k > upK - d && upVector[upOffset + k - 1] < candidate then
                                upVector[upOffset + k - 1]
                            else
                                candidate

                    let mutable xLocal = x
                    let mutable y = xLocal - k

                    while xLocal > lowerA && y > lowerB && dataA.Data[xLocal - 1] = dataB.Data[y - 1] do
                        xLocal <- xLocal - 1
                        y <- y - 1

                    upVector[upOffset + k] <- xLocal

                    if (not oddDelta) && k >= downK - d && k <= downK + d then
                        if upVector[upOffset + k] <= downVector[downOffset + k] then
                            result <-
                                { x = downVector[downOffset + k]
                                  y = downVector[downOffset + k] - k }

                            found <- true

            d <- d + 1

        if found then
            result
        else
            failwith "No shortest middle snake could be found"

    /// <summary>
    /// A recursive implementation of the LCS algo
    /// </summary>
    /// <remarks>
    /// This code makes use of mutability for performance
    /// </remarks>
    /// <param name="dataA"></param>
    /// <param name="lowerA"></param>
    /// <param name="upperA"></param>
    /// <param name="dataB"></param>
    /// <param name="lowerB"></param>
    /// <param name="upperB"></param>
    /// <param name="downVector"></param>
    /// <param name="upVector"></param>
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

        while lA < uA && lB < uB && dataA.Data[lA] = dataB.Data[lB] do
            lA <- lA + 1
            lB <- lB + 1

        while lA < uA && lB < uB && dataA.Data[uA - 1] = dataB.Data[uB - 1] do
            uA <- uA - 1
            uB <- uB - 1

        if lA = uA then
            for i in lB .. uB - 1 do
                dataB.Modified[i] <- true
        elif lB = uB then
            for i in lA .. uA - 1 do
                dataA.Modified[i] <- true
        else
            let smsData = sms dataA lA uA dataB lB uB downVector upVector
            lcs dataA lA smsData.x dataB lB smsData.y downVector upVector
            lcs dataA smsData.x uA dataB smsData.y uB downVector upVector


    /// <summary>
    /// Function to generate diffs from DiffData objects
    /// </summary>
    /// <param name="dataA"></param>
    /// <param name="dataB"></param>
    let private createDiffs (dataA: DiffData) (dataB: DiffData) : DiffItem list =
        let mutable lineA = 0
        let mutable lineB = 0
        let diffs = System.Collections.Generic.List<DiffItem>()

        while lineA < dataA.Length || lineB < dataB.Length do
            if
                lineA < dataA.Length
                && lineB < dataB.Length
                && not dataA.Modified[lineA]
                && not dataB.Modified[lineB]
            then
                lineA <- lineA + 1
                lineB <- lineB + 1
            else
                let startA = lineA
                let startB = lineB

                while lineA < dataA.Length && (lineB >= dataB.Length || dataA.Modified[lineA]) do
                    lineA <- lineA + 1

                while lineB < dataB.Length && (lineA >= dataA.Length || dataB.Modified[lineB]) do
                    lineB <- lineB + 1

                if startA < lineA || startB < lineB then
                    diffs.Add(
                        { StartA = startA
                          StartB = startB
                          deletedA = lineA - startA
                          insertedB = lineB - startB }
                    )

        diffs |> Seq.toList

    /// <summary>
    /// Main private diffText function with overridable options
    /// </summary>
    /// <param name="textA"></param>
    /// <param name="textB"></param>
    /// <param name="opts"></param>
    let private diffTextOpts (textA: string) (textB: string) (opts: DiffOpts) : DiffItem list =
        let h = Hashtable(textA.Length + textB.Length)
        let codesA = diffCodes textA h opts
        let codesB = diffCodes textB h opts
        h.Clear()
        let dataA = DiffData(codesA)
        let dataB = DiffData(codesB)
        let max = dataA.Length + dataB.Length + 1
        let downVector = Array.zeroCreate (2 * max + 2)
        let upVector = Array.zeroCreate (2 * max + 2)
        lcs dataA 0 dataA.Length dataB 0 dataB.Length downVector upVector
        optimise dataA
        optimise dataB
        createDiffs dataA dataB

    /// <summary>
    /// Main internal api for diffing text with case and space sensitivity
    /// This version returns the summary as a list of DiffItems
    /// </summary>
    /// <param name="textA"></param>
    /// <param name="textB"></param>
    let diffTextToSummary (textA: string) (textB: string) : DiffItem list =
        diffTextOpts
            textA
            textB
            { TrimSpace = false
              IgnoreSpace = false
              IgnoreCase = false }



    /// <summary>
    /// The main internal api for outputting hunk based changes as lists of DiffHunk types
    /// </summary>
    /// <param name="textA"></param>
    /// <param name="textB"></param>
    let diffTextToHunks (textA: string) (textB: string) : DiffHunk list =
        let contextSize = 3 //Defaults to showing 3 lines of context
        let linesA = textA.Replace("\r", "").Split('\n')
        let linesB = textB.Replace("\r", "").Split('\n')
        let diffs = diffTextToSummary textA textB

        let rec processDiff (remainingDiffs: DiffItem list) : DiffHunk list =
            match remainingDiffs with
            | [] -> []
            | diff :: rest ->
                // Calculate context ranges
                let contextStart = max 0 (diff.StartA - contextSize)

                let contextEnd =
                    min (linesA.Length - 1) (diff.StartA + diff.deletedA + contextSize - 1)

                let changes =
                    [
                      // Context before
                      yield! [ contextStart .. diff.StartA - 1 ] |> List.map (fun i -> Context linesA[i])
                      // Removed lines
                      yield!
                          [ diff.StartA .. diff.StartA + diff.deletedA - 1 ]
                          |> List.map (fun i -> Removed linesA[i])
                      // Added lines
                      yield!
                          [ diff.StartB .. diff.StartB + diff.insertedB - 1 ]
                          |> List.map (fun i -> Added linesB[i])
                      // Context after
                      yield!
                          [ diff.StartA + diff.deletedA .. contextEnd ]
                          |> List.map (fun i -> Context linesA[i]) ]

                let hunk =
                    { StartLineA = contextStart + 1 // 1-based line numbers
                      LinesA = contextEnd - contextStart + 1
                      StartLineB = (diff.StartB - (diff.StartA - contextStart)) + 1
                      LinesB = changes |> List.length
                      Lines = changes }

                hunk :: processDiff rest

        processDiff diffs
