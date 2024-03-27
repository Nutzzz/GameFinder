using System.ComponentModel;

namespace GameFinder.Common;

/// <summary>
/// Problem identification
/// </summary>
public enum Problem
{
    /// <summary>
    /// Install pending (queued, downloading, or install in progress)
    /// </summary>
    [Description("This item is waiting to install")]
    InstallPending,
    /// <summary>
    /// Not found in data (The game is installed, but the launcher may not agree)
    /// </summary>
    /// <remarks>
    /// Opposite of NotFoundInData
    /// </remarks>
    [Description("This item was not found in the launcher's manifests or database")]
    NotFoundInData,
    /// <summary>
    /// Not found on disk (The launcher thinks the game is installed, but it's not)
    /// </summary>
    /// <remarks>
    /// Opposite of NotFoundOnDisk
    /// </remarks>
    [Description("This item's installation was not found")]
    NotFoundOnDisk,
    /// <summary>
    /// Expired trial
    /// </summary>
    [Description("This item is an expired trial or part of a lapsed membership")]
    ExpiredTrial,
    /// <summary>
    /// Does not meet requirements
    /// </summary>
    /// <remarks>
    /// The MAME handler uses this when the driver emulation status doesn't meet the minimum ("imperfect" by default)
    /// </remarks>
    [Description("This item does not meet requirements")]
    DoesNotMeetRequirements,
    /// <summary>
    /// Failed to verify (The game is on the disk, but files may be corrupt or a mismatched version)
    /// </summary>
    [Description("This item failed verification")]
    FailedToVerify,
}
