namespace Nex.Tests.Mocking

open System
open System.IO

/// <summary>
/// Fixture class for creating an environment to help initialise repos
/// </summary>
type InitFixture() =
    let mutable testDir =
        let dir = Path.Combine(Path.GetTempPath(), $"nex-test-{Guid.NewGuid()}")
        Directory.CreateDirectory(dir) |> ignore
        dir

    let originalDir = Environment.CurrentDirectory

    member _.TestDir = testDir
    member _.OriginalDir = originalDir

    interface IDisposable with
        member x.Dispose() =
            Environment.CurrentDirectory <- x.OriginalDir

            if Directory.Exists(x.TestDir) then
                Directory.Delete(x.TestDir, recursive = true)


/// <summary>
/// Fixture class for creating a mock nex repo for testing
/// </summary>
type RepoFixture() =
    let testDir =
        let dir = Path.Combine(Path.GetTempPath(), $"nex-test-{Guid.NewGuid()}")
        Directory.CreateDirectory(dir) |> ignore
        dir

    let originalDir = Environment.CurrentDirectory
    let repoSrc = Path.Combine(AppContext.BaseDirectory, "mock-nex-repo")

    let CopyDirectory source target =
        for dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories) do
            Directory.CreateDirectory(dirPath.Replace(source, target)) |> ignore

        for filePath in Directory.GetFiles(source, "*", SearchOption.AllDirectories) do
            File.Copy(filePath, filePath.Replace(source, target), overwrite = true)

    do CopyDirectory repoSrc testDir

    member _.TestDir = testDir
    member _.OriginalDir = originalDir

    interface IDisposable with
        member x.Dispose() =
            Environment.CurrentDirectory <- x.OriginalDir

            if Directory.Exists(x.TestDir) then
                Directory.Delete(x.TestDir, recursive = true)
