namespace Nex.Core.Utils.Locale

open System.Globalization
open Nex.Core
open Nex.Core.Types
open Nex.Core.Utils.Config
open Version

/// <summary>
/// The available nex languages
/// </summary>
type Language =
    | EN
    | FR
// Languages can be added as needed

/// <summary>
/// Represents general messages that fall outside of responses for actions
/// </summary>
type UtilityMessage = | VersionMessage


/// <summary>
/// Represents wrappers for system actions that need to reply with localised messages
/// </summary>
type Message =
    | InitResponse of InitAction
    | CommitResponse of CommitAction
    | DiffResponse of DiffAction
    | UtilityMessage of UtilityMessage
    | FaultResponse of FaultAction
    | NotImplemented

module Tr =

    /// <summary>
    /// Type union to define a type that allows the programme to return dynamic or static strings for responses
    /// </summary>
    type private Translatable =
        | Simple of string
        | WithArgs of (string -> string)


    /// <summary>
    /// The object map for all localisable strings within the system
    /// </summary>
    /// <remarks>
    /// These translations are first grouped by the language and then by a type union
    /// that maps the Message to a 'Translatable' Type which can take arguments for dynamic responses or return a static
    /// string for a particular action's output
    /// </remarks>
    let private translations: Map<Language, Map<Message, Translatable>> =
        Map.ofList
            [ (EN,
               Map.ofList
                   [ (UtilityMessage VersionMessage, Simple $"NEX VCS {printCurrent}")
                     (InitResponse RepositoryCreated, WithArgs(sprintf "Repository created successfully @ %s"))
                     (InitResponse RepositoryExists, WithArgs(sprintf "A repository already exists at @ %s"))
                     (InitResponse DirectoryCreateFailed,
                      Simple "Failed to create directory. Do you have sufficient permissions for this action?")
                     (InitResponse ConfigWriteFailed,
                      Simple "Failed to write configuration file. Do you have sufficient permissions for this action?")

                     (DiffResponse UncommitedChanges, Simple "Uncommitted changes to the nex repo:")
                     (DiffResponse FileDiffResult_FileName, WithArgs(sprintf "File: %s"))
                     (DiffResponse NoChanges, Simple "No changes")
                     (DiffResponse AddedNLines, WithArgs(sprintf "Added %s lines"))
                     (DiffResponse DeletedNLines, WithArgs(sprintf "Deleted %s lines"))
                     (DiffResponse ChangedNLines, WithArgs(sprintf "Changed %s lines"))

                     (CommitResponse Created, Simple "Commit created")
                     (FaultResponse NoRepo,
 Simple
     "No nex repo could be found. Is there a valid .nex folder or .nexlink in this location?\nCreate one with 'nex init'")
                     (FaultResponse Fatal,
                      Simple
                          "Nex encountered an unrecoverable issue. Is there a valid .nex folder or .nexlink in this location?\nCreate one with 'nex init'") ])
              (FR,
               Map.ofList
                   [ (UtilityMessage VersionMessage, Simple $"NEX VCS {printCurrent}")
                     (InitResponse RepositoryCreated, WithArgs(sprintf "Dépôt créé avec succès @ %s"))
                     (InitResponse RepositoryExists, WithArgs(sprintf "Un dépôt existe déjà à @ %s"))
                     (InitResponse DirectoryCreateFailed, Simple "Échec de la création du répertoire")
                     (InitResponse ConfigWriteFailed, Simple "Échec de l'écriture du fichier de configuration")
                     (CommitResponse Created, Simple "Commit créé")

                     (DiffResponse UncommitedChanges, Simple "Modifications non validées dans le dépôt nex :")
                     (FaultResponse Fatal,
                      Simple
                          "Nex a rencontré un problème irrécupérable. Y a-t-il un dossier .nex valide ou un .nexlink à cet emplacement ?\nCréez-en un avec 'nex init'") ]) ]

    /// <summary>
    /// Helper function to change a dotnet culture to a valid nex language or default to English
    /// </summary>
    /// <param name="culture"></param>
    let private cultureToLanguage (culture: CultureInfo) =
        match culture.TwoLetterISOLanguageName.ToUpperInvariant() with
        | "FR" -> FR
        | _ -> EN // Default to English


    /// <summary>
    /// Helper function for getting the relevant culture from a language code
    /// </summary>
    /// <param name="language">The 2 character language code from the config</param>
    let languageToCulture (language: string) =
        match language with
        | "FR" -> FR
        | _ -> EN

    /// <summary>
    /// Helper function to fetch the system language
    /// </summary>
    let getSystemLanguage () =
        CultureInfo.CurrentUICulture |> cultureToLanguage


    /// <summary>
    /// Function to get the non-localised version of a string by including a language
    /// </summary>
    /// <remarks>
    ///  In most cases it is going to be correct to fetch a message via the 'getLocalisedMessages' function that applies
    ///  the correct localisation to the message
    /// </remarks>
    /// <param name="arg"></param>
    /// <param name="lang"></param>
    /// <param name="msg"></param>
    let getMessage (arg: string option) (lang: Language) (msg: Message) =
        //printfn "Message not found for language: %A, message: %A" lang msg //TODO: Remove me once the functionality is complete

        translations
        |> Map.tryFind lang
        |> Option.bind (fun langMap -> langMap |> Map.tryFind msg)
        |> function
            | Some(Simple s) -> s
            | Some(WithArgs f) -> f arg.Value
            | None -> failwith "Action Not found!"

    /// <summary>
    /// Function to fetch a localised version of the action message
    /// </summary>
    /// <param name="arg">An option string that can be fed into dynamic messages</param>
    let getLocalisedMessage (arg: string option) =
        getLanguage () |> languageToCulture |> (getMessage arg)
