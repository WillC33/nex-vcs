namespace Nex.Core

open System
open System.IO
open Nex.Core.DiffCore
open Nex.Core.Types
open Nex.Core.Utils.Config
open Nex.Core.Utils.FileResolver
open Nex.Core.Utils.Hashing
open Nex.Core.Utils.Serialisation



module StageCore =
    [<CLIMutable>]
    type StagedEntry = { FilePath: string; Hash: string }

    let private compareHashes (newHash: string) (committedHash: string option) =
        match committedHash with
        | Some hash -> hash = newHash
        | None -> false

    // IO operations
    let stagingFile (workingDir: string) =
        Path.Combine(workingDir, ".nex", "stage.bson")

    let loadStagingArea: Result<StagedEntry list, string> =
        let file = stagingFile <| getWorkingDirectory ()

        try
            if File.Exists(file) then readBson file |> Ok else Ok []
        with ex ->
            Error ex.Message

    let private saveStagingArea (entries: StagedEntry list) : Result<unit, string> =
        let file = stagingFile <| getWorkingDirectory ()

        try
            let dir = Path.GetDirectoryName(file)

            if not (Directory.Exists(dir)) then
                Directory.CreateDirectory(dir) |> ignore

            writeBson<StagedEntry[]> file (entries |> List.toArray)
            Ok()
        with ex ->
            Error ex.Message

    let private readFileContent (path: string) : Result<byte[], string> =
        try
            File.ReadAllBytes path |> Ok
        with ex ->
            Error $"Failed to read file: {ex.Message}"

    let checkFileChanged (target: string) : Result<string option, string> =
        if not (File.Exists target) then
            Ok None
        else
            readFileContent target
            |> Result.map (fun content ->
                let newHash = computeBlobHash content
                let committedContent = getCommittedContent target

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

    let private writeToStaging (entries: StagedEntry list) =
        let stage = loadStagingArea

        stage
        |> Result.bind (fun existing ->
            existing
            |> List.filter (fun e -> not (List.exists (fun n -> n.FilePath = e.FilePath) entries))
            |> List.append entries
            |> saveStagingArea)
        |> ignore

    let private stageFile (target: string) : StageAction =
        let { AbsolutePath = abs } = resolvePaths target
        let changed = checkFileChanged target

        match changed with
        | Ok(Some hash) ->
            let entry = [ { FilePath = abs; Hash = hash } ]
            writeToStaging entry
            StageAction.Staged
        | Ok None -> StageAction.Unchanged
        | Error _ -> StageAction.NotFound


    let private stageFolder (folder: string) : Result<StageAction, StageAction> =
        let { AbsolutePath = abs } = resolvePaths folder

        collectFiles abs
        |> Result.map (
            List.choose (fun file ->
                match checkFileChanged file with
                | Ok(Some hash) -> Some { FilePath = abs; Hash = hash }
                | _ -> None)
        )
        |> Result.bind (function
            | [] -> Ok StageAction.Unchanged
            | entries -> writeToStaging entries |> fun _ -> Ok StageAction.Staged)
        |> Result.mapError (fun _ -> StageAction.NotFound)


    let stage (path: string) : Result<StageAction, StageAction> =
        if String.IsNullOrWhiteSpace path then
            Error StageAction.NotFound
        else
            let { AbsolutePath = absolutePath } = resolvePaths path

            if Directory.Exists absolutePath then
                stageFolder absolutePath |> Result.mapError (fun _ -> StageAction.NotFound)
            elif File.Exists absolutePath then
                stageFile absolutePath |> Ok
            else
                Error StageAction.NotFound


    // Unstage a single file
    let private unstageFile (target: string) : StageAction =
        let { AbsolutePath = abs } = resolvePaths target
        let stage = loadStagingArea

        match stage with
        | Ok entries ->
            let updatedEntries = List.filter (fun e -> e.FilePath <> abs) entries

            match saveStagingArea updatedEntries with
            | Ok _ -> StageAction.Unstaged
            | Error _ -> StageAction.NotFound
        | Error _ -> StageAction.NotFound

    // Unstage all files in a folder
    let private unstageFolder (folder: string) : Result<StageAction, StageAction> =
        let { AbsolutePath = abs } = resolvePaths folder

        match loadStagingArea with
        | Ok entries ->
            let updatedEntries = List.filter (fun e -> not (e.FilePath.StartsWith(abs))) entries

            match saveStagingArea updatedEntries with
            | Ok _ -> Ok StageAction.Unstaged
            | Error _ -> Error StageAction.NotFound
        | Error _ -> Error StageAction.NotFound

    // Unstage a file or folder
    let unstage (path: string) : Result<StageAction, StageAction> =
        if String.IsNullOrWhiteSpace path then
            Error StageAction.NotFound
        else
            let { AbsolutePath = abs } = resolvePaths path

            if Directory.Exists abs then
                unstageFolder abs |> Result.mapError (fun _ -> StageAction.NotFound)
            elif File.Exists abs then
                unstageFile abs |> Ok
            else
                Error StageAction.NotFound

    let stageStatus =
        match loadStagingArea with
        | Ok stage -> stage
        | Error _ -> []
