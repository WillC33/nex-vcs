namespace Nex.Core.Types

open System

/// <summary>
/// Represents a single file object in the commit
/// </summary>
type FileEntry = { path: string; hash: string }

///<summary>
/// Represents commit metadata and its associated files.
///</summary>
type CommitObj =
    { id: string // Commit id (hash)
      parent: string option // Parent commit hash (if any)
      message: string // Commit message
      timestamp: DateTime // Commit timestamp (UTC)
      files: FileEntry list } // List of file entries
