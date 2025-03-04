namespace Nex.Core

open System.IO



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
            let path = Path.Combine(workingDir, target)
Path.GetFullPath path

    // IO operations
    let stagingFile (workingDir: string) =
        Path.Combine(workingDir, ".nex", "stage.bson")

    let loadStagingArea (file: string) : Result<StagedEntry list, string> =
        try
            if File.Exists(file) then readBson file |> Ok else Ok []
        with ex ->
            Error ex.Message

    let private saveStagingArea (file: string) (entries: StagedEntry list) : Result<unit, string> =
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

    let private writeToStaging workingDir (entries: StagedEntry list) =
        let stage = loadStagingArea workingDir

        stage
        |> Result.bind (fun existing ->
            existing
            |> List.filter (fun e -> not (List.exists (fun n -> n.FilePath = e.FilePath) entries))
            |> List.append entries
            |> saveStagingArea workingDir)
        |> ignore

    let private stageFile (workingDir: string) (target: string) : StageAction =
        let changed = checkFileChanged target

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
                match checkFileChanged file with
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


    // Unstage a single file
    let private unstageFile (workingDir: string) (target: string) : StageAction =
        let relativePath = Path.GetRelativePath(workingDir, target)
        let stage = loadStagingArea workingDir

        match stage with
        | Ok entries ->
            let updatedEntries = List.filter (fun e -> e.FilePath <> relativePath) entries

            match saveStagingArea workingDir updatedEntries with
            | Ok _ -> StageAction.Unstaged
            | Error _ -> StageAction.NotFound
        | Error _ -> StageAction.NotFound

    // Unstage all files in a folder
    let private unstageFolder (workingDir: string) (folder: string) : Result<StageAction, StageAction> =
        let absoluteFolder = resolveFilePath workingDir folder
        let stage = loadStagingArea workingDir

        match stage with
        | Ok entries ->
            let updatedEntries =
                List.filter
                    (fun e -> not (e.FilePath.StartsWith(Path.GetRelativePath(workingDir, absoluteFolder))))
                    entries

            match saveStagingArea workingDir updatedEntries with
            | Ok _ -> Ok StageAction.Unstaged
            | Error _ -> Error StageAction.NotFound
        | Error _ -> Error StageAction.NotFound

    // Unstage a file or folder
    let unstage (path: string) : Result<StageAction, StageAction> =
        let workingDir = getWorkingDirectory ()

        if String.IsNullOrWhiteSpace path then
            Error StageAction.NotFound
        else
            let absolutePath = resolveFilePath workingDir path

            if Directory.Exists absolutePath then
                unstageFolder workingDir absolutePath
                |> Result.mapError (fun _ -> StageAction.NotFound)
            elif File.Exists absolutePath then
                unstageFile workingDir absolutePath |> Ok
            else
                Error StageAction.NotFound

    let stageStatus =
        match loadStagingArea <| getWorkingDirectory () with
        | Ok stage -> stage
        | Error _ -> []
