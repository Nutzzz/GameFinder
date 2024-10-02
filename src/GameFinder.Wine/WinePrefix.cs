using JetBrains.Annotations;

namespace GameCollector.Wine;

/// <summary>
/// Represents a wine prefix.
/// </summary>
[PublicAPI]
public record WinePrefix : AWinePrefix
{
    public string? UserName { get; init; }

    protected override string GetUserName()
    {
        return UserName ?? base.GetUserName();
    }
}
