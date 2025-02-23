module DiffCoreTests

open System
open System.IO
open Nex.Core
open Nex.Core.Types
open Xunit

type DiffCoreTests() =
    let mutable originalWorkingDir = ""
    let testWorkingDir = Path.Combine(Path.GetTempPath(), "nex-test")
    let testRepoDir = Path.Combine(testWorkingDir, ".nex")

    let setupTestRepo () =
        // Clean up any existing test directory
        if Directory.Exists(testWorkingDir) then
            Directory.Delete(testWorkingDir, true)

        // Store original working directory
        originalWorkingDir <- Environment.CurrentDirectory

        // Create test directories
        Directory.CreateDirectory(testWorkingDir) |> ignore
        Directory.CreateDirectory(testRepoDir) |> ignore
        Directory.CreateDirectory(Path.Combine(testRepoDir, "objects")) |> ignore
        Directory.CreateDirectory(Path.Combine(testRepoDir, "refs")) |> ignore
        Directory.CreateDirectory(Path.Combine(testRepoDir, "refs", "heads")) |> ignore

        // Create config.toml with absolute paths
        let configContent =
            $"""
working_directory = "{testWorkingDir}"
language = "EN"
"""

        File.WriteAllText(Path.Combine(testRepoDir, "config.toml"), configContent)

        // Create empty HEAD file
        File.WriteAllText(Path.Combine(testRepoDir, "refs", "HEAD"), "")

        // Set working directory for tests
        Environment.CurrentDirectory <- testWorkingDir

    let createCommit (id: string) (message: string) (files: FileEntry list) =
        let commitObj: CommitObj =
            { id = id
              parent = None
              message = message
              timestamp = DateTime.Now
              files = files }

        Directory.CreateDirectory(Path.Combine(testRepoDir, "objects")) |> ignore
        File.WriteAllText(Path.Combine(testRepoDir, "refs", "HEAD"), id)

        let commitPath = Path.Combine(testRepoDir, "objects", id)
        File.WriteAllText(commitPath, Newtonsoft.Json.JsonConvert.SerializeObject(commitObj))

    let createBlob (hash: string) (content: string) =
        let blobPath = Path.Combine(testRepoDir, "objects", hash)
        File.WriteAllText(blobPath, content)

    member _.Cleanup() =
        try
            if not (String.IsNullOrEmpty(originalWorkingDir)) then
                Environment.CurrentDirectory <- originalWorkingDir

            if Directory.Exists(testWorkingDir) then
                Directory.Delete(testWorkingDir, true)
        with _ ->
            ()

    interface IDisposable with
        member this.Dispose() = this.Cleanup()

    [<Fact>]
    member _.``DiffFile - Returns correct hunks for modified file``() =
        setupTestRepo ()
        let workingDir = Path.GetFullPath(Environment.CurrentDirectory)
        let fileName = "test.txt"
        let absoluteFilePath = Path.GetFullPath(Path.Combine(workingDir, fileName))

        let file =
            { path = absoluteFilePath
              hash = "original123" }

        let originalContent = "Line 1\nLine 2\nLine 3"
        let modifiedContent = "Line 1\nModified Line\nLine 3"
        createBlob file.hash originalContent
        createCommit "commit123" "test commit" [ file ]
        File.WriteAllText(absoluteFilePath, modifiedContent)

        let result = DiffCore.diffFile fileName

        Assert.Single(result) |> ignore
        let hunk = result.Head

        Assert.Collection(
            hunk.Lines,
            (fun line -> Assert.Equal(Context "Line 1", line)),
            (fun line -> Assert.Equal(Removed "Line 2", line)),
            (fun line -> Assert.Equal(Added "Modified Line", line)),
            (fun line -> Assert.Equal(Context "Line 3", line))
        )

    [<Fact>]
    member _.``DiffFile - Returns empty list when files are identical``() =
        setupTestRepo ()
        let content = "Test content\nSecond line"
        let fileName = "test.txt"

        let absoluteFilePath =
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, fileName))

        let file =
            { path = absoluteFilePath
              hash = "content123" }

        File.WriteAllText(fileName, content)
        createCommit "commit123" "test commit" [ file ]
        createBlob file.hash content

        let result = DiffCore.diffFile fileName

        Assert.Empty(result)



    [<Fact>]
    member _.``DiffWorkingDirectory - Returns empty for clean repository``() =
        setupTestRepo ()

        let result = DiffCore.diffWorkingDirectory ()
        Assert.Empty(result)

    [<Fact>]
    member _.``DiffWorkingDirectory - Detects modified and new files``() =
        setupTestRepo ()

        let file =
            { path = "file1.txt"
              hash = "original123" }

        createCommit "commit123" "test commit" [ file ]
        createBlob file.hash "Original content"
        File.WriteAllText("file1.txt", "Modified content")
        File.WriteAllText("file2.txt", "New file")

        let result = DiffCore.diffWorkingDirectory ()

        Assert.Equal(2, result.Length)
        Assert.Contains("file1.txt", result |> List.map fst)
        Assert.Contains("file2.txt", result |> List.map fst)


// Not yet implemented
(*[<Fact>]
    member _.``DiffWorkingDirectory - Respects nexignore patterns for all files of a type``() =
        setupTestRepo ()
        File.WriteAllText(".nexignore", "*.log")
        File.WriteAllText("test.txt", "content")
        File.WriteAllText("test.log", "log content")

        let result = DiffCore.diffWorkingDirectory ()

        Assert.Single(result) |> ignore
        Assert.Equal("test.txt", (result |> List.head |> fst))
    *)
