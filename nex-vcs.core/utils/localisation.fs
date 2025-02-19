namespace Nex.Core.Utils.Locale

open System.Globalization
open Nex.Core.Types
open Nex.Core.Utils.Config

/// <summary>
/// The available nex languages
/// </summary>
type Language =
    | EN
    | FR
// Languages can be added as needed

/// <summary>
/// Represents wrappers for system actions that need to reply with localised messages
/// </summary>
type Message =
    | InitResponse of InitAction
    | CommitResponse of CommitAction
    | FaultResponse of FaultAction
    | NotImplemented

module Tr =

    /// <summary>
    /// The object map for all localisable string within the system
    /// </summary>
    /// <remarks>
    /// These translations are first grouped by the language and then by a type union
    /// that maps the
    /// </remarks>
    let private translations =
        Map.ofList
            [ (EN,
               Map.ofList
                   [ (InitResponse RepositoryCreated, "Repository created successfully")
                     (InitResponse RepositoryExists, "A repository already exists at this location")
                     (InitResponse DirectoryCreateFailed, "Failed to create directory")
                     (InitResponse ConfigWriteFailed, "Failed to write configuration file")
                     (CommitResponse Created, "Commit created")
                     (FaultResponse Fatal,
                      "Nex encountered an unrecoverable issue. Is there a valid .nex folder or .nexlink in this location? Create one with 'nex init'") ])
              (FR,
               Map.ofList
                   [ (InitResponse RepositoryCreated, "Dépôt créé avec succès")
                     (InitResponse RepositoryExists, "Un dépôt existe déjà à cet emplacement")
                     (InitResponse DirectoryCreateFailed, "Échec de la création du répertoire")
                     (InitResponse ConfigWriteFailed, "Échec de l'écriture du fichier de configuration")
                     (CommitResponse Created, "Commit créé")
                     (FaultResponse Fatal,
                      "Nex a rencontré un problème irrécupérable. Y a-t-il un dossier .nex valide ou un .nexlink à cet emplacement ? Créez-en un avec 'nex init'") ]) ]

    /// <summary>
    /// Helper function to change a dotnet culture to a valid nex language or default to English
    /// </summary>
    /// <param name="culture"></param>
    let private cultureToLanguage (culture: CultureInfo) =
        match culture.TwoLetterISOLanguageName.ToUpperInvariant() with
        | "FR" -> FR
        | _ -> EN // Default to English


    let languageToCulture (language: string) =
        match language with
        | "FR" -> FR
        | _ -> EN

    let getSystemLanguage () =
        CultureInfo.CurrentUICulture |> cultureToLanguage


    let getMessage (lang: Language) (msg: Message) =
        translations
        |> Map.tryFind lang
        |> Option.bind (fun langMap -> langMap |> Map.tryFind msg)
        |> Option.defaultValue "Message not found"

    /// Gets message in system language
    let getLocalisedMessage = getLanguage () |> languageToCulture |> getMessage
