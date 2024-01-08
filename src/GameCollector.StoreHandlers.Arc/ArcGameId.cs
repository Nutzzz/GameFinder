using TransparentValueObjects;

namespace GameCollector.StoreHandlers.Arc;

/// <summary>
/// Represents an id for games installed with Arc.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct ArcGameId { }
