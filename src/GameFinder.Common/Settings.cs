namespace GameFinder.Common;

public class Settings
{
    public bool InstalledOnly { get; set; } = false;
    public bool BaseOnly { get; set; } = false;
    public bool OwnedOnly { get; set; } = false;
    public bool GamesOnly { get; set; } = false;

    public Settings(bool installed = false, bool parents = false, bool owned = false, bool games = false)
    {
        InstalledOnly = installed;
        BaseOnly = parents;
        OwnedOnly = owned;
        GamesOnly = games;
    }
}
