namespace Nex.Core.Utils

open System.Globalization
open System.IO
open Nex.Core.Types
open Nex.Core.Utils.NexDirectory
open Tomlyn

module Config =

    /// Initialises the config file with the working directory.
    let initConfig configPath workingDirectory =
        let language =
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToUpperInvariant()

        let configContent =
            $"""# Nex configuration File

working_directory = "{workingDirectory}"
language = "{language}"
      """

        File.WriteAllText(configPath, configContent)


    /// <summary>
    /// Loads the config file
    /// </summary>
    let loadConfig () =
        try
            let repoPath = tryGetNexRepoPath ()

            let configPath =
                match repoPath with
                | Some value -> $"{value}/config.toml"
                | None ->
                    failwith
                        "No nex repo can be loaded. Is there a valid .nex folder or .nexlink file in this location?"

            if not (File.Exists(configPath)) then
                failwithf $"Config file not found at %s{configPath}"

            let tomlText = File.ReadAllText(configPath)
            let config = Toml.Parse(tomlText).ToModel<NexConfig>()
            Ok config
        with ex ->
            Error ex.Message


    /// <summary>
    /// Gets a named property from the nex config
    /// </summary>
    let getConfigProp (property: NexConfig -> 'T) = loadConfig () |> Result.map property

    /// Gets the working directory
    let getWorkingDirectory () =
        getConfigProp _.WorkingDirectory |> Result.defaultValue ".."

    /// Gets the language
    let getLanguage () =
        getConfigProp _.Language |> Result.defaultValue "EN"
