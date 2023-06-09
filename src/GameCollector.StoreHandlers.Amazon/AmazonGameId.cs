using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vogen;

namespace GameCollector.StoreHandlers.Amazon;

/// <summary>
/// Represents an id for games installed with Amazon Games.
/// </summary>
[ValueObject<string>]
public readonly partial struct AmazonGameId { }

/// <inheritdoc/>
[PublicAPI]
public class AmazonGameIdComparer : IEqualityComparer<AmazonGameId>
{
    private static AmazonGameIdComparer? _default;

    /// <summary>
    /// Default equality comparer that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public static AmazonGameIdComparer Default => _default ??= new();

    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Default constructor that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public AmazonGameIdComparer() : this(StringComparison.OrdinalIgnoreCase) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stringComparison"></param>
    public AmazonGameIdComparer(StringComparison stringComparison)
    {
        _stringComparison = stringComparison;
    }

    /// <inheritdoc/>
    public bool Equals(AmazonGameId x, AmazonGameId y) => string.Equals(x.Value, y.Value, _stringComparison);

    /// <inheritdoc/>
    public int GetHashCode(AmazonGameId obj) => obj.Value.GetHashCode(_stringComparison);
}
