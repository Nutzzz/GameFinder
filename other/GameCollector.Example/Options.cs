using CommandLine;

namespace GameCollector.Example
{
    public class Options
    {
        [Option("amazon", HelpText = "Search for Amazon games")]
        public bool Amazon { get; set; } = true;

        [Option("arc", HelpText = "Search for Arc games")]
        public bool Arc { get; set; } = true;

        [Option("battlenet", HelpText = "Search for Blizzard Battle.net games")]
        public bool BattleNet { get; set; } = true;

        [Option("bethnet", HelpText = "Search for Bethesda.net games [deprecated]")]
        public bool BethNet { get; set; } = false;

        [Option("bigfish", HelpText = "Search for Big Fish games [in progress]")]
        public bool BigFish { get; set; } = false;

        [Option("ea_desktop", HelpText = "Search for EA Desktop games")]
        public bool EADesktop { get; set; } = true;

        [Option("egs", HelpText = "Search for Epic games")]
        public bool EGS { get; set; } = true;

        [Option("gamejolt", HelpText = "Search for Game Jolt games [in progress]")]
        public bool GameJolt { get; set; } = false;

        [Option("gog", HelpText = "Search for GOG games")]
        public bool GOG { get; set; } = true;

        [Option("humble", HelpText = "Search for Humble games [in progress]")]
        public bool Humble { get; set; } = false;

        [Option("indiegala", HelpText = "Search for Indiegala games [in progress]")]
        public bool Indiegala { get; set; } = false;

        [Option("itch", HelpText = "Search for itch games [in progress]")]
        public bool Itch { get; set; } = false;

        [Option("legacy", HelpText = "Search for Legacy games [in progress]")]
        public bool Legacy { get; set; } = false;

        [Option("oculus", HelpText = "Search for Oculus games [in progress]")]
        public bool Oculus { get; set; } = false;

        [Option("origin", HelpText = "Search for Origin games [deprecated]")]
        public bool Origin { get; set; } = false;

        [Option("paradox", HelpText = "Search for Paradox games [in progress]")]
        public bool Paradox { get; set; } = false;

        [Option("plarium", HelpText = "Search for Plarium Play games [in progress]")]
        public bool Plarium { get; set; } = false;

        [Option("riot", HelpText = "Search for Riot games")]
        public bool Riot { get; set; } = true;

        [Option("rockstar", HelpText = "Search for Rockstar games [in progress]")]
        public bool Rockstar { get; set; } = false;

        [Option("steam", HelpText = "Search for Steam games")]
        public bool Steam { get; set; } = true;

        [Option("ubisoft", HelpText = "Search for Ubisoft Connect games")]
        public bool Ubisoft { get; set; } = true;

        [Option("wargaming", HelpText = "Search for Wargaming.net games [in progress]")]
        public bool WargamingNet { get; set; } = false;

        [Option("wine", HelpText = "Search for wine prefixes")]
        public bool Wine { get; set; } = true;

        [Option("xbox", HelpText = "Search for Xbox/Microsoft Store UWP apps/games [deprecated]")]
        public bool Xbox { get; set; } = false;
    }
}
