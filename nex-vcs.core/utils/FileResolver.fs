module Nex.Core.Utils.FileResolver

open System.IO
open Nex.Core.Utils.NexDirectory

/// <summary>
/// Defines a path set with an absolute and repo relative paths
/// </summary>
type PathSet =
    { AbsolutePath: string
      RelativePath: string }

/// <summary>
/// Gets the relative path from the .nex folder
/// </summary>
/// <param name="absolutePath"></param>
let getRelativePathFromNex (absolutePath: string) : string =
    match tryGetNexRepoPath () with
    | Some dir -> Path.Combine(dir, absolutePath) |> Path.GetFullPath
    | None -> Path.GetFullPath absolutePath


/// <summary>
/// Converts an absolute file path into a path relative to the working directory.
/// </summary>
/// <param name="absolutePath">The absolute file path.</param>
/// <returns>The file path relative to the working directory.</returns>
let rec getRelativePathFromRepo (absolutePath: string) : string =
    let wd = getRelativePathFromNex <| Config.getWorkingDirectory ()
    Path.GetRelativePath(wd, absolutePath)

/// <summary>
/// Resolves a given file path to an absolute path, using the working directory
/// if the provided path is relative.
/// </summary>
/// <param name="target">The target file path, either absolute or relative.</param>
/// <returns>The absolute path corresponding to the target.</returns>
let getAbsPath (target: string) : string =
    if Path.IsPathRooted(target) then
        target
    else
        let fromNex = getRelativePathFromNex <| Config.getWorkingDirectory ()
        Path.Combine(fromNex, target) |> Path.GetFullPath

/// <summary>
/// Resolves both the absolute and relative paths for a given target.
/// </summary>
/// <param name="target">The target file path, either absolute or relative.</param>
/// <returns>
/// A tuple containing the absolute path and the relative path with respect to the working directory.
/// </returns>
let resolvePaths (target: string) : PathSet =
    let absolutePath =
        Config.getWorkingDirectory ()
        |> getRelativePathFromNex
        |> fun p -> Path.Combine(p, target) |> Path.GetFullPath
        |> getAbsPath

    let relativePath = getRelativePathFromRepo absolutePath

    { AbsolutePath = absolutePath
      RelativePath = relativePath }

/// <summary>
/// Resolves the paths when there isn't a nex config to be relative to
/// </summary>
/// <param name="target">The target file path, either absolute or relative</param>
/// <returns>
/// A tuple containing the absolute path and the relative path with respect to the working directory.
/// </returns>
let resolveInitPaths (target: string) : PathSet =
    let absolutePath = getAbsPath target
    let relativePath = getRelativePathFromRepo absolutePath

    { AbsolutePath = absolutePath
      RelativePath = relativePath }
