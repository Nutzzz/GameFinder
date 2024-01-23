using TransparentValueObjects;

namespace GameCollector.DataHandlers.TheGamesDb;

/// <summary>
/// Represents an id for game data from TheGamesDB.net.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct TheGamesDbGameId : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

