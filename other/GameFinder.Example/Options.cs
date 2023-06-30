using CommandLine;

namespace GameFinder.Example;

public class Options
{
    [Option("all", HelpText = "Search for games from all handlers", Hidden = true, Group = "Handlers")]
    public bool All { get; set; } = false;

    [Option("amazon", HelpText = "Search for Amazon games", Hidden = true, Group = "Handlers")]
    public bool Amazon { get; set; } = false;

    [Option("arc", HelpText = "Search for Arc games", Hidden = true, Group = "Handlers")]
    public bool Arc { get; set; } = false;

    [Option("battlenet", HelpText = "Search for Battle.net games", Hidden = true, Group = "Handlers")]
    public bool BattleNet { get; set; } = false;

    [Option("bigfish", HelpText = "Search for Big Fish Game Manager games", Hidden = true, Group = "Handlers")]
    public bool BigFish { get; set; } = false;

    [Option("dolphin", HelpText = "Search for Dolphin ROMs [path to .exe required]", MetaValue = "PATH", Group = "Handlers")]
    public string? Dolphin { get; set; }

    [Option("ea", HelpText = "Search for EA app games", Hidden = true, Group = "Handlers")]
    public bool EADesktop { get; set; } = false;

    [Option("epic", HelpText = "Search for Epic Games Launcher games", Hidden = true, Group = "Handlers")]
    public bool EGS { get; set; } = false;

    [Option("gamejolt", HelpText = "Search for Game Jolt Client games", Hidden = true, Group = "Handlers")]
    public bool GameJolt { get; set; } = false;

    [Option("gog", HelpText = "Search for GOG GALAXY games", Hidden = true, Group = "Handlers")]
    public bool GOG { get; set; } = false;

    [Option("humble", HelpText = "Search for Humble App games", Hidden = true, Group = "Handlers")]
    public bool Humble { get; set; } = false;

    [Option("igclient", HelpText = "Search for Indiegala Client games", Hidden = true, Group = "Handlers")]
    public bool IGClient { get; set; } = false;

    [Option("itch", HelpText = "Search for itch games", Hidden = true, Group = "Handlers")]
    public bool Itch { get; set; } = false;

    [Option("legacy", HelpText = "Search for Legacy Games Launcher games", Hidden = true, Group = "Handlers")]
    public bool Legacy { get; set; } = false;

    [Option("mame", HelpText = "Search for MAME ROMs [path to .exe required]", MetaValue = "PATH", Group = "Handlers")]
    public string? MAME { get; set; }

    [Option("oculus", HelpText = "Search for Oculus games", Hidden = true, Group = "Handlers")]
    public bool Oculus { get; set; } = false;

    [Option("origin", HelpText = "Search for Origin games", Hidden = true, Group = "Handlers")]
    public bool Origin { get; set; } = false;

    [Option("paradox", HelpText = "Search for Paradox Launcher games", Hidden = true, Group = "Handlers")]
    public bool Paradox { get; set; } = false;

    [Option("plarium", HelpText = "Search for Plarium Play games", Hidden = true, Group = "Handlers")]
    public bool Plarium { get; set; } = false;

    [Option("riot", HelpText = "Search for Riot Client games", Hidden = true, Group = "Handlers")]
    public bool Riot { get; set; } = false;

    [Option("rockstar", HelpText = "Search for Rockstar Games Launcher games", Hidden = true, Group = "Handlers")]
    public bool Rockstar { get; set; } = false;

    [Option("steam", HelpText = "Search for Steam games", Hidden = true, Group = "Handlers")]
    public bool Steam { get; set; } = false;

    [Option("ubisoft", HelpText = "Search for Ubisoft Connect games", Hidden = true, Group = "Handlers")]
    public bool Ubisoft { get; set; } = false;

    [Option("wargaming", HelpText = "Search for Wargaming.Net Game Center games", Hidden = true, Group = "Handlers")]
    public bool WargamingNet { get; set; } = false;

    [Option("xbox", HelpText = "Search for Xbox Games Pass games", Hidden = true, Group = "Handlers")]
    public bool Xbox { get; set; } = false;

    [Option("wine", HelpText = "Search for Wine prefixes")]
    public bool Wine { get; set; } = false;

    [Option("bottles", HelpText = "Search for Wine prefixes managed with Bottles")]
    public bool Bottles { get; set; } = false;

    [Option("steamapi", MetaValue="KEY", HelpText = "Specify Steam API key from <https://steamcommunity.com/dev/apikey>")]
    public string? SteamAPI { get; set; }
}
