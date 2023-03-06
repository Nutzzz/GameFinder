using JetBrains.Annotations;

namespace GameCollector.Wine;

/// <summary>
/// Represents a wine prefix.
/// </summary>
[PublicAPI]
public class WinePrefix : AWinePrefix
{
    internal WinePrefix(string configurationDirectory) : base(configurationDirectory) { }
}
