using TransparentValueObjects;

namespace GameCollector.StoreHandlers.GameJolt;

/// <summary>
/// Represents an id for games installed with Game Jolt Client.
/// </summary>
[ValueObject<ulong>]
public readonly partial struct GameJoltGameId { }
