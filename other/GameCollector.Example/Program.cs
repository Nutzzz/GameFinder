using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using CommandLine;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using GameCollector.StoreHandlers.Amazon;
using GameCollector.StoreHandlers.Arc;
using GameCollector.StoreHandlers.BattleNet;
//using GameCollector.StoreHandlers.BethNet;
using GameCollector.StoreHandlers.EADesktop;
using GameCollector.StoreHandlers.EADesktop.Crypto;
using GameCollector.StoreHandlers.EADesktop.Crypto.Windows;
using GameCollector.StoreHandlers.EGS;
using GameCollector.StoreHandlers.GOG;
using GameCollector.StoreHandlers.IGClient;
using GameCollector.StoreHandlers.Itch;
using GameCollector.StoreHandlers.Origin;
using GameCollector.StoreHandlers.Riot;
using GameCollector.StoreHandlers.Steam;
using GameCollector.StoreHandlers.Ubisoft;
//using GameCollector.StoreHandlers.Xbox;
using GameCollector.Wine;
using GameCollector.Wine.Bottles;
/*
using GameCollector.StoreHandlers.BigFish;
using GameCollector.StoreHandlers.GameJolt;
using GameCollector.StoreHandlers.Humble;
using GameCollector.StoreHandlers.Legacy;
using GameCollector.StoreHandlers.Oculus;
using GameCollector.StoreHandlers.Paradox;
using GameCollector.StoreHandlers.Plarium;
using GameCollector.StoreHandlers.Rockstar;
using GameCollector.StoreHandlers.WargamingNet;
*/
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace GameCollector.Example;

public static class Program
{
    public static void Main(string[] args)
    {
        var config = new LoggingConfiguration();

        var coloredConsoleTarget = new ColoredConsoleTarget("coloredConsole")
        {
            EnableAnsiOutput = true,
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
            Layout = "${longdate}|${level:uppercase=true}|${message:withexception=true}"
        };

        var fileTarget = new FileTarget("file")
        {
            FileName = "log.log",
        };

        config.AddRuleForAllLevels(coloredConsoleTarget);
        config.AddRuleForAllLevels(fileTarget);

        LogManager.Configuration = config;

        var logger = new NLogLoggerProvider().CreateLogger(nameof(Program));

        Parser.Default
            .ParseArguments<Options>(args)
            .WithParsed(x => Run(x, logger));
    }

    [SuppressMessage("Design", "MA0051:Method is too long")]
    private static void Run(Options options, ILogger logger)
    {
        if (File.Exists("log.log")) File.Delete("log.log");

        /*
        options.Amazon = false;      // NEW
        options.Arc = false;         // NEW
        options.BattleNet = false;   // NEW
        options.EADesktop = false;
        options.EGS = false;
        options.GOG = false;
        options.IGClient = true;     // NEWEST
        options.Itch = true;         // NEWEST
        options.Origin = false;
        options.Riot = false;        // NEW
        options.Steam = false;
        options.Ubisoft = false;     // NEW
        */

        if (options.Amazon)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Amazon Games is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Amazon Games");
                var handler = new AmazonHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Amazon", results, logger);
            }
        }

        if (options.Arc)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Arc is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Arc");
                var handler = new ArcHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Arc", results, logger);
            }
        }

        if (options.BattleNet)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Blizzard Battle.net is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Blizzard Battle.net");
                var handler = new BattleNetHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("BattleNet", results, logger);
            }
        }

        if (options.EADesktop)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* EA Desktop is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* EA Desktop");
                var decryptionKey = Decryption.CreateDecryptionKey(new HardwareInfoProvider());
                var sDecryptionKey = Convert.ToHexString(decryptionKey).ToLower(CultureInfo.InvariantCulture);
                logger.LogDebug("EA decryption key: {}", sDecryptionKey);

                var handler = new EADesktopHandler { SchemaPolicy = StoreHandlers.EADesktop.SchemaPolicy.Ignore, };
                var results = handler.FindAllGames();
                LogGamesAndErrors("EADesktop", results, logger);
            }
        }

        if (options.EGS)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Epic Games Store is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Epic Games Store");
                var handler = new EGSHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("EGS", results, logger);
            }
        }

        if (options.GOG)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* GOG Galaxy is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* GOG Galaxy");
                var handler = new GOGHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("GOG", results, logger);
            }
        }

        if (options.IGClient)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Indiegala IGClient is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Indiegala IGClient");
                var handler = new IGClientHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("IGClient", results, logger);
            }
        }

        if (options.Itch)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* itch is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* itch");
                var handler = new ItchHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Itch", results, logger);
            }
        }

        if (options.Origin)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* EA Origin is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* EA Origin");
                var handler = new OriginHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Origin", results, logger);
            }
        }

        if (options.Riot)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Riot Client is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Riot Client");
                var handler = new RiotHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Riot", results, logger);
            }
        }

        if (options.Steam)
        {
            logger.LogDebug("* Steam");
            var handler = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new SteamHandler(new WindowsRegistry())
                : new SteamHandler(registry: null);

            var results = handler.FindAllGames();
            LogGamesAndErrors("Steam", results, logger);
        }

        if (options.Ubisoft)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Ubisoft Connect is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Ubisoft Connect");
                var handler = new UbisoftHandler { SchemaPolicy = StoreHandlers.Ubisoft.SchemaPolicy.Ignore, };
                var results = handler.FindAllGames();
                LogGamesAndErrors("Ubisoft", results, logger);
            }
        }

        if (options.Wine)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                logger.LogError("Wine is only supported on Linux!");
            }
            else
            {
                var prefixManager = new DefaultWinePrefixManager(new FileSystem());
                foreach (var result in prefixManager.FindPrefixes())
                {
                    result.Switch(prefix =>
                    {
                        logger.LogInformation($"Found wine prefix at {prefix.ConfigurationDirectory}");
                    }, error =>
                    {
                        logger.LogError(error.Value);
                    });
                }
            }
        }
        
        if (options.Bottles)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                logger.LogError("Bottles is only supported on Linux!");
            }
            else
            {
                var prefixManager = new BottlesWinePrefixManager(new FileSystem());
                LogWinePrefixes(prefixManager, logger);
            }
        }
        
        /*
        if (options.BethNet)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Bethesda.net is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Bethesda.net");
                var handler = new BethNetHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("BethNet", results, logger);
            }
        }

        if (options.Xbox)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Xbox/Microsoft Store UWP is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Xbox/Microsoft Store UWP");
                var handler = new XboxHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Xbox", results, logger);
            }
        }
        */

        /*
        if (options.BigFish)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Big Fish Games is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Big Fish Games");
                var handler = new BigFishHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("BigFish", results, logger);
            }
        }

        if (options.GameJolt)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Game Jolt Client is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Game Jolt Client");
                var handler = new GameJoltHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("GameJolt", results, logger);
            }
        }

        if (options.Humble)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Humble App is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Humble App");
                var handler = new HumbleHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Humble", results, logger);
            }
        }

        if (options.Legacy)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Legacy Games is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Legacy Games");
                var handler = new LegacyHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Legacy", results, logger);
            }
        }

        if (options.Oculus)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Oculus is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Oculus");
                var handler = new OculusHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Oculus", results, logger);
            }
        }

        if (options.Paradox)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Paradox Launcher is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Paradox Launcher");
                var handler = new ParadoxHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Paradox", results, logger);
            }
        }

        if (options.Plarium)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Plarium is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Plarium");
                var handler = new PlariumHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Plarium", results, logger);
            }
        }

        if (options.Rockstar)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Rockstar Games Launcher is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Rockstar Games Launcher");
                var handler = new RockstarHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("Rockstar", results, logger);
            }
        }

        if (options.WargamingNet)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Wargaming.net Game Center is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Wargaming.net Game Center");
                var handler = new WargamingNetHandler();
                var results = handler.FindAllGames();
                LogGamesAndErrors("WargamingNet", results, logger);
            }
        }
        */
    }
    
    private static void LogWinePrefixes<TWinePrefix>(
        IWinePrefixManager<TWinePrefix> prefixManager, ILogger logger)
    where TWinePrefix : AWinePrefix
    {
        foreach (var result in prefixManager.FindPrefixes())
        {
            result.Switch(prefix =>
            {
                logger.LogInformation($"Found wine prefix at {prefix.ConfigurationDirectory}");
            }, error =>
            {
                logger.LogError(error.Value);
            });
        }
    }

    private static void LogGamesAndErrors<TGame>(string handler, IEnumerable<Result<TGame>> results, ILogger logger)
        where TGame: class
    {
        foreach (var (game, error) in results)
        {
            if (game is not null)
            {
                logger.LogInformation("Found {} {}", handler, game);
            }
            else
            {
                logger.LogError("{}", error);
            }
        }
    }
}
