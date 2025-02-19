module Nex.Core.Main

open System
open Argu
open Nex.Core.Types
open Nex.Core.Utils
open Nex.Core.Utils.Locale
open Nex.Core.Utils.Locale.Tr
open WriterUI

type LogArgs =
    | [<AltCommandLine("-c")>] Concise

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Concise -> "Show log in concise format"

type CliArguments =
    | [<CliPrefix(CliPrefix.None)>] Init of path: string option
    | [<CliPrefix(CliPrefix.None)>] Commit of message: string
    | [<CliPrefix(CliPrefix.None)>] Diff
    | [<CliPrefix(CliPrefix.None)>] Checkout of hash: string
    | [<CliPrefix(CliPrefix.None)>] Log of ParseResults<LogArgs>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Init _ -> "Initialise a new nex repository in the specified path or current directory"
            | Commit _ -> "Create a new commit with the specified message"
            | Diff -> "Show changes between working directory and last commit"
            | Checkout _ -> "Checkout a specific commit by hash"
            | Log _ -> "Show commit history (use -c for concise format)"

let errorHandler =
    ProcessExiter(
        colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.DarkYellow
    )

let parser =
    ArgumentParser.Create<CliArguments>(programName = "nex", errorHandler = errorHandler)

[<EntryPoint>]
let main argv =
    Writer.Message("nex vcs v0.1", ConsoleColor.Green)

    try
        let results = parser.ParseCommandLine(argv)

        match results.GetAllResults() with
        | [ Init path ] ->
            match InitCore.initRepo path with
            | Ok t -> getLocalisedMessage (InitResponse t) |> Writer.Message
            | Error e -> getLocalisedMessage (InitResponse e) |> Writer.Error

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

        | [ Diff ] ->
            printfn "Checking changes to the repo:"
            let oldText = System.IO.File.ReadAllText("oldVersion.txt")
            let newText = System.IO.File.ReadAllText("newVersion.txt")
            let diffs = DiffEngine.Diff.diffText oldText newText

            diffs
            |> List.iter (fun d ->
                printfn "At A:%d, B:%d, deleted: %d, inserted: %d" d.StartA d.StartB d.deletedA d.insertedB)

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
        getLocalisedMessage (FaultResponse Fatal) |> Writer.Error
        1
