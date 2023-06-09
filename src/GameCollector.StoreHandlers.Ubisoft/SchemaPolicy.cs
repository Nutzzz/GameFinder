using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Ubisoft;

/// <summary>
/// Policy to employ when the schema version doesn't match the supported schema version.
/// See <see cref="Ubisoft.SchemaPolicy"/> for more information.
/// </summary>
[PublicAPI]
public enum SchemaPolicy
{
    /// <summary>
    /// Completely ignores the new schema version.
    /// </summary>
    Ignore,

    /// <summary>
    /// Creates a warning about the new schema version. Note that this is represented as
    /// an error using <see cref="GameFinder.Common.ErrorMessage"/>. This does not abort the
    /// parsing.
    /// </summary>
    Warn,

    /// <summary>
    /// Creates an error and aborts the parsing.
    /// </summary>
    Error,
}
