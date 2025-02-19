module Nex.Core.Utils.Hashing

open System.Security.Cryptography
open System.Text

/// <summary>
/// Computes the Hash for use in the nex repo
/// </summary>
/// <param name="input">A byte array to hash</param>
let toHash (input: byte[]) =
   use sha = SHA1.Create()
   sha.ComputeHash(input) |> Array.map _.ToString("x2") |> String.concat ""
    
/// <summary>
/// Computes a blob hash using a header "blob <size>\0".
/// </summary>
/// <param name="content">the blob content to hash</param>
let computeBlobHash (content: byte[]) =
   let header = $"blob %d{content.Length}\u0000"
   let headerBytes = Encoding.UTF8.GetBytes(header)
   let fullBytes = Array.append headerBytes content
   toHash fullBytes 