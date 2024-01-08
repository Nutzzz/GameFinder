using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TransparentValueObjects;

namespace GameCollector.StoreHandlers.WargamingNet;

/// <summary>
/// Represents an id for games installed with Wargaming.net Game Center.
/// </summary>
[ValueObject<string>]
public readonly partial struct WargamingNetGameId : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

/// <inheritdoc/>
[PublicAPI]
public class WargamingNetGameIdComparer : IEqualityComparer<WargamingNetGameId>
{
    private static WargamingNetGameIdComparer? _default;

    /// <summary>
    /// Default equality comparer that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public static WargamingNetGameIdComparer Default => _default ??= new();

    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Default constructor that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public WargamingNetGameIdComparer() : this(StringComparison.OrdinalIgnoreCase) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stringComparison"></param>
    public WargamingNetGameIdComparer(StringComparison stringComparison)
    {
        _stringComparison = stringComparison;
    }

    /// <inheritdoc/>
    public bool Equals(WargamingNetGameId x, WargamingNetGameId y) => string.Equals(x.Value, y.Value, _stringComparison);

    /// <inheritdoc/>
    public int GetHashCode(WargamingNetGameId obj) => obj.Value.GetHashCode(_stringComparison);
}
