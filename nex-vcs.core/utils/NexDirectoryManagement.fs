module Nex.Core.Utils.NexDirectory

open System
open System.IO

/// <summary>
/// Fetches an option for an existing directory
/// </summary>
/// <param name="path"></param>
let private directoryIfExists path =
    if Directory.Exists(path) then Some path else None

/// <summary>
/// Checks if a directory exists at the given path.
/// </summary>
/// <param name="workingDirOpt">The path to check for the directory.</param>
/// <returns>Some path if the directory exists, otherwise None.</returns>
let fetchInitDir (workingDirOpt: string option) =
    match workingDirOpt with
    | Some wd -> wd
    | None -> Environment.CurrentDirectory

/// <summary>
/// Fetches the nex repo path
/// </summary>
/// <returns>The nex directory path.</returns>
let tryGetNexRepoPath () =
    let currentDir = Environment.CurrentDirectory
    let localNexPath = Path.Combine(currentDir, ".nex")
    let nexLinkFile = Path.Combine(currentDir, ".nexlink")

    // Check if .nex directory exists.
    directoryIfExists localNexPath
    |> Option.orElseWith (fun () ->
        if File.Exists(nexLinkFile) then
            let repoPath = File.ReadAllText(nexLinkFile).Trim()
            directoryIfExists repoPath
        else
            None)
