namespace Nex.Core.Utils

open System.IO
open Nex.Core.Types
open Nex.Core.Utils.Directories
open Tomlyn

module ConfigParser =

    /// Initialises the config file with the working directory.
    let initConfig configPath workingDirectory =
        let configContent =
            $"""# Nex configuration File

# The working directory for the Nex repository.
working_directory = "{workingDirectory}"
      """

        File.WriteAllText(configPath, configContent)


    /// <summary>
    /// Loads the config file
    /// </summary>
    let loadConfig () =
        let repoPath = tryGetNexRepoPath ()
        let configPath =
            match repoPath with
                | Some value -> $"{value}/config.toml"
                | None -> failwith "No nex repo can be loaded. Is there a valid .nex folder or .nexlink file in this location?"
            
        if not (File.Exists(configPath)) then
            failwithf $"Config file not found at %s{configPath}"

        let tomlText = File.ReadAllText(configPath)
        Toml.Parse(tomlText).ToModel<NexConfig>()
        

    /// <summary>
    /// Gets the working directory from the nex config
    /// </summary>
    let getWorkingDirectory () =
        let config = loadConfig ()
        config.WorkingDirectory
