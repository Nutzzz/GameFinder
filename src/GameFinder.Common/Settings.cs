namespace GameFinder.Common;

public class Settings
{
    public bool InstalledOnly { get; set; } = false;
    public bool BaseOnly { get; set; } = false;
    public bool OwnedOnly { get; set; } = false;
    public bool GamesOnly { get; set; } = false;

    public Settings(bool installedOnly = false, bool baseOnly = false, bool ownedOnly = false, bool gamesOnly = false)
    {
        InstalledOnly = installedOnly;
        BaseOnly = baseOnly;
        OwnedOnly = ownedOnly;
        GamesOnly = gamesOnly;
    }
}
