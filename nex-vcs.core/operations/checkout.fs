namespace Nex.Core

open System
open System.IO

module Checkout =
    open Nex.Core.Utils.Serialisation

    // Represents a commit object, same as used in the commit module.
    type FileEntry = { path: string; hash: string }

    type CommitObj =
        { id: string
          parent: string option
          message: string
          timestamp: DateTime
          files: FileEntry list }

    /// Reads and deserializes a commit object from the objects directory given its hash.
    let private readCommit (commitHash: string) : CommitObj =
        let objectsDir = ".nex/objects"
        let commitPath = Path.Combine(objectsDir, commitHash)

        if not (File.Exists(commitPath)) then
            failwithf "Commit %s does not exist." commitHash
        else
            readBson commitPath

    /// Reads a blob (file content) from the objects directory given its hash.
    let private readBlob (blobHash: string) : byte[] =
        let objectsDir = ".nex/objects"
        let blobPath = Path.Combine(objectsDir, blobHash)

        if not (File.Exists(blobPath)) then
            failwithf "Blob %s does not exist." blobHash
        else
            File.ReadAllBytes(blobPath)

    /// Restores all files from the given commit into the working directory.
    let checkoutCommit (commitHash: string) =
        // Read and deserialize the commit
        let commitObj = readCommit commitHash
        printfn "Checking out commit: %s" commitHash
        printfn "Commit message: %s" commitObj.message
        printfn "Timestamp: %O" commitObj.timestamp

        // For each file entry, read the blob and write the file.
        commitObj.files
        |> List.iter (fun entry ->
            let content = readBlob entry.hash
            // Write the content to the working directory at the given file path.
            // Optionally, create directories if they don't exist.
            let dir = Path.GetDirectoryName(entry.path)

            if not (String.IsNullOrWhiteSpace(dir)) then
                Directory.CreateDirectory(dir) |> ignore

            File.WriteAllBytes(entry.path, content)
            printfn "Restored file: %s" entry.path)

        // Optionally update HEAD or inform the user.
        printfn "Checkout complete."
