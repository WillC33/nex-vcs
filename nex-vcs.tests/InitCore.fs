module InitCore

open System
open System.IO
open Xunit
open Nex.Core
open Nex.Core.Types

type InitCoreTests() =
    let tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())

    // Cleans up created files and directories
    interface IDisposable with
        member _.Dispose() =
            if Directory.Exists tempPath then
                Directory.Delete(tempPath, true)
            //This test needs to clear the .nex folder from the runtime folder just in case of previous failures
            let defaultDir = Path.Combine(Directory.GetCurrentDirectory(), ".nex")

            if Directory.Exists(defaultDir) then
                Directory.Delete(defaultDir, true)


    [<Fact>]
    member _.``Repository is created successfully in specified directory``() =
        let testDir = Path.Combine(tempPath, "testRepo")
        let result = InitCore.initRepo (Some testDir)

        Assert.True(
            match result with
            | Ok RepositoryCreated -> true
            | _ -> false
        )

        Assert.True(Directory.Exists(Path.Combine(testDir, ".nex")))

    [<Fact>]
    member _.``Repository creation fails when directory already contains nex repository``() =
        let testDir = Path.Combine(tempPath, "existingRepo")
        Directory.CreateDirectory(Path.Combine(testDir, ".nex")) |> ignore
        let result = InitCore.initRepo (Some testDir)

        Assert.True(
            match result with
            | Error RepositoryExists -> true
            | _ -> false
        )

    [<Fact>]
    member _.``Repository creation fails with invalid path``() =
        let result = InitCore.initRepo (Some "/invalid/path/that/should/not/exist")

        Assert.True(
            match result with
            | Error DirectoryCreateFailed -> true
            | _ -> false
        )

    [<Fact>]
    member _.``Repository creation works with null path``() =
        let result = InitCore.initRepo None

        Assert.True(
            match result with
            | Ok RepositoryCreated -> true
            | _ -> false
        )

    [<Fact>]
    member _.``Repository creation defaults to current directory with empty path``() =
        let result = InitCore.initRepo (Some "")

        Assert.True(
            match result with
            | Error DirectoryCreateFailed -> false
            | _ -> true
        )

    [<Fact>]
    member _.``Repository creation allowed with special characters in path``() =
        //This has been tested on MacOS and is allowed
        let testDir = Path.Combine(tempPath, "test|<>:*?")
        let result = InitCore.initRepo (Some testDir)

        Assert.True(
            match result with
            | Error DirectoryCreateFailed -> false
            | _ -> true
        )
