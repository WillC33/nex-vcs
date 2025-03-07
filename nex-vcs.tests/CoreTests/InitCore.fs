module InitCore

open System
open System.Diagnostics
open System.IO
open Xunit
open Nex.Core
open Nex.Core.Types

/// <summary>
/// Tests for the public api of init operation
/// </summary>
type InitCoreTests() =

    /// Helper to generate a temporary testing dir
    let tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())

    /// Helper for testing failures with permissions on write for unix file system types
    let createDirectoryWithPermissions path permissions =
        Directory.CreateDirectory(path) |> ignore
        let psi = ProcessStartInfo("chmod", permissions + " " + path)
        psi.RedirectStandardOutput <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        let proc = Process.Start(psi)
        proc.WaitForExit()

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
        let result = InitCore.initRepo (Some "/invalid///path/that/should/not/exist")

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
        let testDir = Path.Combine(tempPath, "test|<>:*?")
        let result = InitCore.initRepo (Some testDir)

        Assert.True(
            match result with
            | Error DirectoryCreateFailed -> false
            | _ -> true
        )

    [<Fact>]
    member _.``Repository creation should return Error when directory cannot be created due to permission issues``() =
        let protectedDir = Path.Combine(tempPath, "protectedDir")
        Directory.CreateDirectory(protectedDir) |> ignore
        createDirectoryWithPermissions protectedDir "400"
        let result = InitCore.initRepo (Some protectedDir)

        Assert.True(
            match result with
            | Error DirectoryCreateFailed -> true
            | _ -> false
        )
