namespace Nex.Core

open System
open System.IO
open Nex.Core.DiffCore
open Nex.Core.Utils.Hashing
open Nex.Core.Utils.Serialisation

module StageCore =

    /// Represents a file staged for commit.
    [<CLIMutable>]
    type StagedEntry = { FilePath: string; Hash: string }

    /// Computes the path of the staging file given a working directory.
    let stagingFile (workingDir: string) : string =
        Path.Combine(workingDir, ".nex", "stage.bson")

    /// Loads the staging area from disk, returning an empty list if none exists.
    let loadStagingArea (workingDir: string) : StagedEntry list =
        let file = stagingFile workingDir

        if File.Exists(file) then readBson file else []

    /// Saves the staging area to disk.
    let saveStagingArea (workingDir: string) (entries: StagedEntry list) : unit =
        let file = stagingFile workingDir
        let dir = Path.GetDirectoryName(file)

        if not (Directory.Exists(dir)) then
            Directory.CreateDirectory(dir) |> ignore

        writeBson file entries

    /// Checks whether the file at target (relative or absolute) is changed compared to the committed version.
    /// Returns Some newHash if changed, or None if unchanged.
    let checkFileChanged (workingDir: string) (target: string) : string option =
        let absoluteFile =
            if Path.IsPathRooted(target) then
                target
            else
                Path.Combine(workingDir, target)

        if not (File.Exists(absoluteFile)) then
            None
        else
            let content = File.ReadAllBytes(absoluteFile)
            let newHash = computeBlobHash content
            let committedContent = getCommittedContent absoluteFile

            let committedHash =
                if String.IsNullOrWhiteSpace(committedContent) then
                    None
                else
                    let bytes = System.Text.Encoding.UTF8.GetBytes(committedContent)

                    Some(
                        toHash (
                            Array.append
                                (System.Text.Encoding.UTF8.GetBytes(sprintf "blob %d\u0000" bytes.Length))
                                bytes
                        )
                    )

            match committedHash with
            | Some h when h = newHash -> None
            | _ -> Some newHash

    /// Stages a file (or directory, if you extend collectFiles) only if it has changed.
    /// Returns Ok () on success or Error with an explanatory message.
    let stageFile (workingDir: string) (target: string) : Result<unit, string> =
        match checkFileChanged workingDir target with
        | None -> Error(sprintf "File '%s' is unchanged; cannot stage." target)
        | Some newHash ->
            // Compute a relative path to the working directory.
            let relativePath =
                if Path.IsPathRooted(target) then
                    Path.GetRelativePath(workingDir, target)
                else
                    target

            let newEntry =
                { StagedEntry.FilePath = relativePath
                  Hash = newHash }

            let current = loadStagingArea workingDir
            // Replace any existing entry for the same file.
            let updated =
                newEntry :: (current |> List.filter (fun e -> e.FilePath <> relativePath))

            saveStagingArea workingDir updated
            Ok()

    /// (Placeholder) You can later add a function to stage an entire folder recursively.
    let stageFolder (workingDir: string) (folder: string) : Result<unit, string> =
        // This could use a recursive file-collection function, then map stageFile over the results.
        // For now, we simply return an error.
        Error "Recursive staging not implemented yet."
