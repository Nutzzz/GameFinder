using GameFinder.Common;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.GameJolt;

/// <summary>
/// Policy to employ when the wttf or manifest version doesn't match the supported version.
/// See <see cref="GameJoltHandler.VersionPolicy"/> for more information.
/// </summary>
[PublicAPI]
public enum VersionPolicy
{
    /// <summary>
    /// Completely ignores the new wttf or manifest version.
    /// </summary>
    Ignore,

    /// <summary>
    /// Creates a warning about the new wttf or manifest version. Note that this is represented as
    /// an error using <see cref="ErrorMessage"/>. This does not abort the
    /// parsing.
    /// </summary>
    Warn,

    /// <summary>
    /// Creates an error and aborts the parsing.
    /// </summary>
    Error,
}
