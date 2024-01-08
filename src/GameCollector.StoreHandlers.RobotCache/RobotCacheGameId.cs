using TransparentValueObjects;

namespace GameCollector.StoreHandlers.RobotCache;

/// <summary>
/// Represents an id for games installed with the Robot Cache Client.
/// </summary>
[ValueObject<int>]
public readonly partial struct RobotCacheGameId { }
