using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using CommandLine;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EADesktop.Crypto;
using GameFinder.StoreHandlers.EADesktop.Crypto.Windows;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Xbox;
using GameFinder.Wine;
using GameFinder.Wine.Bottles;
using GameCollector.StoreHandlers.Amazon;
using GameCollector.StoreHandlers.Arc;
using GameCollector.StoreHandlers.BattleNet;
using GameCollector.StoreHandlers.BigFish;
using GameCollector.StoreHandlers.GameJolt;
using GameCollector.StoreHandlers.Humble;
using GameCollector.StoreHandlers.IGClient;
using GameCollector.StoreHandlers.Itch;
using GameCollector.StoreHandlers.Legacy;
using GameCollector.StoreHandlers.Oculus;
using GameCollector.StoreHandlers.Paradox;
using GameCollector.StoreHandlers.Plarium;
using GameCollector.StoreHandlers.Riot;
using GameCollector.StoreHandlers.Rockstar;
using GameCollector.StoreHandlers.Ubisoft;
using GameCollector.StoreHandlers.WargamingNet;
using GameCollector.EmuHandlers.Dolphin;
using GameCollector.EmuHandlers.MAME;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using OneOf;
using FileSystem = NexusMods.Paths.FileSystem;
using IFileSystem = NexusMods.Paths.IFileSystem;
using ILogger = Microsoft.Extensions.Logging.ILogger;

[assembly: ExcludeFromCodeCoverage]
namespace GameFinder.Example;

public static class Program
{
    private static NLogLoggerProvider _provider = null!;

    public static void Main(string[] args)
    {
        var config = new LoggingConfiguration();

        var coloredConsoleTarget = new ColoredConsoleTarget("coloredConsole")
        {
            DetectConsoleAvailable = true,
            EnableAnsiOutput = OperatingSystem.IsLinux(), // windows hates this
            UseDefaultRowHighlightingRules = false,
            WordHighlightingRules =
            {
                new ConsoleWordHighlightingRule
                {
                    Regex = @"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\.\d+",
                    CompileRegex = true,
                    ForegroundColor = ConsoleOutputColor.Gray,
                },
                new ConsoleWordHighlightingRule("DEBUG", ConsoleOutputColor.Gray, ConsoleOutputColor.NoChange),
                new ConsoleWordHighlightingRule("INFO", ConsoleOutputColor.Cyan, ConsoleOutputColor.NoChange),
                new ConsoleWordHighlightingRule("ERROR", ConsoleOutputColor.Red, ConsoleOutputColor.NoChange),
                new ConsoleWordHighlightingRule("WARNING", ConsoleOutputColor.Yellow, ConsoleOutputColor.NoChange),
            },
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message:withexception=true}",
        };

        var fileTarget = new FileTarget("file")
        {
            FileName = "log.log",
        };

        config.AddRuleForAllLevels(coloredConsoleTarget);
        config.AddRuleForAllLevels(fileTarget);

        LogManager.Configuration = config;
        _provider = new NLogLoggerProvider();

        var logger = _provider.CreateLogger(nameof(Program));

        Parser.Default
            .ParseArguments<Options>(args)
            .WithParsed(x => Run(x, logger));
    }

    [SuppressMessage("Design", "MA0051:Method is too long")]
    private static void Run(Options options, ILogger logger)
    {
        var realFileSystem = FileSystem.Shared;

        var logFile = realFileSystem.GetKnownPath(KnownPath.CurrentDirectory).CombineUnchecked("log.log");
        if (realFileSystem.FileExists(logFile)) realFileSystem.DeleteFile(logFile);

        logger.LogInformation("Operating System: {OSDescription}", RuntimeInformation.OSDescription);

        if (options.All) // Enable all handlers
        {
            options.Amazon = true;
            options.Arc = true;
            options.BattleNet = true;
            options.BigFish = true;
            options.Dolphin ??= "";
            options.EADesktop = true;
            options.EGS = true;
            options.GameJolt = true;
            options.GOG = true;
            options.Humble = true;
            options.IGClient = true;
            options.Itch = true;
            options.Legacy = true;
            options.MAME ??= "";
            options.Oculus = true;
            options.Origin = true;
            options.Paradox = true;
            options.Plarium = true;
            options.Riot = true;
            options.Rockstar = true;
            options.Steam = true;
            options.Ubisoft = true;
            options.WargamingNet = true;
            options.Xbox = true;
        }

        if (OperatingSystem.IsWindows())
        {
            var windowsRegistry = WindowsRegistry.Shared;
            if (options.Steam) RunSteamHandler(realFileSystem, windowsRegistry, options.SteamAPI);
            if (options.GOG) RunGOGHandler(windowsRegistry, realFileSystem);
            if (options.EGS) RunEGSHandler(windowsRegistry, realFileSystem);
            if (options.Origin) RunOriginHandler(realFileSystem);
            if (options.Xbox) RunXboxHandler(realFileSystem);
            if (options.EADesktop)
            {
                var hardwareInfoProvider = new HardwareInfoProvider();
                var decryptionKey = Decryption.CreateDecryptionKey(new HardwareInfoProvider());
                var sDecryptionKey = Convert.ToHexString(decryptionKey).ToLower(CultureInfo.InvariantCulture);
                logger.LogDebug("EA Decryption Key: {DecryptionKey}", sDecryptionKey);

                RunEADesktopHandler(realFileSystem, windowsRegistry, hardwareInfoProvider);
            }
            if (options.Amazon) RunAmazonHandler(windowsRegistry, realFileSystem);
            if (options.Arc) RunArcHandler(windowsRegistry, realFileSystem);
            if (options.BattleNet) RunBattleNetHandler(realFileSystem);
            if (options.BigFish) RunBigFishHandler(windowsRegistry, realFileSystem);
            if (options.GameJolt) RunGameJoltHandler(realFileSystem);
            if (options.Humble) RunHumbleHandler(windowsRegistry, realFileSystem);
            if (options.IGClient) RunIGClientHandler(realFileSystem);
            if (options.Itch) RunItchHandler(realFileSystem);
            if (options.Legacy) RunLegacyHandler(windowsRegistry, realFileSystem);
            if (options.Oculus) RunOculusHandler(windowsRegistry, realFileSystem);
            if (options.Paradox) RunParadoxHandler(windowsRegistry, realFileSystem);
            if (options.Plarium) RunPlariumHandler(realFileSystem);
            if (options.Riot) RunRiotHandler(realFileSystem);
            if (options.Rockstar) RunRockstarHandler(windowsRegistry, realFileSystem);
            if (options.Ubisoft) RunUbisoftHandler(windowsRegistry, realFileSystem);
            if (options.WargamingNet) RunWargamingNetHandler(windowsRegistry, realFileSystem);

            if (options.Dolphin is not null)
            {
                if (Path.IsPathRooted(options.Dolphin))
                {
                    var path = realFileSystem.FromFullPath(options.Dolphin);
                    RunDolphinHandler(realFileSystem, windowsRegistry, path);
                }
                else
                    logger.LogError("Bad Dolphin path {DolphinPath}", options.Dolphin);
            }

            if (options.MAME is not null)
            {
                if (Path.IsPathRooted(options.MAME))
                {
                    var path = realFileSystem.FromFullPath(options.MAME);
                    RunMAMEHandler(realFileSystem, path);
                }
                else
                    logger.LogError("Bad MAME path {MAMEPath}", options.MAME);
            }
        }

        if (OperatingSystem.IsLinux())
        {
            if (options.Steam) RunSteamHandler(realFileSystem, registry: null, options.SteamAPI);
            var winePrefixes = new List<AWinePrefix>();

            if (options.Wine)
            {
                var prefixManager = new DefaultWinePrefixManager(realFileSystem);
                winePrefixes.AddRange(LogWinePrefixes(prefixManager, _provider.CreateLogger("Wine")));
            }

            if (options.Bottles)
            {
                var prefixManager = new BottlesWinePrefixManager(realFileSystem);
                winePrefixes.AddRange(LogWinePrefixes(prefixManager, _provider.CreateLogger("Bottles")));
            }

            foreach (var winePrefix in winePrefixes)
            {
                var wineFileSystem = winePrefix.CreateOverlayFileSystem(realFileSystem);
                var wineRegistry = winePrefix.CreateRegistry(realFileSystem);

                if (options.GOG) RunGOGHandler(wineRegistry, wineFileSystem);
                if (options.EGS) RunEGSHandler(wineRegistry, wineFileSystem);
                if (options.Origin) RunOriginHandler(wineFileSystem);
                if (options.Xbox) RunXboxHandler(wineFileSystem);
            }
        }

        logger.LogInformation($"{nameof(Program)} complete");
    }

    private static void RunGOGHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(GOGHandler));
        var handler = new GOGHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunEGSHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(EGSHandler));
        var handler = new EGSHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunOriginHandler(IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(OriginHandler));
        var handler = new OriginHandler(fileSystem, null);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunEADesktopHandler(
        IFileSystem fileSystem,
        IRegistry registry,
        IHardwareInfoProvider hardwareInfoProvider)
    {
        var logger = _provider.CreateLogger(nameof(EADesktopHandler));
        var handler = new EADesktopHandler(fileSystem, registry, hardwareInfoProvider);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCodeAttribute",
        Justification = "Required types are preserved using TrimmerRootDescriptor file.")]
    private static void RunXboxHandler(IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(XboxHandler));
        var handler = new XboxHandler(fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunSteamHandler(IFileSystem fileSystem, IRegistry? registry, string? steamAPI)
    {
        var logger = _provider.CreateLogger(nameof(SteamHandler));
        var handler = new SteamHandler(fileSystem, registry, steamAPI);
        LogGamesAndErrors(handler.FindAllGames(), logger, game =>
        {
            if (!OperatingSystem.IsLinux()) return;
            var protonPrefix = game.GetProtonPrefix();
            if (!fileSystem.DirectoryExists(protonPrefix.ConfigurationDirectory)) return;
            logger.LogInformation("Proton Directory for this game: {}", protonPrefix.ProtonDirectory.GetFullPath());
        });
    }

    private static void RunAmazonHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(AmazonHandler));
        var handler = new AmazonHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunArcHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(ArcHandler));
        var handler = new ArcHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunBattleNetHandler(IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(BattleNetHandler));
        var handler = new BattleNetHandler(fileSystem, null);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunBigFishHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(BigFishHandler));
        var handler = new BigFishHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunGameJoltHandler(IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(GameJoltHandler));
        var handler = new GameJoltHandler(fileSystem, null);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunHumbleHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(HumbleHandler));
        var handler = new HumbleHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunIGClientHandler(IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(IGClientHandler));
        var handler = new IGClientHandler(fileSystem, null);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunItchHandler(IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(ItchHandler));
        var handler = new ItchHandler(fileSystem, null);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunLegacyHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(LegacyHandler));
        var handler = new LegacyHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunOculusHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(OculusHandler));
        var handler = new OculusHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunParadoxHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(ParadoxHandler));
        var handler = new ParadoxHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunPlariumHandler(IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(PlariumHandler));
        var handler = new PlariumHandler(fileSystem, null);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunRiotHandler(IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(RiotHandler));
        var handler = new RiotHandler(fileSystem, null);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunRockstarHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(RockstarHandler));
        var handler = new RockstarHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunUbisoftHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(UbisoftHandler));
        var handler = new UbisoftHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunWargamingNetHandler(IRegistry registry, IFileSystem fileSystem)
    {
        var logger = _provider.CreateLogger(nameof(WargamingNetHandler));
        var handler = new WargamingNetHandler(registry, fileSystem);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunDolphinHandler(IFileSystem fileSystem, IRegistry registry, AbsolutePath path)
    {
        var logger = _provider.CreateLogger(nameof(DolphinHandler));
        var handler = new DolphinHandler(fileSystem, registry, path); //, logger);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static void RunMAMEHandler(IFileSystem fileSystem, AbsolutePath path)
    {
        var logger = _provider.CreateLogger(nameof(MAMEHandler));
        var handler = new MAMEHandler(fileSystem, path); //, logger);
        LogGamesAndErrors(handler.FindAllGames(), logger);
    }

    private static List<AWinePrefix> LogWinePrefixes<TWinePrefix>(IWinePrefixManager<TWinePrefix> prefixManager, ILogger logger)
    where TWinePrefix : AWinePrefix
    {
        var res = new List<AWinePrefix>();

        foreach (var result in prefixManager.FindPrefixes())
        {
            result.Switch(prefix =>
            {
                logger.LogInformation("Found wine prefix at {PrefixConfigurationDirectory}", prefix.ConfigurationDirectory);
                res.Add(prefix);
            }, error =>
            {
                logger.LogError("{Error}", error);
            });
        }

        return res;
    }

    private static void LogGamesAndErrors<TGame>(IEnumerable<OneOf<TGame, ErrorMessage>> results, ILogger logger, Action<TGame>? action = null)
        where TGame : class
    {
        foreach (var result in results)
        {
            result.Switch(game =>
            {
                logger.LogInformation("Found {Game}", game);
                action?.Invoke(game);
            }, error =>
            {
                logger.LogError("{Error}", error);
            });
        }
    }
}
