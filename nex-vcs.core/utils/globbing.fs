namespace Nex.Core.Utils

open System

module Globbing =
    // Pattern matching for single characters
    let (|DirSeparator|_|) (c: char) =
        if c = '/' || c = '\\' then Some() else None

    // Helper function for readability
    let isDirSeparator (c: char) = c = '/' || c = '\\'

    // Pattern matching active patterns for glob syntax
    let (|Star|_|) (pattern: string) =
        if pattern.StartsWith("*") && not (pattern.StartsWith("**")) then
            Some(pattern.Substring(1))
        else
            None

    let (|DoubleStar|_|) (pattern: string) =
        if pattern.StartsWith("**") then
            Some(pattern.Substring(2))
        else
            None

    let (|Question|_|) (pattern: string) =
        if pattern.StartsWith("?") then
            Some(pattern.Substring(1))
        else
            None

    let (|CharSet|_|) (pattern: string) =
        if pattern.StartsWith("[") then
            let closingIndex = pattern.IndexOf(']')

            if closingIndex > 0 then
                let charSet = pattern.Substring(1, closingIndex - 1)
                Some(charSet, pattern.Substring(closingIndex + 1))
            else
                None
        else
            None

    let private matchCharRange (charList: char list) (c: char) =
        charList |> List.exists (fun el -> el = c)

    let private charIsNot (pattern: char) (target: char) = pattern <> target

    let private getCharRange (charSet: string) =
        let rec processChars index chars =
            match index with
            | i when i >= charSet.Length -> List.rev chars
            | i when i + 2 < charSet.Length && charSet[i + 1] = '-' ->
                let start, endChar = charSet[i], charSet[i + 2]
                let rangeChars = [ int start .. int endChar ] |> List.map char
                processChars (i + 3) (List.append rangeChars chars)
            | i -> processChars (i + 1) (charSet[i] :: chars)

        processChars 0 []


    let (|BraceSet|_|) (pattern: string) =
        if pattern.StartsWith("{") then
            let closingIndex = pattern.IndexOf('}')

            if closingIndex > 0 then
                let charSet = pattern.Substring(1, closingIndex - 1)
                Some(charSet, pattern.Substring(closingIndex + 1))
            else
                None
        else
            None

    let (|PathSeparator|_|) (pattern: string) =
        if not (String.IsNullOrEmpty pattern) && (pattern[0] = '/' || pattern[0] = '\\') then
            Some(pattern.Substring(1))
        else
            None

    let (|Literal|_|) (pattern: string) =
        if not (String.IsNullOrEmpty pattern) && not ("*?[/\\".Contains(pattern[0])) then
            let nextSpecialChar =
                [| "*"; "?"; "["; "/"; "\\" |]
                |> Array.map (fun c -> pattern.IndexOf(c))
                |> Array.filter (fun i -> i >= 0)
                |> Array.sortWith (fun a b -> a.CompareTo(b))
                |> Array.tryHead

            match nextSpecialChar with
            | Some index when index > 0 ->
                // Take the chunk of literal text up to the next special character
                Some(pattern.Substring(0, index), pattern.Substring(index))
            | _ ->
                // Take the entire remaining string as a literal
                Some(pattern, "")
        else
            None


    type WorkItem = { Pattern: string; Input: string }

    /// <summary>
    /// Tail-recursive implementation of glob pattern matching
    /// </summary>
    /// <param name="pattern">The glob pattern</param>
    /// <param name="input">The input string</param>
    let isMatch (pattern: string) (input: string) =

        // Helper that manages the explicit stack
        let rec matchGlobTailRec (workItems: WorkItem list) =
            match workItems with
            | [] -> false // Empty work list means no successful matches
            | { Pattern = ""; Input = "" } :: _ -> true // Empty pattern matches empty input - success!
            | { Pattern = ""; Input = _ } :: rest -> matchGlobTailRec rest // Empty pattern doesn't match non-empty input
            | { Pattern = p; Input = "" } :: rest when p = "**" || p = "**/" -> true // ** can match empty strings - success!
            | { Pattern = p; Input = _ } :: rest when p = "*" -> true // * can match empty strings - success!
            | { Pattern = p; Input = "" } :: rest when p.StartsWith("*") ->
                // Try consuming the * and continue
                matchGlobTailRec ({ Pattern = p.Substring(1); Input = "" } :: rest)
            | { Pattern = _; Input = "" } :: rest -> matchGlobTailRec rest // Non-empty pattern doesn't match empty input

            // Double-star pattern
            | { Pattern = p; Input = i } :: rest when p.StartsWith("**") ->
                let restPattern = p.Substring(2)
                // Try both: consume ** or consume one char from input
                matchGlobTailRec (
                    { Pattern = restPattern; Input = i }
                    :: (if i.Length > 0 then
                            { Pattern = p; Input = i.Substring(1) } :: rest
                        else
                            rest)
                )

            // Special case for **/
            | { Pattern = p; Input = i } :: rest when p.StartsWith("**/") ->
                let restPattern = p.Substring(3)
                // Try both: consume **/ or consume one char from input
                matchGlobTailRec (
                    { Pattern = restPattern; Input = i }
                    :: (if i.Length > 0 then
                            { Pattern = p; Input = i.Substring(1) } :: rest
                        else
                            rest)
                )

            // Single-star pattern
            | { Pattern = p; Input = i } :: rest when p.StartsWith("*") && not (p.StartsWith("**")) ->
                let restPattern = p.Substring(1)
                // Try both: consume * or consume one non-separator char
                matchGlobTailRec (
                    { Pattern = restPattern; Input = i }
                    :: (if i.Length > 0 && not (isDirSeparator i[0]) then
                            { Pattern = p; Input = i.Substring(1) } :: rest
                        else
                            rest)
                )

            // Question mark
            | { Pattern = p; Input = i } :: rest when p.StartsWith("?") && i.Length > 0 && not (isDirSeparator i[0]) ->
                matchGlobTailRec (
                    { Pattern = p.Substring(1)
                      Input = i.Substring(1) }
                    :: rest
                )

            // Path separator
            | { Pattern = p; Input = i } :: rest when
                p.StartsWith("/")
                || p.StartsWith("\\") && i.Length > 0 && (i[0] = '/' || i[0] = '\\')
                ->
                matchGlobTailRec (
                    { Pattern = p.Substring(1)
                      Input = i.Substring(1) }
                    :: rest
                )

            // Character set
            | { Pattern = p; Input = i } :: rest when p.StartsWith("[") && i.Length > 0 ->
                let closingIndex = p.IndexOf(']')
                //TODO: This needs a little more work for handling negations, see broken tests

                if closingIndex > 0 then
                    let charSet = p.Substring(1, closingIndex - 1)
                    let restPattern = p.Substring(closingIndex + 1)
                    let negate = charSet.StartsWith("!")
                    let actualSet = if negate then charSet.Substring(1) else charSet

                    let isMatch =
                        if negate then
                            not <| charIsNot actualSet[0] i[0]
                        else
                            let charList = getCharRange charSet
                            matchCharRange charList i[0]

                    if (negate && not isMatch) || (not negate && isMatch) then
                        matchGlobTailRec (
                            { Pattern = restPattern
                              Input = i.Substring(1) }
                            :: rest
                        )
                    else
                        matchGlobTailRec rest
                else
                    matchGlobTailRec rest

            // Literal chunk
            | { Pattern = p; Input = i } :: rest when not (String.IsNullOrEmpty p) && not ("*?[/\\".Contains(p[0])) ->
                // Find next special character
                let nextSpecialIndices =
                    [| "*"; "?"; "["; "/"; "\\" |]
                    |> Array.map (fun c -> p.IndexOf(c))
                    |> Array.filter (fun idx -> idx >= 0)

                let literalLength =
                    if nextSpecialIndices.Length > 0 then
                        Array.min nextSpecialIndices
                    else
                        p.Length

                if
                    literalLength > 0
                    && i.Length >= literalLength
                    && i
                        .Substring(0, literalLength)
                        .Equals(p.Substring(0, literalLength), StringComparison.OrdinalIgnoreCase)
                then
                    matchGlobTailRec (
                        { Pattern = p.Substring(literalLength)
                          Input = i.Substring(literalLength) }
                        :: rest
                    )
                else
                    matchGlobTailRec rest

            // No match for this work item, try next one
            | _ :: rest -> matchGlobTailRec rest

        // Start with a single work item
        matchGlobTailRec [ { Pattern = pattern; Input = input } ]
