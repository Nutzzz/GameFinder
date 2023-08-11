using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vogen;

namespace GameCollector.StoreHandlers.Riot;

/// <summary>
/// Represents an id for games installed with the Riot Client.
/// </summary>
[ValueObject<string>]
public readonly partial struct RiotGameId { }

/// <inheritdoc/>
[PublicAPI]
public class RiotGameIdComparer : IEqualityComparer<RiotGameId>
{
    private static RiotGameIdComparer? _default;

    /// <summary>
    /// Default equality comparer that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public static RiotGameIdComparer Default => _default ??= new();

    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Default constructor that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public RiotGameIdComparer() : this(StringComparison.OrdinalIgnoreCase) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stringComparison"></param>
    public RiotGameIdComparer(StringComparison stringComparison)
    {
        _stringComparison = stringComparison;
    }

    /// <inheritdoc/>
    public bool Equals(RiotGameId x, RiotGameId y) => string.Equals(x.Value, y.Value, _stringComparison);

    /// <inheritdoc/>
    public int GetHashCode(RiotGameId obj) => obj.Value.GetHashCode(_stringComparison);
}
