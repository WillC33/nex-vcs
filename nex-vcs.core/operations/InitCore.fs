namespace Nex.Core

open System.IO
open Nex.Core.Types
open Nex.Core.Utils
open Nex.Core.Utils.Directories

/// <summary>
/// Core logic for initialising a nex repository in a given directory
/// </summary>
module InitCore =

    /// <summary>
    /// Ensures that a directory is created at the path
    /// </summary>
    /// <param name="path"></param>
    let private ensureDirectory path =
        try
            Directory.CreateDirectory(path) |> ignore
            Ok()
        with ex ->
            Error DirectoryCreateFailed

    /// <summary>
    /// Ensures that a file can be written to
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    let private ensureFileWrite path (content: string) =
        try
            File.WriteAllText(path, content)
            Ok()
        with ex ->
            Error DirectoryCreateFailed

    /// <summary>
    /// Check whether a nex repository has already been generated in the directory
    /// </summary>
    /// <param name="path"></param>
    let private checkRepositoryExists path =
        if Directory.Exists(path) then
            Error RepositoryExists
        else
            Ok()

    /// <summary>
    /// Ensures that the config is written to the nex directory
    /// </summary>
    /// <param name="configFile"></param>
    /// <param name="workingDir"></param>
    let private ensureWriteConfig configFile workingDir =
        try
            Config.initConfig configFile workingDir
            Ok()
        with ex ->
            Error ConfigWriteFailed

    /// <summary>
    /// Initialises the nex repo at the given directory
    /// </summary>
    /// <remarks>
    /// This is the primary public interface for initialising a repository and can be called in user facing functionality
    /// </remarks>
    /// <param name="workingDirOpt"></param>
    let initRepo (workingDirOpt: string option) : Result<InitAction, InitAction> =
        let createPaths workingDir =
            let repositoryDir = Path.Combine(workingDir, ".nex")

            {| Repository = repositoryDir
               Objects = Path.Combine(repositoryDir, "objects")
               Refs = Path.Combine(repositoryDir, "refs")
               Head = Path.Combine(repositoryDir, "refs/HEAD")
               Config = Path.Combine(repositoryDir, "config.toml") |}

        fetchInitDir workingDirOpt
        |> createPaths
        |> fun paths ->
            checkRepositoryExists paths.Repository
            |> Result.bind (fun _ -> ensureDirectory paths.Repository)
            |> Result.bind (fun _ -> ensureDirectory paths.Objects)
            |> Result.bind (fun _ -> ensureDirectory paths.Refs)
            |> Result.bind (fun _ -> ensureFileWrite paths.Head "")
            |> Result.bind (fun _ -> ensureWriteConfig paths.Config paths.Repository)
            |> Result.map (fun _ -> RepositoryCreated)
