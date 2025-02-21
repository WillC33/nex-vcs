module Nex.Core.Main

open System
open Argu
open Nex.Cli
open Nex.Core.Types
open Nex.Core.Utils
open Nex.Core.Utils.Locale
open Nex.Core.Utils.Locale.Tr
open WriterUI

/// <summary>
/// Sub args for the log command
/// </summary>
type LogArgs =
    | [<AltCommandLine("-c")>] Concise

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Concise -> "Show log in concise format"

/// <summary>
/// Type for the core top level commands accessible via the cli
/// </summary>
type CliArguments =
    | [<CliPrefix(CliPrefix.None)>] Init of path: string option
    | [<CliPrefix(CliPrefix.None)>] Commit of message: string
    | [<CliPrefix(CliPrefix.None)>] Diff of path: string option
    | [<CliPrefix(CliPrefix.None)>] Checkout of hash: string
    | [<CliPrefix(CliPrefix.None)>] Log of ParseResults<LogArgs>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Init _ -> "Initialise a new nex repository in the specified path or current directory"
            | Commit _ -> "Create a new commit with the specified message"
            | Diff _ -> "Show changes between working directory and last commit"
            | Checkout _ -> "Checkout a specific commit by hash"
            | Log _ -> "Show commit history (use -c for concise format)"

/// <summary>
/// Handles unmatched arguments by returning Argu docs
/// </summary>
let errorHandler =
    ProcessExiter(
        colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.DarkYellow
    )

/// <summary>
/// The main parser implementation
/// </summary>
let parser =
    ArgumentParser.Create<CliArguments>(programName = "nex", errorHandler = errorHandler)

/// <summary>
/// Main switch for forwarding cli arguments to the relevant operations
/// </summary>
/// <param name="argv">The CLI args</param>
[<EntryPoint>]
let main argv =
    getLocalisedMessage None (UtilityMessage VersionMessage) |> Writer.Message

    try
        let results = parser.ParseCommandLine(argv)

        match results.GetAllResults() with
        | [ Init path ] ->
            let resolvedPath = defaultArg path ". " // Use current directory if no path provided

            match InitCore.initRepo (Some resolvedPath) with
            | Ok t -> getLocalisedMessage (Some resolvedPath) (InitResponse t) |> Writer.Message
            | Error e -> getLocalisedMessage (Some resolvedPath) (InitResponse e) |> Writer.Error

            0

        | cmd when
            List.exists
                (function
                | Init _ -> false
                | _ -> true)
                cmd
            && Result.isError (Config.loadConfig ())
            ->
            printfn "This command requires a nex repository. Run 'nex init' first."
            1

        | [ Commit message ] ->
            printfn $"Committing with message: %s{message}"
            Commit.commitSingleFile message
            0

        | [ Diff path ] ->
            Writer.Message("Uncommitted changes to the nex repo:", ConsoleColor.White)

            match path with
            | Some p -> DiffCore.diffFile p |> DiffCli.displayHunkDiffs p
            | None -> DiffCore.diffWorkingDirectory () |> DiffCli.displaySummaryDiffs

            0

        | [ Checkout hash ] ->
            printfn $"Checking out the commit: %s{hash}"
            Checkout.checkoutCommit hash
            0

        | [ Log logArgs ] ->
            printfn "-- Commit log --"
            let isConcise = logArgs.Contains Concise
            Log.showLog () // Update Log.showLog to accept isConcise parameter
            0

        | _ ->
            parser.PrintUsage() |> printfn "%s"
            0

    with ex ->
        getLocalisedMessage None (FaultResponse Fatal) |> Writer.Error
        1
