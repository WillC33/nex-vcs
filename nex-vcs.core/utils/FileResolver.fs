module Nex.Core.Utils.FileResolver

open System.IO
open Nex.Core.Utils.NexDirectory

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
        Path.Combine(Config.getWorkingDirectory (), target) |> Path.GetFullPath

/// <summary>
/// Converts an absolute file path into a path relative to the working directory.
/// </summary>
/// <param name="absolutePath">The absolute file path.</param>
/// <returns>The file path relative to the working directory.</returns>
let getRelativePathFromRepo (absolutePath: string) : string =
    let wd = Config.getWorkingDirectory ()
    Path.GetRelativePath(Config.getWorkingDirectory (), absolutePath)

/// <summary>
///
/// </summary>
/// <param name="absolutePath"></param>
let getRelativePathFromNex (absolutePath: string) : string =
    match tryGetNexRepoPath () with
    | Some dir -> Path.GetRelativePath(dir, absolutePath)
    | None -> failwith "Desastro"

/// <summary>
/// Resolves both the absolute and relative paths for a given target.
/// </summary>
/// <param name="target">The target file path, either absolute or relative.</param>
/// <returns>
/// A tuple containing the absolute path and the relative path with respect to the working directory.
/// </returns>
let resolvePaths
    (target: string)
    : {| AbsolutePath: string
         RelativePath: string |}
    =
    let absolutePath = getAbsPath target
    let relativePath = getRelativePathFromRepo absolutePath

    {| AbsolutePath = absolutePath
       RelativePath = relativePath |}
