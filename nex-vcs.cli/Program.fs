module Nex.Core.Main

open Nex.Core.Types
open Nex.Core.Utils
open Nex.Core.Utils.Locale
open Nex.Core.Utils.Locale.Tr

let requiresRepo cmd =
    match cmd with
    | "init" -> false
    | _ -> true

[<EntryPoint>]
let main argv =
    // In situations where the config cannot be read the language will be handled by the system
    // This is a fallback option called with getMessage instead of getLocalisedMessage
    let preConfigLang = getSystemLanguage ()

    try
        match argv with
        | args when requiresRepo args[0] && Result.isError (Config.loadConfig ()) ->
            printfn "This command requires a nex repository. Run 'nex init' first."
            1
        | [| "init" |]
        | [| "init"; _ |] ->
            let path =
                match argv with
                | [| _; path |] -> Some path
                | _ -> None

            match Init.initRepo path with
            | Ok t -> getMessage preConfigLang (InitResponse t)
            | Error e -> getMessage preConfigLang (InitResponse e)
            |> printf "%s"

            0
        | [| "commit"; message |] ->
            printfn $"Committing with message: %s{message}"
            Commit.commitSingleFile message
            0
        | [| "diff" |] ->
            printfn "Checking changes to the repo:"
            let oldText = System.IO.File.ReadAllText("oldVersion.txt")
            let newText = System.IO.File.ReadAllText("newVersion.txt")
            let diffs = DiffEngine.Diff.diffText oldText newText

            diffs
            |> List.iter (fun d ->
                printfn "At A:%d, B:%d, deleted: %d, inserted: %d" d.StartA d.StartB d.deletedA d.insertedB)

            0
        | [| "checkout"; hash |] ->
            printfn $"Checking out the commit: %s{hash}"
            Checkout.checkoutCommit hash
            0
        | [| "log" |] ->
            printfn "-- Commit log --"
            Log.showLog ()
            0
        | _ ->
            printfn "Usage: nex <command> [options]"
            0
    with _ ->

        getMessage preConfigLang (FaultResponse Fatal) |> printf "%s"
        1
