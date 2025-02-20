module Nex.Cli.DiffCli

open System
open Nex.Core.Types
open WriterUI

/// <summary>
/// Displays a summary of changes for a set of files
/// </summary>
/// <param name="fileDiffs">List of file paths and their corresponding diff summaries</param>
let displaySummaryDiffs (fileDiffs: (string * DiffItem list) list) =
    if List.isEmpty fileDiffs then
        Writer.Message("No changes detected", ConsoleColor.Green)
    else
        fileDiffs
        |> List.iter (fun (path, diffs) ->
            Writer.Message($"File: {path}", ConsoleColor.White)

            diffs
            |> List.iter (fun diff ->
                let changeType =
                    match diff.deletedA, diff.insertedB with
                    | 0, n -> $"Added {n} lines"
                    | n, 0 -> $"Removed {n} lines"
                    | n, m -> $"Changed {n} lines to {m} lines"

                Writer.Message($"  {changeType} at line {diff.StartA + 1}", ConsoleColor.DarkYellow)))

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
