namespace Nex.Core

open System
open System.IO
open System.Text
open Nex.Core.Types
open Nex.Core.Utils.NexDirectory
open Nex.Core.Utils.Hashing

module CommitCore =
    open Nex.Core.Utils.Serialisation

    /// <summary>
    /// Writes the blob to the objects directory if it doesn't already exist.
    /// </summary>
    /// <param name="repoPath">the path of the nex repository</param>
    /// <param name="blobHash">the hash of the blob</param>
    /// <param name="content">the content of the commit</param>
    let private writeBlob (repoPath: string) (blobHash: string) (content: byte[]) =
        let blobPath = Path.Combine($"{repoPath}/objects", blobHash)

        if File.Exists(blobPath) then
            ()

        File.WriteAllBytes(blobPath, content)

    /// <summary>
    /// Writes the commit object, updating its id field with the computed hash.
    /// </summary>
    /// <param name="repoPath">the path of the nex repository</param>
    /// <param name="commitObj">the commit metadata object</param>
    let private writeCommit repoPath commitObj =
        let json = Newtonsoft.Json.JsonConvert.SerializeObject(commitObj)
        let bytes = Encoding.UTF8.GetBytes(json)
        let header = sprintf "commit %d\u0000" bytes.Length
        let headerBytes = Encoding.UTF8.GetBytes(header)
        let fullBytes = Array.append headerBytes bytes
        let commitHash = toHash fullBytes

        let updatedCommit = { commitObj with id = commitHash }
        let commitFilePath = Path.Combine(sprintf "%s/objects" repoPath, commitHash)

        if not (File.Exists(commitFilePath)) then
            writeBson commitFilePath updatedCommit

        commitHash

    /// Registers a commit from the staged files with the given commit message.
    let commitFromStaging (message: string) =
        let repoPath =
            match tryGetNexRepoPath () with
            | Some p -> p
            | None ->
                failwith
                    "No nex repository could be located for the commit. Does your folder contain a .nex folder or .nexlink?"

        let stageFile = Path.Combine(repoPath, "stage.bson")

        if not (File.Exists(stageFile)) then
            failwith "No staged files found."

        let stagedEntries =
            match StageCore.loadStagingArea with
            | Ok entries -> entries
            | Error _ -> failwith "Failed to load staging area."

        if List.isEmpty stagedEntries then
            printfn "No files to commit."
        else
            let fileEntries =
                stagedEntries
                |> List.map (fun entry ->
                    let absolutePath = Path.Combine(repoPath, "../", entry.FilePath)
                    let content = File.ReadAllBytes(absolutePath)
                    writeBlob repoPath entry.Hash content

                    { path = entry.FilePath
                      hash = entry.Hash })

            let headPath = $"{repoPath}/refs/HEAD"

            let parent =
                if File.Exists(headPath) then
                    let parentHash = File.ReadAllText(headPath).Trim()
                    if parentHash = "" then None else Some parentHash
                else
                    None

            // Create the commit object with an empty id; it will be updated in writeCommit.
            let commitObj: CommitObj =
                { id = ""
                  parent = parent
                  message = message
                  timestamp = DateTime.UtcNow
                  files = fileEntries }

            let commitHash = writeCommit repoPath commitObj

            // Update HEAD to point to the new commit.
            File.WriteAllText(headPath, commitHash)

            printfn $"Created commit: %s{commitHash}"
