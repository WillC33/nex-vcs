namespace Nex.Core

open System
open System.IO
open Nex.Core.DiffEngine
open Nex.Core.Utils.Config
open Nex.Core.Utils.Directories

module DiffCore =

    /// We will always ignore these...
    let private standardIgnores = [ ".nex"; ".nexlink" ]

    /// <summary>
    /// Retrieves the set of files and folders to ignore from .nexignore or defaults
    /// </summary>
    /// <param name="workingDir"></param>
    let private getIgnoredFiles (workingDir: string) =
        let nexignorePath = Path.Combine(workingDir, ".nexignore")

        match File.Exists(nexignorePath) with
        | true ->
            File.ReadAllLines(nexignorePath)
            |> Array.toList
            |> List.append standardIgnores
            |> List.distinct
        | false -> standardIgnores

    /// <summary>
    /// Fetches the content from the HEAD commit
    /// </summary>
    /// <param name="filePath"></param>
    let private getCommittedContent (filePath: string) =
        match tryGetNexRepoPath () with
        | None -> ""
        | Some repoPath ->
            let headPath = Path.Combine(repoPath, "HEAD")

            if not (File.Exists(headPath)) then
                ""
            else
                let commitHash = File.ReadAllText(headPath).Trim()

                if commitHash = "" then
                    ""
                else
                    let objectPath = Path.Combine(repoPath, "objects", commitHash)
                    let fileObjectPath = Path.Combine(objectPath, filePath)

                    if File.Exists(fileObjectPath) then
                        File.ReadAllText(fileObjectPath)
                    else
                        ""

    /// <summary>
    /// Checks against the .nexignore for matching patterns to exclude a file/directory
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="path"></param>
    let private matchesIgnorePattern (pattern: string) (path: string) =
        let normalisedPattern = pattern.TrimStart('/').TrimEnd('/')
        let isDirectory = pattern.EndsWith('/')

        let createRegex (p: string) =
            let pattern =
                "^"
                + p.Replace(".", "\\.").Replace("*", ".*")
                + (if isDirectory then "(/.*)?$" else "$")

            System.Text.RegularExpressions.Regex(pattern)

        let matchDirectory (p: string) =
            let separator = Path.DirectorySeparatorChar.ToString()
            path.StartsWith(p + separator) || path = p

        match normalisedPattern.Contains("*") with
        | true -> createRegex normalisedPattern |> _.IsMatch(path)
        | false ->
            if isDirectory then
                matchDirectory normalisedPattern
            else
                path.Equals(normalisedPattern, StringComparison.OrdinalIgnoreCase)

    /// <summary>
    /// Recursively traverses the working directory for necessary files
    /// </summary>
    let private findFiles (baseDir: string) (ignoreList: string list) : string list =
        let rec traverse dir =
            let relativePath (path: string) : string =
                path
                    .Replace(baseDir, "")
                    .TrimStart(Path.DirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '/')

            Directory.GetFiles(dir)
            |> Array.filter (fun f ->
                let relPath = relativePath f
                not (List.exists (fun ignore -> matchesIgnorePattern ignore relPath) ignoreList))
            |> Array.toList
            |> List.append (
                Directory.GetDirectories(dir)
                |> Array.filter (fun d ->
                    let relPath = relativePath d
                    not (List.exists (fun ignore -> matchesIgnorePattern ignore relPath) ignoreList))
                |> Array.collect (fun d -> traverse d |> List.toArray)
                |> Array.toList
            )

        traverse baseDir

    /// <summary>
    /// Provides a hunk diff of a given file
    /// </summary>
    /// <param name="filePath"></param>
    let diffFile (filePath: string) =
        let currentContent =
            if File.Exists(filePath) then
                File.ReadAllText(filePath)
            else
                "" //TODO this will need to be an error FileNotFound

        let committedContent = getCommittedContent filePath
        diffTextToHunks committedContent currentContent

    /// <summary>
    /// Provides a summary diff for the working directory of the nex repository
    /// </summary>
    let diffWorkingDirectory () =
        let workingDir = getWorkingDirectory ()

        if not (Directory.Exists(workingDir)) then
            []
        else
            let ignores = getIgnoredFiles workingDir
            let files = findFiles workingDir ignores

            files
            |> List.map (fun file ->
                let relativePath =
                    file
                        .Replace(workingDir, "")
                        .TrimStart(Path.DirectorySeparatorChar)
                        .Replace(Path.DirectorySeparatorChar, '/')

                let currentContent = File.ReadAllText(file)
                let committedContent = getCommittedContent relativePath
                relativePath, diffTextToSummary committedContent currentContent)
            |> List.filter (fun (_, diffs) -> not (List.isEmpty diffs))
