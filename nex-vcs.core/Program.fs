module Nex.Core.Main

open Nex.Core

[<EntryPoint>]
let main argv =

    match argv with
    | [| "init"; path |] ->
        printfn "Attempting to create a nex repo..."
        Init.initRepo (Some path)
        0
    | [| "commit"; message |] ->
        printfn $"Committing with message: %s{message}"
        Commit.commitSingleFile message
        0
    | [| "diff"; |] ->
        printfn "Checking changes to the repo:"
        Diff.diffAll
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
