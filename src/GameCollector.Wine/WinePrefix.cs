using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.Wine;

/// <summary>
/// Represents a wine prefix.
/// </summary>
[PublicAPI]
public class WinePrefix : AWinePrefix
{
    internal WinePrefix(AbsolutePath configurationDirectory) : base(configurationDirectory) { }
}
