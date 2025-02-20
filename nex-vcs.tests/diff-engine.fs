module DiffEngineTests

open Nex.Core.DiffEngine
open Nex.Core.Types
open Xunit

type DiffEngineTests() =

    [<Fact>]
    member _.``Diffing two identical strings returns no differences``() =
        let textA = "This is a test."
        let textB = "This is a test."
        let result = diffTextToSummary textA textB
        Assert.Empty(result)

    [<Fact>]
    member _.``Diffing two completely different strings returns full differences``() =
        let textA = "This is a test."
        let textB = "Completely different text."
        let result = diffTextToSummary textA textB
        printf $"{result.Head.insertedB}"
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(0, result.Head.StartA)
        Assert.Equal(0, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing with case sensitivity``() =
        let textA = "This is a Test."
        let textB = "This is a test."
        let result = diffTextToSummary textA textB
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(0, result.Head.StartA)
        Assert.Equal(0, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing with whitespace sensitivity``() =
        let textA = "This is a test."
        let textB = "This  is a test."
        let result = diffTextToSummary textA textB
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(0, result.Head.StartA)
        Assert.Equal(0, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing with empty strings``() =
        let textA = ""
        let textB = ""
        let result = diffTextToSummary textA textB
        Assert.Empty(result)

    [<Fact>]
    member _.``Diffing with one empty string``() =
        let textA = "This is a test."
        let textB = ""
        let result = diffTextToSummary textA textB
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(0, result.Head.StartA)
        Assert.Equal(0, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing with special characters``() =
        let textA = "This is a test with special characters: !@#$%^&*()"
        let textB = "This is a test with special characters: !@#$%^&*()"
        let result = diffTextToSummary textA textB
        Assert.Empty(result)

    [<Fact>]
    member _.``Diffing with multiline strings``() =
        let textA = "This is a test.\nWith multiple lines.\nAnd more lines."
        let textB = "This is a test.\nWith multiple lines.\nAnd even more lines."
        let result = diffTextToSummary textA textB
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(2, result.Head.StartA)
        Assert.Equal(2, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing two identical strings returns no differences for hunks``() =
        let textA = "This is a test."
        let textB = "This is a test."
        let result = diffTextToSummary textA textB
        Assert.Empty(result)

    [<Fact>]
    member _.``Diffing two completely different strings returns full differences for hunks``() =
        let textA = "This is a test."
        let textB = "Completely different text."
        let result = diffTextToSummary textA textB
        printf $"{result.Head.insertedB}"
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(0, result.Head.StartA)
        Assert.Equal(0, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing with case sensitivity for hunks``() =
        let textA = "This is a Test."
        let textB = "This is a test."
        let result = diffTextToSummary textA textB
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(0, result.Head.StartA)
        Assert.Equal(0, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing with whitespace sensitivity for hunks``() =
        let textA = "This is a test."
        let textB = "This  is a test."
        let result = diffTextToSummary textA textB
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(0, result.Head.StartA)
        Assert.Equal(0, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing with empty strings for hunks``() =
        let textA = ""
        let textB = ""
        let result = diffTextToSummary textA textB
        Assert.Empty(result)

    [<Fact>]
    member _.``Diffing with one empty string for hunks``() =
        let textA = "This is a test."
        let textB = ""
        let result = diffTextToSummary textA textB
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(0, result.Head.StartA)
        Assert.Equal(0, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Diffing with special characters for hunks``() =
        let textA = "This is a test with special characters: !@#$%^&*()"
        let textB = "This is a test with special characters: !@#$%^&*()"
        let result = diffTextToSummary textA textB
        Assert.Empty(result)

    [<Fact>]
    member _.``Diffing with multiline strings for hunks``() =
        let textA = "This is a test.\nWith multiple lines.\nAnd more lines."
        let textB = "This is a test.\nWith multiple lines.\nAnd even more lines."
        let result = diffTextToSummary textA textB
        Assert.NotEmpty(result)
        Assert.Equal(1, result.Length)
        Assert.Equal(2, result.Head.StartA)
        Assert.Equal(2, result.Head.StartB)
        Assert.Equal(1, result.Head.deletedA)
        Assert.Equal(1, result.Head.insertedB)

    [<Fact>]
    member _.``Hunk diffing two identical strings returns no hunks``() =
        let textA = "This is a test."
        let textB = "This is a test."
        let result = diffTextToHunks textA textB
        Assert.Empty(result)

    [<Fact>]
    member _.``Hunk diffing two completely different strings returns one hunk``() =
        let textA = "This is a test."
        let textB = "Completely different text."
        let result = diffTextToHunks textA textB
        Assert.Single(result)
        let hunk = result.Head
        Assert.Equal(1, hunk.StartLineA)
        Assert.Equal(1, hunk.StartLineB)
        Assert.Contains(Removed "This is a test.", hunk.Lines)
        Assert.Contains(Added "Completely different text.", hunk.Lines)

    [<Fact>]
    member _.``Hunk diffing with case sensitivity``() =
        let textA = "This is a Test."
        let textB = "This is a test."
        let result = diffTextToHunks textA textB
        Assert.Single(result)
        let hunk = result.Head
        Assert.Equal(1, hunk.StartLineA)
        Assert.Equal(1, hunk.StartLineB)
        Assert.Contains(Removed "This is a Test.", hunk.Lines)
        Assert.Contains(Added "This is a test.", hunk.Lines)

    [<Fact>]
    member _.``Hunk diffing with whitespace sensitivity``() =
        let textA = "This is a test."
        let textB = "This  is a test."
        let result = diffTextToHunks textA textB
        Assert.Single(result)
        let hunk = result.Head
        Assert.Equal(1, hunk.StartLineA)
        Assert.Equal(1, hunk.StartLineB)
        Assert.Contains(Removed "This is a test.", hunk.Lines)
        Assert.Contains(Added "This  is a test.", hunk.Lines)

    [<Fact>]
    member _.``Hunk diffing with empty strings``() =
        let textA = ""
        let textB = ""
        let result = diffTextToHunks textA textB
        Assert.Empty(result)

    [<Fact>]
    member _.``Hunk diffing with one empty string``() =
        let textA = "This is a test."
        let textB = ""
        let result = diffTextToHunks textA textB
        Assert.Single(result)
        let hunk = result.Head
        Assert.Equal(1, hunk.StartLineA)
        Assert.Equal(1, hunk.StartLineB)
        Assert.Contains(Removed "This is a test.", hunk.Lines)

    [<Fact>]
    member _.``Hunk diffing multiline with context``() =
        let textA = "Line 1\nLine 2\nLine 3\nLine to change\nLine 5\nLine 6\nLine 7"
        let textB = "Line 1\nLine 2\nLine 3\nChanged line\nLine 5\nLine 6\nLine 7"
        let result = diffTextToHunks textA textB
        Assert.Single(result)
        let hunk = result.Head
        Assert.Equal(1, hunk.StartLineA)
        Assert.Equal(1, hunk.StartLineB)
        Assert.Contains(Context "Line 3", hunk.Lines)
        Assert.Contains(Removed "Line to change", hunk.Lines)
        Assert.Contains(Added "Changed line", hunk.Lines)
        Assert.Contains(Context "Line 5", hunk.Lines)
