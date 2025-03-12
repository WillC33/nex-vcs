open System
open Xunit
open System.IO
open Nex.Tests.Mocking
open Xunit.Categories

/// <summary>
/// This is a quick smoke test that the fixture code is working and can correctly mock the structures needed
/// If these tests are broken it is likely that any test breakages are caused by the mocking helpers
/// </summary>
///<summary>
/// Smoke test that init fixture is working to provide a directory to instantiate the nex directory
/// </summary>
[<SystemTest>]
[<Fact>]
let ``RepoFixture properly creates test directory with mock repo`` () =
    use fixture = new RepoFixture()

    // Assert test directory was created
    Assert.True(Directory.Exists(fixture.TestDir))

    // Assert mock repo was copied correctly
    let nexDir = Path.Combine(fixture.TestDir, ".nex")
    Assert.True(Directory.Exists(nexDir))

    // Assert specific mock repo files were copied
    let configFile = Path.Combine(nexDir, "config.toml")
    let headFile = Path.Combine(nexDir, "refs", "HEAD")

    Assert.True(File.Exists(configFile))
    Assert.True(File.Exists(headFile))

    // Verify contents
    let configContent = File.ReadAllText(configFile)
    Assert.Contains("config", configContent.ToLower())

/// <summary>
/// Smoke test to check that there is a working nex repo provided by this fixture
/// </summary>
[<SystemTest>]
[<Fact>]
let ``InitFixture creates temporary directory and cleans up`` () =
    // Capture the directory before fixture creation
    let beforeDir = Environment.CurrentDirectory

    // Using a scope to ensure disposal happens
    let tempDirPath =
        use fixture = new InitFixture()

        // Assertions
        Assert.True(Directory.Exists(fixture.TestDir))
        Assert.Equal(beforeDir, fixture.OriginalDir)

        // Create a test file in the temp directory
        let testFilePath = Path.Combine(fixture.TestDir, "test.txt")
        File.WriteAllText(testFilePath, "This is a test")
        Assert.True(File.Exists(testFilePath))

        // Store path for checking after disposal
        fixture.TestDir

    // After fixture is disposed, directory should be gone
    Assert.False(Directory.Exists(tempDirPath))

    // Current directory should be restored
    Assert.Equal(beforeDir, Environment.CurrentDirectory)
