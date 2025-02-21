namespace Nex.Core

/// <summary>
/// Module that defines the versions of nex modules
/// </summary>
module Version =
    type private Version = { Major: int; Minor: int; Patch: int }

    /// <summary>
    /// The current version of nex core
    /// </summary>
    let private current: Version = { Major = 0; Minor = 1; Patch = 0 }

    /// Displays a version string in a M.m.p format
    let private toString version =
        $"v%d{version.Major}.%d{version.Minor}.%d{version.Patch}"

    /// <summary>
    /// Outputs the current version of nex as a 'M.m.p' string
    /// </summary>
    let printCurrent = toString current
