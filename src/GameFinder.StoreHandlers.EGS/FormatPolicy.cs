using GameFinder.Common;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.EGS;

/// <summary>
/// Policy to employ when the format version doesn't match the supported format version.
/// See <see cref="EGSHandler.FormatPolicy"/> for more information.
/// </summary>
[PublicAPI]
public enum FormatPolicy
{
    /// <summary>
    /// Completely ignores the new format version.
    /// </summary>
    Ignore,

    /// <summary>
    /// Creates a warning about the new format version. Note that this is represented as
    /// an error using <see cref="ErrorMessage"/>. This does not abort the
    /// parsing.
    /// </summary>
    Warn,

    /// <summary>
    /// Creates an error and aborts the parsing.
    /// </summary>
    Error,
}
