using TransparentValueObjects;

namespace GameFinder.StoreHandlers.GOG;

/// <summary>
/// Represents an id for games installed with GOG Galaxy.
/// </summary>
[ValueObject<long>]
public readonly partial struct GOGGameId : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}
