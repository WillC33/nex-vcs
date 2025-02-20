namespace Nex.Core.Types

/// Represents one diff item.
type DiffItem =
    { StartA: int
      StartB: int
      deletedA: int
      insertedB: int }

/// Represents a line in a diff hunk output
type DiffLine =
    | Added of string
    | Removed of string
    | Context of string

/// Represents a diff hunk
type DiffHunk =
    { StartLineA: int
      LinesA: int
      StartLineB: int
      LinesB: int
      Lines: DiffLine list }
