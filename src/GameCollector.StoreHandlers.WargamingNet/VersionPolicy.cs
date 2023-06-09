using GameFinder.Common;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.WargamingNet;

/// <summary>
/// Policy to employ when the metadata or gameinfo version doesn't match the supported version.
/// See <see cref="WargamingNetHandler.VersionPolicy"/> for more information.
/// </summary>
[PublicAPI]
public enum VersionPolicy
{
    /// <summary>
    /// Completely ignores the new metadata or gameinfo version.
    /// </summary>
    Ignore,

    /// <summary>
    /// Creates a warning about the new metadata or gameinfo version. Note that this is represented as
    /// an error using <see cref="ErrorMessage"/>. This does not abort the
    /// parsing.
    /// </summary>
    Warn,

    /// <summary>
    /// Creates an error and aborts the parsing.
    /// </summary>
    Error,
}
