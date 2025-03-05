module DiffCoreTests

open System
open System.IO
open Nex.Core
open Nex.Core.Types
open Xunit
open Nex.Core.Utils.Serialisation

/// <summary>
/// An attribute to define sequential test executions for those that have shared repository resources
/// </summary>
[<CollectionDefinition("Sequential-Execution", DisableParallelization = true)>]
type SequentialCollection() = class end


//Tests have some mutable shared state for creating the repo
[<Collection("Sequential-Execution")>]
type DiffCoreTests() =
    let mutable originalWorkingDir = ""
    let mutable testWorkingDir = ""
    let mutable testRepoDir = ""

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
        writeBson commitPath commitObj

    let createBlob (hash: string) (content: string) =
        let blobPath = Path.Combine(testRepoDir, "objects", hash)
        File.WriteAllText(blobPath, content)

    let setup () =
        // Clean up any existing test directory
        if Directory.Exists(testWorkingDir) then
            Directory.Delete(testWorkingDir, true)

        // Store original working directory
        originalWorkingDir <- Environment.CurrentDirectory
        testWorkingDir <- Path.Combine(Path.GetTempPath(), "nex-test")
        testRepoDir <- Path.Combine(testWorkingDir, ".nex")

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

    interface IClassFixture<DiffCoreTests>
    //member this.ClassFixture() = this.Setup()


    [<Fact>]
    member _.``DiffFile - Returns correct hunks for modified file``() =
        setup ()

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

        match result with
        | Ok res ->
            Assert.Single res |> ignore
            let hunk = res.Head

            Assert.Collection(
                hunk.Lines,
                (fun line -> Assert.Equal(Context "Line 1", line)),
                (fun line -> Assert.Equal(Removed "Line 2", line)),
                (fun line -> Assert.Equal(Added "Modified Line", line)),
                (fun line -> Assert.Equal(Context "Line 3", line))
            )
        | Error _ -> Assert.Fail "No result"


    [<Fact>]
    member _.``DiffFile - Returns empty list when files are identical``() =
        setup ()

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

        match result with
        | Ok res -> Assert.Empty res
        | Error _ -> Assert.Fail ""




    [<Fact>]
    member _.``DiffWorkingDirectory - Returns empty for clean repository``() =
        setup ()

        let result = DiffCore.diffWorkingDirectory ()
        Assert.Empty(result)

    [<Fact>]
    member _.``DiffWorkingDirectory - Detects modified and new files``() =
        setup ()

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
