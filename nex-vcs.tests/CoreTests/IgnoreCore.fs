namespace Nex.Tests.Core

open System
open System.IO
open Nex.Core
open Nex.Tests.Mocking
open Xunit
open Xunit.Categories

/// <summary>
/// Tests for the public API of IgnoreCore module
/// </summary>
[<UnitTest>]
type IgnoreCoreTests() =
    [<Fact>]
    member _.``getRepoIgnorePatterns returns combined patterns from ignore files``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        // Create test ignore files
        File.WriteAllLines(Path.Combine(fixture.TestDir, ".nexignore"), [| "*.tmp"; "build/" |])
        File.WriteAllLines(Path.Combine(fixture.TestDir, ".nexignorelocal"), [| "*.log"; "temp/" |])

        let patterns = IgnoreCore.getRepoIgnorePatterns ()

        Assert.Contains("*.tmp", patterns)
        Assert.Contains("build/", patterns)
        Assert.Contains("*.log", patterns)
        Assert.Contains("temp/", patterns)
        Assert.Contains(".nex", patterns) // Standard ignore
        Assert.Contains(".nexlink", patterns) // Standard ignore

    [<Fact>]
    member _.``getRepoIgnorePatterns returns standard ignores when no ignore files exist``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        // Ensure no ignore files exist
        if File.Exists(Path.Combine(fixture.TestDir, ".nexignore")) then
            File.Delete(Path.Combine(fixture.TestDir, ".nexignore"))

        if File.Exists(Path.Combine(fixture.TestDir, ".nexignorelocal")) then
            File.Delete(Path.Combine(fixture.TestDir, ".nexignorelocal"))

        let patterns = IgnoreCore.getRepoIgnorePatterns ()

        Assert.Contains(".nex", patterns)
        Assert.Contains(".nexlink", patterns)
        Assert.Equal(2, patterns.Length)

    [<Fact>]
    member _.``shouldIgnore correctly identifies files that should be ignored``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        File.WriteAllLines(Path.Combine(fixture.TestDir, ".nexignore"), [| "*.tmp"; "build/"; "docs/*.md" |])

        // Create test files and directories
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "build"))
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "docs"))
        File.WriteAllText(Path.Combine(fixture.TestDir, "test.tmp"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "test.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "docs/readme.md"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "docs/notes.txt"), "")

        Assert.True(IgnoreCore.shouldIgnore "test.tmp")
        Assert.True(IgnoreCore.shouldIgnore "build/output.txt")
        Assert.True(IgnoreCore.shouldIgnore "docs/readme.md")
        Assert.False(IgnoreCore.shouldIgnore "test.txt")
        Assert.False(IgnoreCore.shouldIgnore "docs/notes.txt")

    [<Fact>]
    member _.``getRepoNonIgnoredFiles returns all non-ignored files``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        File.WriteAllLines(Path.Combine(fixture.TestDir, ".nexignore"), [| "*.tmp"; "build/" |])

        // Create test files and directories
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "build"))
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "src"))
        File.WriteAllText(Path.Combine(fixture.TestDir, "test.tmp"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "test.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "build/output.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "src/main.fs"), "")

        let nonIgnoredFiles = IgnoreCore.getRepoNonIgnoredFiles ()

        Assert.Contains("test.txt", nonIgnoredFiles)
        Assert.Contains("src/main.fs", nonIgnoredFiles)
        Assert.DoesNotContain("test.tmp", nonIgnoredFiles)
        Assert.DoesNotContain("build/output.txt", nonIgnoredFiles)

    [<Fact>]
    member _.``getRepoIgnorePatterns filters out comment and empty lines``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        File.WriteAllLines(
            Path.Combine(fixture.TestDir, ".nexignore"),
            [| "*.tmp"; ""; "# This is a comment"; "  # Another comment"; "build/" |]
        )

        let patterns = IgnoreCore.getRepoIgnorePatterns ()

        Assert.Contains("*.tmp", patterns)
        Assert.Contains("build/", patterns)
        Assert.DoesNotContain("", patterns)
        Assert.DoesNotContain("# This is a comment", patterns)
        Assert.DoesNotContain("  # Another comment", patterns)


    [<Fact>]
    member _.``shouldIgnore handles complex glob patterns correctly``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        File.WriteAllLines(
            Path.Combine(fixture.TestDir, ".nexignore"),
            [| "**/*.js"; "!lib/*.js"; "**/temp/**"; "*.{tmp,log}"; "doc[s]/" |]
        )

        // Create test directory structure
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "src"))
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "lib"))
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "src/temp"))
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "docs"))
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "doc"))

        // Create test files
        File.WriteAllText(Path.Combine(fixture.TestDir, "src/main.js"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "lib/util.js"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "src/app.fs"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "src/temp/cache.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "data.tmp"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "error.log"), "")

        // Test glob pattern matching
        Assert.True(IgnoreCore.shouldIgnore "src/main.js") // Matches **/*.js
        Assert.False(IgnoreCore.shouldIgnore "lib/util.js") // Matches **/*.js but excluded by !lib/*.js
        Assert.False(IgnoreCore.shouldIgnore "src/app.fs") // Doesn't match any pattern
        Assert.True(IgnoreCore.shouldIgnore "src/temp/cache.txt") // Matches **/temp/**
        Assert.True(IgnoreCore.shouldIgnore "data.tmp") // Matches *.{tmp,log}
        Assert.True(IgnoreCore.shouldIgnore "error.log") // Matches *.{tmp,log}
        Assert.True(IgnoreCore.shouldIgnore "docs/readme.txt") // Matches doc[s]/
        Assert.False(IgnoreCore.shouldIgnore "doc/readme.txt") // Doesn't match doc[s]/

    [<Fact>]
    member _.``shouldIgnore handles nested directory patterns correctly``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        File.WriteAllLines(
            Path.Combine(fixture.TestDir, ".nexignore"),
            [| "node_modules/"; "**/obj/"; "bin/Debug/**"; "!bin/Debug/keep.txt" |]
        )

        // Create directories and test files
        let createNestedDirs (basePath: string) (dirs: string list) =
            let fullPath = Path.Combine(fixture.TestDir, basePath)
            Directory.CreateDirectory(fullPath) |> ignore

            for dir in dirs do
                Directory.CreateDirectory(Path.Combine(fullPath, dir)) |> ignore

        createNestedDirs "project" [ "node_modules"; "src"; "bin" ]
        createNestedDirs "project/src" [ "obj" ]
        createNestedDirs "project/bin" [ "Debug"; "Release" ]

        File.WriteAllText(Path.Combine(fixture.TestDir, "project/node_modules/package.json"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "project/src/obj/build.log"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "project/bin/Debug/app.dll"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "project/bin/Debug/keep.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "project/bin/Release/app.dll"), "")

        // Test directory pattern matching
        Assert.True(IgnoreCore.shouldIgnore "project/node_modules/package.json") // Matches node_modules/
        Assert.True(IgnoreCore.shouldIgnore "project/src/obj/build.log") // Matches **/obj/
        Assert.True(IgnoreCore.shouldIgnore "project/bin/Debug/app.dll") // Matches bin/Debug/**
        Assert.False(IgnoreCore.shouldIgnore "project/bin/Debug/keep.txt") // Matches but excluded by !bin/Debug/keep.txt
        Assert.False(IgnoreCore.shouldIgnore "project/bin/Release/app.dll") // Doesn't match bin/Debug/**

    [<Fact>]
    member _.``getRepoNonIgnoredFiles respects nested ignore files``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        // Create main ignore file
        File.WriteAllLines(Path.Combine(fixture.TestDir, ".nexignore"), [| "*.tmp"; "*.bak" |])

        // Create local ignore file
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "subdir"))
        File.WriteAllLines(Path.Combine(fixture.TestDir, ".nexignorelocal"), [| "local-*" |])

        // Create test files
        File.WriteAllText(Path.Combine(fixture.TestDir, "main.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "backup.bak"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "temp.tmp"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "local-config.json"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "subdir/sub.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "subdir/sub.tmp"), "")

        // Delete the ignore files in the returned list for the assertion checks
        let nonIgnoredFiles =
            IgnoreCore.getRepoNonIgnoredFiles ()
            |> List.filter (fun p -> not (p = ".nexignore" || p = ".nexignorelocal"))

        // Check expected files are included/excluded
        Assert.Contains("main.txt", nonIgnoredFiles)
        Assert.Contains("subdir/sub.txt", nonIgnoredFiles)
        Assert.DoesNotContain("backup.bak", nonIgnoredFiles)
        Assert.DoesNotContain("temp.tmp", nonIgnoredFiles)
        Assert.DoesNotContain("local-config.json", nonIgnoredFiles)
        Assert.DoesNotContain("subdir/sub.tmp", nonIgnoredFiles)


    [<Fact>]
    member _.``shouldIgnore handles case sensitivity correctly``() =
        use fixture = new RepoFixture()
        Environment.CurrentDirectory <- fixture.TestDir

        File.WriteAllLines(Path.Combine(fixture.TestDir, ".nexignore"), [| "UPPERCASE/"; "MixedCase.TXT"; "test.jpg" |])

        // Create test files with different casings
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "uppercase"))
        Directory.CreateDirectory(Path.Combine(fixture.TestDir, "UPPERCASE"))
        File.WriteAllText(Path.Combine(fixture.TestDir, "mixedcase.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "MixedCase.txt"), "")
        File.WriteAllText(Path.Combine(fixture.TestDir, "TEST.JPG"), "")

        // Test case sensitivity (should be case insensitive on Windows, case sensitive on Unix)
        // Our implementation should be case insensitive regardless of platform
        Assert.True(IgnoreCore.shouldIgnore "uppercase/file.txt")
        Assert.True(IgnoreCore.shouldIgnore "UPPERCASE/file.txt")
        Assert.True(IgnoreCore.shouldIgnore "mixedcase.txt")
        Assert.True(IgnoreCore.shouldIgnore "MixedCase.TXT")
        Assert.True(IgnoreCore.shouldIgnore "test.jpg")
        Assert.True(IgnoreCore.shouldIgnore "TEST.JPG")
