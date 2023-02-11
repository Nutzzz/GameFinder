using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using CommandLine;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Amazon;
using GameFinder.StoreHandlers.Arc;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EADesktop.Crypto;
using GameFinder.StoreHandlers.EADesktop.Crypto.Windows;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
/*
using GameFinder.StoreHandlers.BattleNet;
using GameFinder.StoreHandlers.BigFish;
using GameFinder.StoreHandlers.GameJolt;
using GameFinder.StoreHandlers.Humble;
using GameFinder.StoreHandlers.IGClient;
using GameFinder.StoreHandlers.Itch;
using GameFinder.StoreHandlers.Legacy;
using GameFinder.StoreHandlers.Oculus;
using GameFinder.StoreHandlers.Paradox;
using GameFinder.StoreHandlers.Plarium;
using GameFinder.StoreHandlers.Riot;
using GameFinder.StoreHandlers.Rockstar;
using GameFinder.StoreHandlers.Ubisoft;
using GameFinder.StoreHandlers.WargamingNet;
*/
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace GameFinder.Example;

public static class Program
{
    public static void Main(string[] args)
    {
        var config = new LoggingConfiguration();

        var consoleTarget = new ConsoleTarget("console");
        var fileTarget = new FileTarget("file")
        {
            FileName = "log.log",
        };

        config.AddRuleForAllLevels(consoleTarget);
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

        options.Amazon = true;
        options.Arc = true;
        //options.EADesktop = false;
        //options.EGS = false;
        //options.GOG = false;
        options.Origin = false;
        //options.Steam = false;

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
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("Arc", results, logger);
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

                var handler = new EADesktopHandler();
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("GOG", results, logger);
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("Origin", results, logger);
            }
        }

        if (options.Steam)
        {
            logger.LogDebug("* Steam");
            var handler = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new SteamHandler(new WindowsRegistry())
                : new SteamHandler(registry: null);

            var results = handler.FindAllGamesEx();
            LogGamesAndErrors("Steam", results, logger);
        }

        /*
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("BattleNet", results, logger);
            }
        }

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
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("Humble", results, logger);
            }
        }

        if (options.IGClient)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogError("* Indiegala Client is only supported on Windows!");
            }
            else
            {
                logger.LogDebug("* Indiegala Client");
                var handler = new IGClientHandler();
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("Itch", results, logger);
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
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("Plarium", results, logger);
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("Riot", results, logger);
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("Rockstar", results, logger);
            }
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
                var handler = new UbisoftHandler();
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("Ubisoft", results, logger);
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
                var results = handler.FindAllGamesEx();
                LogGamesAndErrors("WargamingNet", results, logger);
            }
        }
        */
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
