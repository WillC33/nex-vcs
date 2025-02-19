namespace Nex.Core

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Newtonsoft.Json
open Nex.Core.Types
open Nex.Core.Utils.Directories
open Nex.Core.Utils.Hashing

module Commit =

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
        // First, serialise the commit object without the id set.
        let json = JsonConvert.SerializeObject(commitObj)
        let bytes = Encoding.UTF8.GetBytes(json)
        let header = $"commit %d{bytes.Length}\u0000"
        let headerBytes = Encoding.UTF8.GetBytes(header)
        let fullBytes = Array.append headerBytes bytes
        let commitHash = toHash fullBytes

        // Now create a new commit object with the computed id.
        let updatedCommit = { commitObj with id = commitHash }
        let updatedJson = JsonConvert.SerializeObject(updatedCommit)
        let commitPath = Path.Combine($"{repoPath}/objects", commitHash)

        if not (File.Exists(commitPath)) then
            File.WriteAllBytes(commitPath, Encoding.UTF8.GetBytes(updatedJson))

        commitHash

    /// Registers a commit for a single file "code.txt" with the given commit message.
    let commitSingleFile (message: string) =
        let repoPath =
            match tryGetNexRepoPath () with
            | Some p -> p
            | None ->
                failwith
                    "No nex repository could be located for the commit. Does your folder contain a .nex folder or .nexlink?"

        let filePath = "code.txt"

        if not (File.Exists(filePath)) then
            failwith "File 'code.txt' does not exist."

        let content = File.ReadAllBytes(filePath)
        let blobHash = computeBlobHash content
        writeBlob repoPath blobHash content

        let fileEntry = { path = filePath; hash = blobHash }
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
              files = [ fileEntry ] }

        let commitHash = writeCommit repoPath commitObj

        // Update HEAD to point to the new commit.
        File.WriteAllText(headPath, commitHash)

        printfn $"Created commit: %s{commitHash}"
