namespace Nex.Core

open System
open System.IO
open Nex.Core.DiffCore
open Nex.Core.Types
open Nex.Core.Utils.Config
open Nex.Core.Utils.Hashing
open Nex.Core.Utils.Serialisation



module StageCore =
    [<CLIMutable>]
    type private StagedEntry = { FilePath: string; Hash: string }

    let private compareHashes (newHash: string) (committedHash: string option) =
        match committedHash with
        | Some hash -> hash = newHash
        | None -> false

    let private resolveFilePath (workingDir: string) (target: string) =
        if Path.IsPathRooted(target) then
            target
        else
            Path.Combine(workingDir, target)

    // IO operations
    let private stagingFile (workingDir: string) =
        Path.Combine(workingDir, ".nex", "stage.bson")

    let private loadStagingArea (workingDir: string) : Result<StagedEntry list, string> =
        try
            let file = stagingFile workingDir
            if File.Exists(file) then readBson file |> Ok else Ok []
        with ex ->
            Error ex.Message

    let private saveStagingArea (workingDir: string) (entries: StagedEntry list) : Result<unit, string> =
        try
            let file = stagingFile workingDir
            let dir = Path.GetDirectoryName(file)

            if not (Directory.Exists(dir)) then
                Directory.CreateDirectory(dir) |> ignore

            writeBson file entries
            Ok()
        with ex ->
            Error ex.Message

    let private readFileContent (path: string) : Result<byte[], string> =
        try
            File.ReadAllBytes path |> Ok
        with ex ->
            Error $"Failed to read file: {ex.Message}"

    let checkFileChanged (workingDir: string) (target: string) : Result<string option, string> =
        let absoluteFile = resolveFilePath workingDir target

        if not (File.Exists absoluteFile) then
            Ok None
        else
            readFileContent absoluteFile
            |> Result.map (fun content ->
                let newHash = computeBlobHash content
                let committedContent = getCommittedContent absoluteFile

                let committedHash =
                    if String.IsNullOrWhiteSpace committedContent then
                        None
                    else
                        committedContent
                        |> System.Text.Encoding.UTF8.GetBytes
                        |> computeBlobHash
                        |> Some

                if compareHashes newHash committedHash then
                    None
                else
                    Some newHash)

    let private collectFiles (baseDir: string) : Result<string list, string> =
        try
            let rec getFiles dir =
                seq {
                    yield! Directory.GetFiles(dir)

                    for subDir in Directory.GetDirectories(dir) do
                        yield! getFiles subDir
                }

            getFiles baseDir |> Seq.toList |> Ok
        with ex ->
            Error $"Failed to collect files: {ex.Message}"

    let private writeToStaging workingDir (entries: StagedEntry list) =
        loadStagingArea workingDir
        |> Result.bind (fun existing ->
            existing
            |> List.filter (fun e -> not (List.exists (fun n -> n.FilePath = e.FilePath) entries))
            |> List.append entries
            |> saveStagingArea workingDir)
        |> ignore

    let private stageFile (workingDir: string) (target: string) : StageAction =
        let changed = checkFileChanged workingDir target

        match changed with
        | Ok(Some hash) ->
            let relativePath = Path.GetRelativePath(workingDir, target)
            let entry = [ { FilePath = relativePath; Hash = hash } ]
            writeToStaging workingDir entry
            StageAction.Staged
        | Ok None -> StageAction.Unchanged
        | Error _ -> StageAction.NotFound


    let private stageFolder (workingDir: string) (folder: string) : Result<StageAction, StageAction> =
        let absoluteFolder = resolveFilePath workingDir folder

        collectFiles absoluteFolder
        |> Result.map (
            List.choose (fun file ->
                match checkFileChanged workingDir file with
                | Ok(Some hash) ->
                    Some
                        { FilePath = Path.GetRelativePath(workingDir, file)
                          Hash = hash }
                | _ -> None)
        )
        |> Result.bind (function
            | [] -> Ok StageAction.Unchanged
            | entries -> writeToStaging workingDir entries |> fun _ -> Ok StageAction.Staged)
        |> Result.mapError (fun _ -> StageAction.NotFound)


    let stage (path: string) : Result<StageAction, StageAction> =
        let workingDir = getWorkingDirectory ()

        if String.IsNullOrWhiteSpace path then
            Error StageAction.NotFound
        else
            let absolutePath = resolveFilePath workingDir path

            if Directory.Exists absolutePath then
                stageFolder workingDir absolutePath
                |> Result.mapError (fun _ -> StageAction.NotFound)
            elif File.Exists absolutePath then
                stageFile workingDir absolutePath |> Ok
            else
                Error StageAction.NotFound
