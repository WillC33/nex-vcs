namespace Nex.Core

open System
open System.IO
open Newtonsoft.Json
open Nex.Core.Types
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
    /// Helper function to fetch the commit hash of HEAD
    /// </summary>
    /// <param name="repoPath"></param>
    let private tryGetHeadCommitHash (repoPath: string) =
        let headPath = Path.Combine(repoPath, "refs/HEAD")

        if File.Exists(headPath) then
            let hash = File.ReadAllText(headPath).Trim()
            if String.IsNullOrEmpty(hash) then None else Some hash
        else
            None

    /// <summary>
    /// Helper to read a commit object
    /// </summary>
    /// <param name="repoPath"></param>
    /// <param name="commitHash"></param>
    let private tryReadCommitObject (repoPath: string) (commitHash: string) =
        let objectPath = Path.Combine(repoPath, "objects", commitHash)

        if File.Exists(objectPath) then
            try
                let commitJson = File.ReadAllText(objectPath)
                Some(JsonConvert.DeserializeObject<CommitObj>(commitJson))
            with _ ->
                None
        else
            None

    /// <summary>
    /// Helper to read blob content
    /// </summary>
    /// <param name="repoPath"></param>
    /// <param name="fileEntry"></param>
    let private tryReadBlobContent (repoPath: string) (fileEntry: FileEntry) =
        let blobPath = Path.Combine(repoPath, "objects", fileEntry.hash)

        if File.Exists(blobPath) then
            Some(File.ReadAllText(blobPath))
        else
            None

    /// <summary>
    /// Fetches the commited content at HEAD for a given file in the working directory
    /// </summary>
    /// <param name="filePath"></param>
    let private getCommittedContent (filePath: string) =
        let relativeFilePath = Path.Combine(getWorkingDirectory (), filePath)

        match tryGetNexRepoPath () with
        | None -> ""
        | Some repoPath ->
            match tryGetHeadCommitHash repoPath with
            | None -> ""
            | Some commitHash ->
                tryReadCommitObject repoPath commitHash
                |> Option.bind (fun commit ->
                    commit.files
                    |> List.tryFind (fun f -> f.path = relativeFilePath)
                    |> Option.bind (tryReadBlobContent repoPath))
                |> Option.defaultValue ""

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
    /// <param name="relativeFilePath"></param>
    let diffFile (relativeFilePath: string) =
        let filePath = Path.Combine(getWorkingDirectory (), relativeFilePath)

        let currentContent =
            if File.Exists(filePath) then
                File.ReadAllText(filePath)
            else
                "" // Not yet indexed by nex

        let committedContent = getCommittedContent filePath
match committedContent with
| "" -> // Treat new files as a single hunk with all lines added
    [ { StartLineA = 0
        StartLineB = 0
        LinesA = 0
        LinesB = currentContent.Split('\n').Length
        Lines = currentContent.Split('\n') |> Array.toList |> List.map Added } ]
| _ -> diffTextToHunks committedContent currentContent

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
