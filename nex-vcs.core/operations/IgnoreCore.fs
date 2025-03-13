namespace Nex.Core

open System
open System.IO
open System.Text.RegularExpressions
open Nex.Core.Utils.FileResolver

module IgnoreCore =
    /// Standard files and directories to always ignore
    let private standardIgnores = [ ".nex"; ".nexlink" ]

    /// <summary>
    /// Retrieves ignore patterns from .nexignore and .nexignorelocal files
    /// </summary>
    /// <param name="workingDir">The directory to search for ignore files</param>
    let private getIgnorePatterns (workingDir: string) =
        let loadPatternsFromFile path =
            if File.Exists(path) then
                File.ReadAllLines(path)
                |> Array.filter (fun line ->
                    not (String.IsNullOrWhiteSpace(line)) && not (line.TrimStart().StartsWith("#")))
                |> Array.toList
            else
                []

        let nexIgnorePath = Path.Combine(workingDir, ".nexignore")
        let nexIgnoreLocalPath = Path.Combine(workingDir, ".nexignorelocal")

        // Combine patterns from both files and add standard ignores
        loadPatternsFromFile nexIgnorePath
        |> List.append (loadPatternsFromFile nexIgnoreLocalPath)
        |> List.append standardIgnores
        |> List.distinct

    /// <summary>
    /// Determines if a pattern matches a path
    /// </summary>
    /// <param name="pattern">The ignore pattern</param>
    /// <param name="path">The path to check</param>
    let private matchesPattern (pattern: string) (path: string) =
        let normalisedPattern = pattern.TrimStart('/').TrimEnd('/')
        let isDirectoryPattern = pattern.EndsWith('/')

        let createRegex (p: string) =
            let escapedPattern =
                p
                    .Replace(".", "\\.")
                    .Replace("**/", "(.*/)?")
                    .Replace("**", ".*")
                    .Replace("*", "[^/]*")
                    .Replace("?", "[^/]")

            let finalPattern =
                "^" + escapedPattern + (if isDirectoryPattern then "(/.*)?$" else "$")

            Regex(finalPattern, RegexOptions.IgnoreCase)

        let matchDirectory (p: string) =
            let separator = Path.DirectorySeparatorChar.ToString()

            path.Equals(p, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(p + separator, StringComparison.OrdinalIgnoreCase)

        if normalisedPattern.Contains("*") || normalisedPattern.Contains("?") then
            createRegex normalisedPattern |> _.IsMatch(path)
        else if isDirectoryPattern then
            matchDirectory normalisedPattern
        else
            path.Equals(normalisedPattern, StringComparison.OrdinalIgnoreCase)

    /// <summary>
    /// Checks if a path should be ignored based on patterns
    /// </summary>
    /// <param name="patterns">List of ignore patterns</param>
    /// <param name="path">Path to check</param>
    let private isIgnored (patterns: string list) (path: string) =
        let normalisedPath = path.Replace(Path.DirectorySeparatorChar, '/')
        patterns |> List.exists (fun pattern -> matchesPattern pattern normalisedPath)

    /// <summary>
    /// Gets all files in a directory that are not ignored
    /// </summary>
    /// <param name="baseDir">The base directory to scan</param>
    let private getNonIgnoredFiles (baseDir: string) =
        let ignoredPatterns = getIgnorePatterns baseDir

        let rec traverse dir prefix =
            let relativePath (path: string) =
                path
                    .Substring(baseDir.Length)
                    .TrimStart(Path.DirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '/')

            let files =
                Directory.GetFiles(dir)
                |> Array.map relativePath
                |> Array.filter (fun path -> not (isIgnored ignoredPatterns path))
                |> Array.toList

            let subDirFiles =
                Directory.GetDirectories(dir)
                |> Array.map relativePath
                |> Array.filter (fun path -> not (isIgnored ignoredPatterns path))
                |> Array.collect (fun subDir -> traverse (Path.Combine(baseDir, subDir)) subDir |> List.toArray)
                |> Array.toList

            files @ subDirFiles

        traverse baseDir ""


    /// <summary>
    /// Retrieves ignore patterns from .nexignore and .nexignorelocal files in the current working directory
    /// </summary>
    let getRepoIgnorePatterns () =
        let workingDir = resolvePaths "." |> _.AbsolutePath
        getIgnorePatterns workingDir

    /// <summary>
    /// Checks if a path should be ignored based on the default ignore patterns
    /// </summary>
    /// <param name="path">Path to check (can be absolute or relative)</param>
    let shouldIgnore (path: string) =
        let patterns = getRepoIgnorePatterns ()
        let pathSet = resolvePaths path
        isIgnored patterns pathSet.RelativePath

    /// <summary>
    /// Gets all files in the working directory that are not ignored
    /// </summary>
    let getRepoNonIgnoredFiles () =
        let workingDir = resolvePaths "." |> _.AbsolutePath
        getNonIgnoredFiles workingDir
