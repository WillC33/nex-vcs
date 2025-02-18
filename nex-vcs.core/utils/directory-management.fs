module Nex.Core.Utils.dir_utils

open System
open System.IO

let private directoryIfExists path =
    if Directory.Exists(path) then Some path else None

/// Fetches the initial directory from the argument or the current directory.
let fetchInitDir (workingDirOpt: string option) =
    match workingDirOpt with
    | Some wd -> wd
    | None -> Environment.CurrentDirectory

/// Attempts to determine the Nex repository path.
/// It first checks for a local ".nex" folder, then for a ".nexlink" file.
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
    
    
// Example usage:
//match tryGetNexRepoPath () with
//| Some path -> printfn "Nex repository found at: %s" path
//| None -> printfn "No Nex repository found. Please initialize one."
