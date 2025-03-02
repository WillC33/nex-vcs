namespace Nex.Core

open System
open System.IO
open Nex.Core.DiffCore
open Nex.Core.Utils.Hashing
open Nex.Core.Utils.Serialisation

type StageAction =
    | Staged
    | Unstaged
    | Unchanged
    | NotFound

module StageCore =
    [<CLIMutable>]
    type StagedEntry = { FilePath: string; Hash: string }

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
    let stagingFile (workingDir: string) =
        Path.Combine(workingDir, ".nex", "stage.bson")

    let loadStagingArea (workingDir: string) : Result<StagedEntry list, string> =
        try
            let file = stagingFile workingDir
            if File.Exists(file) then readBson file |> Ok else Ok []
        with ex ->
            Error ex.Message

    let saveStagingArea (workingDir: string) (entries: StagedEntry list) : Result<unit, string> =
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
                        committedContent |> System.Text.Encoding.UTF8.GetBytes |> computeBlobHash |> Some

                if compareHashes newHash committedHash then
                    None
                else
                    Some newHash)

    let stageFile (workingDir: string) (target: string) : StageAction =
        match checkFileChanged workingDir target with
        | Error _ -> StageAction.NotFound
        | Ok None -> StageAction.Unchanged
        | Ok(Some newHash) ->
            let relativePath =
                if Path.IsPathRooted(target) then
                    Path.GetRelativePath(workingDir, target)
                else
                    target

            let newEntry =
                { FilePath = relativePath
                  Hash = newHash }

            match loadStagingArea workingDir with
            | Error _ -> StageAction.NotFound
            | Ok current ->
                let updated =
                    newEntry :: (current |> List.filter (fun e -> e.FilePath <> relativePath))

                match saveStagingArea workingDir updated with
                | Ok _ -> StageAction.Staged
                | Error _ -> StageAction.NotFound

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

    let stageFolder (workingDir: string) (folder: string) : Result<StageAction, StageAction> =
        result {
            let absoluteFolder = resolveFilePath workingDir folder
            let! files = collectFiles absoluteFolder

            let stageResults =
                files |> List.map (fun file -> stageFile workingDir file) |> List.distinct

            return
                match stageResults with
                | [] -> StageAction.NotFound
                | results when List.contains StageAction.Staged results -> StageAction.Staged
                | results when List.forall ((=) StageAction.Unchanged) results -> StageAction.Unchanged
                | _ -> StageAction.NotFound
        }
        |> Result.mapError (fun _ -> StageAction.NotFound)

    let stage (workingDir: string) (path: string) : Result<StageAction, StageAction> =
        if String.IsNullOrWhiteSpace path then
            Error StageAction.NotFound
        else
            let absolutePath = resolveFilePath workingDir path

            if Directory.Exists absolutePath then
                stageFolder workingDir absolutePath
            elif File.Exists absolutePath then
                Ok(stageFile workingDir absolutePath)
            else
                Ok StageAction.NotFound
