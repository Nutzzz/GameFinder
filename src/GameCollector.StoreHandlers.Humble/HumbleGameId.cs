using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vogen;

namespace GameCollector.StoreHandlers.Humble;

/// <summary>
/// Represents an id for games installed with Humble App.
/// </summary>
[ValueObject<string>]
public readonly partial struct HumbleGameId { }

/// <inheritdoc/>
[PublicAPI]
public class HumbleGameIdComparer : IEqualityComparer<HumbleGameId>
{
    private static HumbleGameIdComparer? _default;

    /// <summary>
    /// Default equality comparer that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public static HumbleGameIdComparer Default => _default ??= new();

    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Default constructor that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public HumbleGameIdComparer() : this(StringComparison.OrdinalIgnoreCase) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stringComparison"></param>
    public HumbleGameIdComparer(StringComparison stringComparison)
    {
        _stringComparison = stringComparison;
    }

    /// <inheritdoc/>
    public bool Equals(HumbleGameId x, HumbleGameId y) => string.Equals(x.Value, y.Value, _stringComparison);

    /// <inheritdoc/>
    public int GetHashCode(HumbleGameId obj) => obj.Value.GetHashCode(_stringComparison);
}
