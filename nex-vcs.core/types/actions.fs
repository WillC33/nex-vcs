namespace Nex.Core.Types

/// <summary>
/// Represent the actions that can be returned from the init command
/// </summary>
type InitAction =
    // Ok
    | RepositoryCreated
    // Faults
    | RepositoryExists
    | DirectoryCreateFailed
    | ConfigWriteFailed

/// <summary>
/// Represents the actions that can be returned from diff operations
/// </summary>
type DiffAction =
    // Feedback
    | UncommitedChanges
    | NoChanges
    // Ok
    | FileDiffResult_FileName
    | CodeDiffResult
    | AddedNLines
    | DeletedNLines
    | ChangedNLines
    // Faults
    | FailedToGenerate

/// <summary>
/// Represents the actions that can be returned from the commit command
/// </summary>
type CommitAction = | Created


/// <summary>
/// Represents the outcomes of staging actions
/// </summary>
type StageAction =
    | Staged
    | Unstaged
    | Unchanged
    | NotFound

/// <summary>
/// Represents general problems within the programme
/// </summary>
type FaultAction =
    | NoRepo
    | Fatal //Indicates an unrecoverable failure
