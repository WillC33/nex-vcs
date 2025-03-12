namespace Nex.Tests.Core

open System
open System.Text
open Nex.Core.Utils.Hashing
open Xunit

[<Xunit.Categories.UnitTest>]
module HashingTests =
    /// <summary>
    /// Broadly these tests for the hashing are to ensure that the algorithm doesn't change and break past commits
    /// This also serves to check that the code for xxHash is working in other environments as it makes calls to SSE
    /// </summary>
    [<Fact>]
    let ``toHash should return a valid hash string`` () =
        let input = Encoding.UTF8.GetBytes("test input")
        let hash = toHash input
        Assert.False(String.IsNullOrEmpty(hash), "Hash should not be empty")
        Assert.True(hash.Length > 0, "Hash should have a length greater than 0")

    [<Fact>]
    let ``computeBlobHash should return a valid blob hash`` () =
        let content = Encoding.UTF8.GetBytes("blob content")
        let blobHash = computeBlobHash content
        Assert.False(String.IsNullOrEmpty(blobHash), "Blob hash should not be empty")
        Assert.True(blobHash.Length > 0, "Blob hash should have a length greater than 0")

    [<Fact>]
    let ``toHash should produce consistent results`` () =
        let input = Encoding.UTF8.GetBytes("consistent input")
        let hash1 = toHash input
        let hash2 = toHash input
        Assert.Equal(hash1, hash2)

    [<Fact>]
    let ``computeBlobHash should produce consistent results`` () =
        let content = Encoding.UTF8.GetBytes("consistent blob content")
        let blobHash1 = computeBlobHash content
        let blobHash2 = computeBlobHash content
        Assert.Equal(blobHash1, blobHash2)
