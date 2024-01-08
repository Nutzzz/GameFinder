using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TransparentValueObjects;

namespace GameCollector.StoreHandlers.Itch;

/// <summary>
/// Represents an id for games installed with itch.
/// </summary>
[ValueObject<string>]
public readonly partial struct ItchGameId : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

/// <inheritdoc/>
[PublicAPI]
public class BattleNetGameIdComparer : IEqualityComparer<ItchGameId>
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
    public bool Equals(ItchGameId x, ItchGameId y) => string.Equals(x.Value, y.Value, _stringComparison);

    /// <inheritdoc/>
    public int GetHashCode(ItchGameId obj) => obj.Value.GetHashCode(_stringComparison);
}
