module Nex.Cli.DiffCli

open System
open Nex.Core.Types
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
        addedMessage "No changes detected"
    else
        fileDiffs
        |> List.iter (fun (path, diffs) ->
            message None $"File: {path}"

            diffs
            |> List.iter (fun diff ->
                let changeType =
                    match diff.deletedA, diff.insertedB with
                    | 0, n -> $"Added {n} lines"
                    | n, 0 -> $"Removed {n} lines"
                    | n, m -> $"Changed {n} lines to {m} lines"

                message None $"  {changeType} at line {diff.StartA + 1}"))

/// <summary>
/// Displays detailed hunk changes for a single file
/// </summary>
/// <param name="path">File path</param>
/// <param name="hunks">List of diff hunks</param>
let displayHunkDiffs (path: string) (hunks: DiffHunk list) =
    message None $"File: {path}"

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
                                CustomColor = Some ConsoleColor.Gray })
                        $" {text}"
                | Added text ->
                    message
                        (Some
                            { defaultOptions with
                                CustomColor = Some ConsoleColor.Green })
                        $"+{text}"
                | Removed text ->
                    message
                        (Some
                            { defaultOptions with
                                CustomColor = Some ConsoleColor.Red })
                        $"-{text}"))

/// <summary>
/// Displays detailed hunk changes for a single file
/// </summary>
/// <param name="path">File path</param>
/// <param name="hunks">List of diff hunks</param>
let displayHunkDiffs (path: string) (hunks: DiffHunk list) =
    Writer.Message($"File: {path}", ConsoleColor.White)

    if List.isEmpty hunks then
        Writer.Message("  No changes", ConsoleColor.Green)
    else
        hunks
        |> List.iter (fun hunk ->
            Writer.Message(
                $"@@ -{hunk.StartLineA},{hunk.LinesA} +{hunk.StartLineB},{hunk.LinesB} @@",
                ConsoleColor.Cyan
            )

            hunk.Lines
            |> List.iter (fun line ->
                match line with
                | Context text -> Writer.Message($" {text}", ConsoleColor.Gray)
                | Added text -> Writer.Message($"+{text}", ConsoleColor.Green)
                | Removed text -> Writer.Message($"-{text}", ConsoleColor.Red)))
