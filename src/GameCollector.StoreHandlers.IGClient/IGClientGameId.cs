using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TransparentValueObjects;

namespace GameCollector.StoreHandlers.IGClient;

/// <summary>
/// Represents an id for games installed with Indiegala IGClient.
/// </summary>
[ValueObject<string>]
public readonly partial struct IGClientGameId : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

/// <inheritdoc/>
[PublicAPI]
public class IGClientGameIdComparer : IEqualityComparer<IGClientGameId>
{
    private static IGClientGameIdComparer? _default;

    /// <summary>
    /// Default equality comparer that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public static IGClientGameIdComparer Default => _default ??= new();

    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Default constructor that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public IGClientGameIdComparer() : this(StringComparison.OrdinalIgnoreCase) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stringComparison"></param>
    public IGClientGameIdComparer(StringComparison stringComparison)
    {
        _stringComparison = stringComparison;
    }

    /// <inheritdoc/>
    public bool Equals(IGClientGameId x, IGClientGameId y) => string.Equals(x.Value, y.Value, _stringComparison);

    /// <inheritdoc/>
    public int GetHashCode(IGClientGameId obj) => obj.Value.GetHashCode(_stringComparison);
}
