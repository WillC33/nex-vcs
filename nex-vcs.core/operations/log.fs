namespace Nex.Core

open System
open System.IO
open Nex.Core.Types
open Nex.Core.Utils.Config

module Log =
    open Nex.Core.Utils.Serialisation

    /// Represents a file entry in a commit.
    /// Reads and deserializes a commit object from the objects directory.
    let private readCommit (commitHash: string) : CommitObj =
        let objectsDir = ".nex/objects"
        let commitPath = Path.Combine(objectsDir, commitHash)

        if not (File.Exists(commitPath)) then
            failwithf $"Commit %s{commitHash} does not exist."
        else
            readBson commitPath

    /// Recursively builds a list of commits starting from the given commit hash.
    let rec private getCommitHistory (commitHash: string) : CommitObj list =
        let commit = readCommit commitHash

        match commit.parent with
        | Some parentHash when not (String.IsNullOrWhiteSpace(parentHash)) -> commit :: getCommitHistory parentHash
        | _ -> [ commit ]

    /// Displays the commit log by reading the HEAD and printing commit info.
    let showLog () =
        let workingDir = getWorkingDirectory ()
        let headPath = $"{workingDir}/.nex/refs/HEAD"

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
                    printfn "--------------------------------")
