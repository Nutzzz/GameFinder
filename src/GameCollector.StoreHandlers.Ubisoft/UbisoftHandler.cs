using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using JetBrains.Annotations;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using GameCollector.YamlUtils;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using YamlDotNet.Core.Tokens;

namespace GameCollector.StoreHandlers.Ubisoft;

/// <summary>
/// Handler for finding games installed with Ubisoft Connect.
/// </summary>
[PublicAPI]
public class UbisoftHandler : AHandler<Game, string>
{
    internal const string UninstallRegKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// The supported schema version of this handler. You can change the schema policy with
    /// <see cref="SchemaPolicy"/>.
    /// </summary>
    public const decimal SupportedSchemaVersion = 2.0M;

    /// <summary>
    /// Policy to use when the schema version does not match <see cref="SupportedSchemaVersion"/>.
    /// The default behavior is <see cref="Ubisoft.SchemaPolicy.Warn"/>.
    /// </summary>
    public SchemaPolicy SchemaPolicy { get; set; } = SchemaPolicy.Warn;

    /// <summary>
    /// Default constructor. This uses the <see cref="WindowsRegistry"/> implementation of
    /// <see cref="IRegistry"/> and the real file system with <see cref="FileSystem"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public UbisoftHandler() : this(new WindowsRegistry(), new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/>. This uses
    /// the real file system with <see cref="FileSystem"/>.
    /// </summary>
    /// <param name="registry"></param>
    public UbisoftHandler(IRegistry registry) : this(registry, new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/> and
    /// <see cref="IFileSystem"/> when doing tests.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="fileSystem"></param>
    public UbisoftHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var localMachine32 = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

        using var unKey = localMachine32.OpenSubKey(UninstallRegKey);
        if (unKey is null)
        {
            yield return Result.FromError<Game>($"Unable to open HKEY_LOCAL_MACHINE\\{UninstallRegKey}");
            yield break;
        }

        var subKeyNames = unKey.GetSubKeyNames().Where(
            keyName => keyName[(keyName.LastIndexOf('\\') + 1)..].StartsWith("Uplay Install ", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (subKeyNames.Length == 0)
        {
            yield return Result.FromError<Game>($"Registry key {unKey.GetName()} has no sub-keys beginning with \"Uplay Install \"");
            yield break;
        }

        List<string> ubiIds = new();
        foreach (var subKeyName in subKeyNames)
        {
            yield return ParseSubKey(unKey, subKeyName, out string? iconId);
            if (iconId is not null)
                ubiIds.Add(iconId);

        }

        var ubiKey = unKey.OpenSubKey("Uplay");
        if (ubiKey is null || !ubiKey.TryGetString("InstallLocation", out var launcherPath))
        {
            yield break;
        }

        if (installedOnly) yield break;

        // Get owned but not-installed games

        var configFile = _fileSystem.FileInfo.New(Path.Combine(launcherPath, @"cache\configuration\configurations"));
        if (configFile.Exists)
        {
            // This file is mostly yaml text, but there is binary before each entry that I attempt to strip out
            // Each entry is expected to start with "version:" (schema)
            using var stream = new FileStream(configFile.FullName, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            List<string> input = new();
            do
            {
                bool parse = false;
                string? line = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        line = reader.ReadLine();
                        continue;
                    }
                    if (line.Contains("version: ", StringComparison.Ordinal) &&
                        !line.Contains(" version:", StringComparison.Ordinal))
                    {
                        line = line[line.IndexOf("version: ", StringComparison.Ordinal)..];
                        /*
                        var sVersion = line[(line.IndexOf("version: ", StringComparison.Ordinal) + 9)..];
                        if (decimal.TryParse(sVersion, out var version) &&
                            version < SupportedSchemaVersion)
                        {
                            var (schemaMessage, isSchemaError) = CreateSchemaVersionMessage(SchemaPolicy, version, configFile.FullName);
                            if (schemaMessage is not null)
                            {
                                yield return Result.FromError<Game>(schemaMessage);
                                if (isSchemaError)
                                    yield break;
                                break;
                            }
                        }
                        */
                        break;
                    }
                    
                    if (line.Contains('�', StringComparison.Ordinal) ||
                        line.Contains('', StringComparison.Ordinal) ||
                        //line.Contains('~', StringComparison.Ordinal) ||
                        line.StartsWith('#') ||
                        line.EndsWith("#--------------------------------------------------------------------------", StringComparison.Ordinal))
                    {
                        line = reader.ReadLine();
                        continue;
                    }
                    parse = true;
                    input.Add(line);
                    line = reader.ReadLine();
                }
                if (parse)
                {
                    yield return ParseConfigFile(string.Join('\n', input), launcherPath, _registry, ubiIds);
                }
                input = new();
                if (line is not null &&
                    line.Contains("version: ", StringComparison.Ordinal) &&
                    !line.Contains(" version:", StringComparison.Ordinal))
                {
                    input.Add(line);
                }
            }
            while (!reader.EndOfStream);
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game);
    }

    private static Result<Game> ParseConfigFile(string input, string launcherPath, IRegistry registry, List<string> ubiIds)
    {
        string id = "";
        string name = "";
        string path = "";
        string launch = "";
        string icon = "";
        string iconFile = "";
        bool isInstalled = false;

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var config = deserializer.Deserialize<ConfigFile>(input);

            if (config is null || config.Root is null)
                return Result.FromError<Game>("configurations file has no root property!");

            /*
            if (!config.Version.HasValue)
            {
                //return null;
                return Result.FromError<Game>("configurations file does not have a schema version!");
            }
            */

            name = config.Root.DisplayName ?? "";
            if (string.IsNullOrEmpty(name))
            {
                name = config.Root.Name ?? "";
            }
            if (string.IsNullOrEmpty(name))
                return Result.FromError<Game>($"configurations file has no \"root\">\"name\" property!");

            if (config.Root.StartGame is null ||
                (config.Root.IsDlc is not null && ToBool(config.Root.IsDlc)) ||
                (config.Root.IsUlc is not null && ToBool(config.Root.IsUlc)) ||
                (config.Root.OptionalAddonEnabledByDefault is not null &&
                ToBool(config.Root.OptionalAddonEnabledByDefault)))
            {
                return Result.FromError<Game>($"{name} is not a base game!");
            }

            // fallback ID (if available, we'll get the icon filename instead)
            if (config.Root.Uplay is not null)
            {
                if (config.Root.Uplay.GameCode is not null)
                    id = config.Root.Uplay.GameCode;
                else if (config.Root.Uplay.AchievementsSyncId is not null)
                    id = config.Root.Uplay.AchievementsSyncId;
            }

            /*
            if (config.Root.ThirdPartyPlatform is not null)
                return Result.FromError<Game>($"{name} [{id}] is a third-party platform game!"); // e.g., a Steam game
            */

            iconFile = config.Root.IconImage ?? "";
            if (string.IsNullOrEmpty(iconFile))
                iconFile = config.Root.ThumbImage ?? "";

            if (config.Localizations is not null)
            {
                /*
                if (config.Localizations.Default is null)
                    return Result.FromError<Game>($"No \"localizations\">\"default\" found for {name} [{id}].");
                */
                name = Localize(name, config.Localizations.Default);
                iconFile = Localize(iconFile, config.Localizations.Default);
            }

            if (string.IsNullOrEmpty(name) &&
                config.Root.Installer is not null &&
                config.Root.Installer.GameIdentifier is not null)
            {
                name = config.Root.Installer.GameIdentifier;
            }

            if (iconFile.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                iconFile.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                iconFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
                    name = id;
                id = Path.GetFileNameWithoutExtension(iconFile);
                if (ubiIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                    return Result.FromError<Game>($"{name} was already found!");
            }
            if (string.IsNullOrEmpty(name))
                name = id;

            // See if path and exe can be found in registry
            var configGame = config.Root.StartGame.Online;
            if (configGame is not null &&
                configGame.Executables is not null &&
                configGame.Executables.Count > 0)
            {
                if (configGame.Executables[0] is not null &&
                    configGame.Executables[0].WorkingDirectory is not null)
                {
                    var configPath = configGame.Executables[0].WorkingDirectory;
                    if (configPath is not null && configPath.Register is not null)
                    {
                        var sRegistry = configPath.Register;
                        if (sRegistry.Contains('\\', StringComparison.Ordinal))
                        {
                            var hive32 = registry.OpenBaseKey(
                                RegistryHelpers.RegistryHiveFromString(
                                    sRegistry[..sRegistry.IndexOf('\\', StringComparison.OrdinalIgnoreCase)]),
                                RegistryView.Registry32);
                            var sRegKey = sRegistry[(sRegistry.IndexOf('\\', StringComparison.Ordinal) + 1)..sRegistry.LastIndexOf('\\')];
                            using var regKey = hive32.OpenSubKey(sRegKey);
                            if (regKey is not null)
                            {
                                var value = regKey.GetValue(Path.GetFileName(sRegistry));
                                if (value is not null)
                                {
                                    path = value.ToString() ?? "";
                                    var configExe = configGame.Executables[0].Path;
                                    if (configExe is not null && configExe.Relative is not null)
                                        launch = Path.Combine(path, configExe.Relative);
                                    if (File.Exists(launch))
                                        isInstalled = true;
                                }

                            }
                            /*
                            else
                                return Result.FromError<Game>($"Unable to open {sRegKey}");
                            */
                        }
                    }
                }
            }

            return Result.FromGame(new Game(
                Id: id,
                Name: name,
                Path: path,
                Launch: "",
                Icon: icon,
                Uninstall: "",
                IsInstalled: isInstalled,
                Metadata: new(StringComparer.OrdinalIgnoreCase)));
        }
		catch (Exception e)
		{
            return Result.FromException<Game>($"Exception while parsing configurations file\n{e.InnerException}", e);
		}
    }

    internal static (string? message, bool isError) CreateSchemaVersionMessage(
        SchemaPolicy schemaPolicy, decimal schemaVersion, string configFilePath)
    {
        if (schemaVersion == SupportedSchemaVersion) return (null, false);

        return schemaPolicy switch
        {
            SchemaPolicy.Warn => (
                $"File {configFilePath} has a schema version " +
                $"{schemaVersion.ToString(CultureInfo.InvariantCulture)} but this library only supports schema version " +
                $"{SupportedSchemaVersion.ToString(CultureInfo.InvariantCulture)}. " +
                $"This message is a WARNING because the consumer of this library has set {nameof(SchemaPolicy)} to {nameof(SchemaPolicy.Warn)}",
                false),
            SchemaPolicy.Error => (
                $"File {configFilePath} has a schema version " +
                $"{schemaVersion.ToString(CultureInfo.InvariantCulture)} but this library only supports schema version " +
                $"{SupportedSchemaVersion.ToString(CultureInfo.InvariantCulture)}. " +
                $"This is an ERROR because the consumer of this library has set {nameof(SchemaPolicy)} to {nameof(SchemaPolicy.Error)}",
                true),
            SchemaPolicy.Ignore => (null, false),
            _ => throw new ArgumentOutOfRangeException(nameof(schemaPolicy), schemaPolicy, message: null),
        };
    }

    private static string Localize(string key, ConfigLocalLang? lang)
    {
        if (lang is not null)
        {
            switch (key)
            {
                case "NAME":
                    return lang.NAME ?? "";
                case "GAMENAME":
                    return lang.GAMENAME ?? "";
                case "ICONIMAGE":
                    return lang.ICONIMAGE ?? "";
                case "THUMBIMAGE":
                    return lang.THUMBIMAGE ?? "";
                case "DESCRIPTION":
                    return lang.DESCRIPTION ?? "";
                case "l1":
                    return lang.l1 ?? "";
                case "l2":
                    return lang.l2 ?? "";
                case "l3":
                    return lang.l3 ?? "";
                case "l4":
                    return lang.l4 ?? "";
                case "l5":
                    return lang.l5 ?? "";
                default:
                    break;
            }
        }
        return key;
    }

    private static bool ToBool(string key, bool bDefault = false)
    {
        return key.ToLower(CultureInfo.InvariantCulture) switch
        {
            "y" or "yes" or "true" or "on" => true,
            "n" or "no" or "false" or "off" => false,
            _ => bDefault,
        };
    }

    private static Result<Game> ParseSubKey(IRegistryKey unKey, string subKeyName, out string? sId)
    {
        sId = "";
        try
        {
            using var subKey = unKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return Result.FromError<Game>($"Unable to open {unKey}\\{subKeyName}");
            }

            sId = subKeyName[14..];
            if (!int.TryParse(sId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                return Result.FromError<Game>($"The subkey name of {subKey.GetName()} does not end with a number: \"{sId}\"");
            }

            if (!subKey.TryGetString("InstallLocation", out var path))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"InstallLocation\"");
            }

            if (!subKey.TryGetString("DisplayName", out var name))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"DisplayName\"");
            }

            string launch = "uplay://launch/" + id.ToString(CultureInfo.InvariantCulture);
            if (!subKey.TryGetString("DisplayIcon", out var icon)) icon = "";
            if (icon.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                sId = Path.GetFileNameWithoutExtension(icon);
            if (!subKey.TryGetString("UninstallString", out var uninstall)) uninstall = "";

            return Result.FromGame(new Game(
                Id: sId,
                Name: name,
                Path: path,
                Launch: launch,
                Icon: icon,
                Uninstall: uninstall,
                Metadata: new(StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception e)
        {
            return Result.FromException<Game>($"Exception while parsing registry key {unKey}\\{subKeyName}", e);
        }
    }
}
