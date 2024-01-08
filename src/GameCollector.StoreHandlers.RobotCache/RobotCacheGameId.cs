using TransparentValueObjects;

namespace GameCollector.StoreHandlers.RobotCache;

/// <summary>
/// Represents an id for games installed with the Robot Cache Client.
/// </summary>
[ValueObject<int>]
public readonly partial struct RobotCacheGameId : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}
