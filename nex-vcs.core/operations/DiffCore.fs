namespace Nex.Core

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

        if File.Exists(nexignorePath) then
            let userIgnores = File.ReadAllLines(nexignorePath) |> Array.toList
            standardIgnores @ userIgnores |> List.distinct
        else
            standardIgnores

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
    /// Recursively traverses the working directory for necessary files
    /// </summary>
    /// <param name="baseDir"></param>
    /// <param name="ignoreList"></param>
    let private findFiles (baseDir: string) (ignoreList: string list) =
        let rec traverse dir =
            Directory.GetFiles(dir)
            |> Array.filter (fun f ->
                let name = Path.GetFileName(f)
                not (List.exists (fun (ignore: string) -> name.Contains(ignore)) ignoreList))
            |> Array.toList
            |> List.append (
                Directory.GetDirectories(dir)
                |> Array.filter (fun d ->
                    let name = Path.GetFileName(d)
                    not (List.exists (fun (ignore: string) -> name.Contains(ignore)) ignoreList))
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
        let ignores = getIgnoredFiles workingDir
        let files = findFiles workingDir ignores

        files
        |> List.map (fun file ->
            let relativePath =
                file.Replace(workingDir, "").TrimStart(Path.DirectorySeparatorChar)

            let currentContent = File.ReadAllText(file)
            let committedContent = getCommittedContent relativePath
            relativePath, diffTextToSummary committedContent currentContent)
        |> List.filter (fun (_, diffs) -> not (List.isEmpty diffs))
