using CommandLine;

namespace GameFinder.Example;

public class Options
{
    [Option("egs", HelpText = "Search for Epic Games Launcher games")]
    public bool EGS { get; set; } = false;

    [Option("gog", HelpText = "Search for GOG GALAXY games")]
    public bool GOG { get; set; } = false;

    [Option("steam", HelpText = "Search for Steam games")]
    public bool Steam { get; set; } = false;
    [Option("steamapi", HelpText = "Specify Steam API key")]
    public string SteamAPI { get; set; } = "E0E30CA0008A997550EB377FA62EF321";

    [Option("origin", HelpText = "Search for Origin games")]
    public bool Origin { get; set; } = false;

    [Option("ea_desktop", HelpText = "Search for EA app games")]
    public bool EADesktop { get; set; } = false;

    [Option("xbox", HelpText = "Search for Xbox Games Pass games")]
    public bool Xbox { get; set; } = false;

    [Option("wine", HelpText = "Search for wine prefixes")]
    public bool Wine { get; set; } = false;

    [Option("bottles", HelpText = "Search for wine prefixes managed with bottles")]
    public bool Bottles { get; set; } = false;

    [Option("bethnet", HelpText = "Search for Bethesda.net games [DEPRECATED]")]
    public bool BethNet { get; set; } = false;

    [Option("amazon", HelpText = "Search for Amazon games")]
    public bool Amazon { get; set; } = false;

    [Option("arc", HelpText = "Search for Arc games")]
    public bool Arc { get; set; } = false;

    [Option("battlenet", HelpText = "Search for Battle.net games")]
    public bool BattleNet { get; set; } = false;

    [Option("bigfish", HelpText = "Search for Big Fish Game Manager games")]
    public bool BigFish { get; set; } = false;

    [Option("gamejolt", HelpText = "Search for Game Jolt Client games")]
    public bool GameJolt { get; set; } = false;

    [Option("humble", HelpText = "Search for Humble App games")]
    public bool Humble { get; set; } = false;

    [Option("igclient", HelpText = "Search for Indiegala Client games")]
    public bool IGClient { get; set; } = false;

    [Option("itch", HelpText = "Search for itch games")]
    public bool Itch { get; set; } = false;

    [Option("legacy", HelpText = "Search for Legacy Games Launcher games")]
    public bool Legacy { get; set; } = false;

    [Option("oculus", HelpText = "Search for Oculus games")]
    public bool Oculus { get; set; } = false;

    [Option("paradox", HelpText = "Search for Paradox Launcher games")]
    public bool Paradox { get; set; } = false;

    [Option("plarium", HelpText = "Search for Plarium Play games")]
    public bool Plarium { get; set; } = false;

    [Option("riot", HelpText = "Search for Riot Client games")]
    public bool Riot { get; set; } = false;

    [Option("rockstar", HelpText = "Search for Rockstar Games Launcher games")]
    public bool Rockstar { get; set; } = false;

    [Option("ubisoft", HelpText = "Search for Ubisoft Connect games")]
    public bool Ubisoft { get; set; } = false;

    [Option("wargaming", HelpText = "Search for Wargaming.Net Game Center games")]
    public bool WargamingNet { get; set; } = false;

    [Option("dolphin", HelpText = "Search for Dolphin ROMs")]
    public bool Dolphin { get; set; } = false;
    [Option("dolphinpath", HelpText = "Specify Dolphin path")]
    public string DolphinPath { get; set; } = @"X:\Emulation\Dolphin\dolphin.exe";

    [Option("mame", HelpText = "Search for MAME ROMs")]
    public bool MAME { get; set; } = false;
    [Option("mamepath", HelpText = "Specify MAME path")]
    public string MAMEPath { get; set; } = @"X:\Emulation\MAME\mame.exe";

    [Option("all", HelpText = "Search for games from all handlers")]
    public bool All { get; set; } = false;
}
