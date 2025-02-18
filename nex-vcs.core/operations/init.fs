namespace Nex.Core

open System.IO
open Nex.Core.Utils

module Init =

    /// Initialises a nex repository in the given working directory.
    /// If no directory is provided, uses the current directory.
    let initRepo (workingDirOpt: string option) =
        let workingDir = dir_utils.fetchInitDir workingDirOpt

        // Compute the repository path as a subdirectory of the working directory
        let repositoryDir = Path.Combine(workingDir, ".nex")
        let objectsDir = Path.Combine(repositoryDir, "objects")
        let refsDir = Path.Combine(repositoryDir, "refs")
        let headFile = Path.Combine(refsDir, "HEAD")
        let configFile = Path.Combine(repositoryDir, "config.toml")

        if Directory.Exists(repositoryDir) then
            printfn $"A repo already exists at %s{repositoryDir}."
        else
            // Create repository directories as well as an default HEAD and config
            Directory.CreateDirectory(repositoryDir) |> ignore
            Directory.CreateDirectory(objectsDir) |> ignore
            Directory.CreateDirectory(refsDir) |> ignore
            File.WriteAllText(headFile, "")
            ConfigParser.initConfig configFile workingDir

            printfn "Your repo is ready to go. Create a commit with 'nex commit'"
