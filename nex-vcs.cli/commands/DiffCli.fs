module Nex.Cli.DiffCli

open System
open Nex.Core.Types
open Nex.Core.Utils.Locale
open Nex.Core.Utils.Locale.Tr
open WriterUI

let addedMessage =
    message (
        Some
            { CustomColor = Some ConsoleColor.Black
              BackgroundColor = Some ConsoleColor.Green
              IncludeLineSpace = false }
    )

let removedMessage =
    message (
        Some
            { CustomColor = Some ConsoleColor.White
              BackgroundColor = Some ConsoleColor.DarkRed
              IncludeLineSpace = false }
    )

/// <summary>
/// Displays a summary of changes for a set of files
/// </summary>
/// <param name="fileDiffs">List of file paths and their corresponding diff summaries</param>
let displaySummaryDiffs (fileDiffs: (string * DiffItem list) list) =
    if List.isEmpty fileDiffs then
        getLocalisedMessage None (DiffResponse NoChanges) |> addedMessage
    else
        fileDiffs
        |> List.iter (fun (path, diffs) ->
            message None $"File: {path}"

            diffs
            |> List.iter (fun diff ->
                let changeType =
                    match diff.deletedA, diff.insertedB with
                    | 0, n -> getLocalisedMessage (Some(n.ToString())) (DiffResponse AddedNLines)
                    | n, 0 -> getLocalisedMessage (Some(n.ToString())) (DiffResponse DeletedNLines)
                    | n, _ -> getLocalisedMessage (Some(n.ToString())) (DiffResponse ChangedNLines)

                message None $"  {changeType} @ {diff.StartA + 1}"))

/// <summary>
/// Displays detailed hunk changes for a single file
/// </summary>
/// <param name="path">File path</param>
/// <param name="hunks">List of diff hunks</param>
let displayHunkDiffs (path: string) (hunks: DiffHunk list) =
    getLocalisedMessage (Some path) (DiffResponse FileDiffResult_FileName)
    |> message None

    if List.isEmpty hunks then
        message
            (Some
                { defaultOptions with
                    CustomColor = Some ConsoleColor.Green })
            "  No changes"
    else
        hunks
        |> List.iter (fun hunk ->
            message
                (Some
                    { defaultOptions with
                        CustomColor = Some ConsoleColor.Cyan })
                $"@@ -{hunk.StartLineA},{hunk.LinesA} +{hunk.StartLineB},{hunk.LinesB} @@"

            hunk.Lines
            |> List.iter (fun line ->
                match line with
                | Context text ->
                    message
                        (Some
                            { defaultOptions with
                                CustomColor = Some ConsoleColor.Gray
                                IncludeLineSpace = false })
                        $" {text}"
                | Added text ->
                    message
                        (Some
                            { defaultOptions with
                                CustomColor = Some ConsoleColor.DarkGreen
                                IncludeLineSpace = false })
                        $"+{text}"
                | Removed text ->
                    message
                        (Some
                            { defaultOptions with
                                CustomColor = Some ConsoleColor.DarkRed
                                IncludeLineSpace = false })
                        $"-{text}"))
