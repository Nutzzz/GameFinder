using TransparentValueObjects;

namespace GameCollector.StoreHandlers.Oculus;

/// <summary>
/// Represents an id for games installed with Oculus.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct OculusGameId : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

