namespace Nex.Tests.Cli

open Nex.Tests.Mocking
open Xunit
open System
open System.IO
open Xunit.Categories

/// <summary>
/// Integration tests for the Init cli operation
/// </summary>
[<IntegrationTest>]
module InitOperationTests =
    type InitOperationTests(_fixture: InitFixture) =
        let fixture = new InitFixture()

        let verifyConfig path =
            let configPath = Path.Combine(path, ".nex", "config.toml")

            File.Exists(configPath)
            && let content = File.ReadAllText(configPath) in
               content.Contains("working_directory") && content.Contains("language")

        [<Fact>]
        let ``Init CLI - Creates repository and returns to original directory`` () =
            let startDir = Environment.CurrentDirectory
            let testPath = Path.Combine(fixture.TestDir, "return-test")

            let result = Nex.Cli.Main.main [| "init"; testPath |]

            Assert.Equal(0, result)
            Assert.Equal(startDir, Environment.CurrentDirectory)
            Assert.True(verifyConfig testPath)

        [<Fact>]
        let ``Init CLI - Creates repository with complex relative path`` () =
            let testPath = Path.Combine(fixture.TestDir, "deep", "..", "complex")

            let result = Nex.Cli.Main.main [| "init"; testPath |]

            Assert.Equal(0, result)
            Assert.True(Directory.Exists(Path.Combine(fixture.TestDir, "complex", ".nex")))

        (*TODO: Food for thought - [<Fact>]
    let ``Init CLI - Fails gracefully with network path`` () =
        let result = Nex.Cli.Main.main [| "init"; @"\\nonexistent\share" |]
        Assert.Equal(1, result)*)

        [<Fact>]
        let ``Init CLI - Other commands fail in non-repository`` () =
            let result = Nex.Cli.Main.main [| "status" |]
            Assert.Equal(1, result)

        interface IClassFixture<InitFixture> with

