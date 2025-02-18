namespace Nex.Core

open System
open System.IO
open Newtonsoft.Json
open Nex.Core.Utils.ConfigParser

module Log =

    /// Represents a file entry in a commit.
    type FileEntry = { path: string; hash: string }

    /// Represents a commit object.
    type CommitObj = {
        id: string
        parent: string option
        message: string
        timestamp: DateTime
        files: FileEntry list
    }

    /// Reads and deserializes a commit object from the objects directory.
    let private readCommit (commitHash: string) : CommitObj =
        let objectsDir = ".nex/objects"
        let commitPath = Path.Combine(objectsDir, commitHash)
        if not (File.Exists(commitPath)) then
            failwithf $"Commit %s{commitHash} does not exist."
        else
            let json = File.ReadAllText(commitPath)
            JsonConvert.DeserializeObject<CommitObj>(json)

    /// Recursively builds a list of commits starting from the given commit hash.
    let rec private getCommitHistory (commitHash: string) : CommitObj list =
        let commit = readCommit commitHash
        match commit.parent with
        | Some parentHash when not (String.IsNullOrWhiteSpace(parentHash)) ->
            commit :: getCommitHistory parentHash
        | _ ->
            [ commit ]

    /// Displays the commit log by reading the HEAD and printing commit info.
    let showLog () =
        let config = loadConfig ()
        let headPath = $"{config.WorkingDirectory}/.nex/refs/HEAD"
        if not (File.Exists(headPath)) then
            printfn "No HEAD file found. Is this a Nex repository?"
        else
            let headHash = File.ReadAllText(headPath).Trim()
            if String.IsNullOrEmpty(headHash) then
                printfn "No commits yet."
            else
                let history = getCommitHistory headHash
                history
                |> List.iter (fun commit ->
                    printfn "Commit: %s" commit.id
                    printfn "Date:   %O" commit.timestamp
                    printfn "Message: %s" commit.message
                    printfn "--------------------------------"
                )

