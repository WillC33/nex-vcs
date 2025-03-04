module Nex.Core.Utils.Hashing

open System.Text
open Standart.Hash.xxHash

/// <summary>
/// Creates a header for a nex blob object
/// </summary>
/// <param name="size">the content size</param>
let createBlobHeader size =
    $"blob %d{size}\u0000" |> Encoding.UTF8.GetBytes

/// <summary>
/// Computes the Hash for use in the nex repo
/// </summary>
/// <param name="input">A byte array to hash</param>
let toHash (input: byte[]) =
    xxHash3.ComputeHash(input, input.Length) |> _.ToString("x2")

/// <summary>
/// Computes a blob hash using a header "blob <size>\0".
/// </summary>
/// <param name="content">the blob content to hash</param>
let computeBlobHash (content: byte[]) =
    let header = createBlobHeader content.Length
    Array.append header content |> toHash
