using TransparentValueObjects;

namespace GameCollector.StoreHandlers.Plarium;

/// <summary>
/// Represents an id for games installed with Plarium Play.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct PlariumGameId : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

