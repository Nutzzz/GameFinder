using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vogen;

namespace GameCollector.StoreHandlers.BattleNet;

/// <summary>
/// Represents an id for games installed with Blizzard Battle.net.
/// </summary>
[ValueObject<string>]
public readonly partial struct BattleNetGameId { }

/// <inheritdoc/>
[PublicAPI]
public class BattleNetGameIdComparer : IEqualityComparer<BattleNetGameId>
{
    private static BattleNetGameIdComparer? _default;

    /// <summary>
    /// Default equality comparer that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public static BattleNetGameIdComparer Default => _default ??= new();

    private readonly StringComparison _stringComparison;

    /// <summary>
    /// Default constructor that uses <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    public BattleNetGameIdComparer() : this(StringComparison.OrdinalIgnoreCase) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stringComparison"></param>
    public BattleNetGameIdComparer(StringComparison stringComparison)
    {
        _stringComparison = stringComparison;
    }

    /// <inheritdoc/>
    public bool Equals(BattleNetGameId x, BattleNetGameId y) => string.Equals(x.Value, y.Value, _stringComparison);

    /// <inheritdoc/>
    public int GetHashCode(BattleNetGameId obj) => obj.Value.GetHashCode(_stringComparison);
}
