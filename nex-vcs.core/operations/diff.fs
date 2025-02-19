namespace Nex.Core

open System
open System.IO
open Newtonsoft.Json
open Nex.Core.Types
open Nex.Core.Utils
open Nex.Core.Utils.Directories
open Nex.Core.Utils.Hashing

module Diff =

    /// Reads the .nexignore file (if present) from the working directory.
    let getIgnorePatterns (workingDir: string) : string list =
        let ignoreFile = Path.Combine(workingDir, ".nexignore")

        if File.Exists(ignoreFile) then
            File.ReadAllLines(ignoreFile) |> Array.toList
        else
            []

    /// Returns true if the file should be ignored based on a list of patterns.
    let shouldIgnore (filePath: string) (patterns: string list) =
        // You can improve this matching logic with proper pattern matching (e.g. globbing).
        patterns |> List.exists filePath.Contains

    /// Recursively get all file paths in a directory (relative to the root) that are not ignored.
    let rec getFilePaths (root: string) (currentDir: string) (ignore: string list) : string list =
        let files =
            Directory.GetFiles(currentDir)
            |> Array.toList
            |> List.map (fun f -> Path.GetRelativePath(root, f))
            |> List.filter (fun f -> not (shouldIgnore f ignore))

        let subdirs =
            Directory.GetDirectories(currentDir)
            |> Array.toList
            |> List.filter (fun d -> not (shouldIgnore (Path.GetFileName(d)) ignore))

        let subFiles = subdirs |> List.collect (fun d -> getFilePaths root d ignore)
        files @ subFiles

    /// Reads the HEAD commit from the repository (if present).
    let getHeadCommit (repoPath: string) : CommitObj option =
        let headPath = Path.Combine(repoPath, "refs", "HEAD")

        if File.Exists(headPath) then
            let commitHash = File.ReadAllText(headPath).Trim()
            printfn $"{commitHash}"
            if String.IsNullOrEmpty(commitHash) then
                None
            else
                let commitFilePath = Path.Combine(repoPath, "objects", commitHash)

                if File.Exists(commitFilePath) then
                    let json = File.ReadAllText(commitFilePath)
                    Some(JsonConvert.DeserializeObject<CommitObj>(json))
                else
                    None
        else
            None

    /// Computes a snapshot of the current working directory as a list of (relativePath, blobHash).
    let getCurrentSnapshot (workingDir: string) (ignore: string list) =
        let filePaths = getFilePaths workingDir workingDir ignore

        filePaths
        |> List.map (fun file ->
            let fullPath = Path.Combine(workingDir, file)
            let content = File.ReadAllBytes(fullPath)
            let blobHash = computeBlobHash content
            (file, blobHash))

    /// Computes the diff between the current working directory snapshot and the commit snapshot.
    let diff (repoPath: string) (workingDir: string) =
        let ignore = getIgnorePatterns workingDir
        let currentSnapshot = getCurrentSnapshot workingDir ignore
        // Build a map from the current snapshot.
        let currentMap = currentSnapshot |> Map.ofList

        // Get the snapshot from the HEAD commit.
        match getHeadCommit repoPath with
        | None -> printfn "No HEAD commit found. Displaying all files."
        | Some commit ->
            let commitMap = commit.files |> List.map (fun fe -> fe.path, fe.hash) |> Map.ofList

            // Files in the working directory that are not in the commit.
            let newFiles =
                currentMap
                |> Map.filter (fun file _ -> not (Map.containsKey file commitMap))
                |> Map.toList
                |> List.map fst

            // Files in both, but with different hashes.
            let modifiedFiles =
                currentMap
                |> Map.toList
                |> List.choose (fun (file, hash) ->
                    match Map.tryFind file commitMap with
                    | Some commitHash when commitHash <> hash -> Some file
                    | _ -> None)

            // Files that are in the commit but not in the working directory.
            let removedFiles =
                commitMap
                |> Map.filter (fun file _ -> not (Map.containsKey file currentMap))
                |> Map.toList
                |> List.map fst

            if List.isEmpty newFiles && List.isEmpty modifiedFiles && List.isEmpty removedFiles then
                printfn "No changes detected."
            else
                if not (List.isEmpty newFiles) then
                    printfn "New files:"
                    newFiles |> List.iter (printfn "  %s")

                if not (List.isEmpty modifiedFiles) then
                    printfn "Modified files:"
                    modifiedFiles |> List.iter (printfn "  %s")

                if not (List.isEmpty removedFiles) then
                    printfn "Removed files:"
                    removedFiles |> List.iter (printfn "  %s")

    let diffAll =
        let workingDir = ConfigParser.getWorkingDirectory ()
        match tryGetNexRepoPath () with
        | Some repoDir -> diff repoDir workingDir
        | None -> failwith "No nex repo could be found"
