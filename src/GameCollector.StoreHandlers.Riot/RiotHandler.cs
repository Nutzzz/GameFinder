using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using JetBrains.Annotations;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using static System.Environment;

namespace GameCollector.StoreHandlers.Riot;

/// <summary>
/// Handler for finding games installed with Riot Client.
/// </summary>
[PublicAPI]
public class RiotHandler : AHandler<Game, string>
{
    internal const string RiotRegKey = @"Software\Perfect World Entertainment\Core";

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new()
        {
            AllowTrailingCommas = true,
        };

    /// <summary>
    /// Default constructor. This uses the <see cref="WindowsRegistry"/> implementation of
    /// <see cref="IRegistry"/> and the real file system with <see cref="FileSystem"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public RiotHandler() : this(new WindowsRegistry(), new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/>. This uses
    /// the real file system with <see cref="FileSystem"/>.
    /// </summary>
    /// <param name="registry"></param>
    public RiotHandler(IRegistry registry) : this(registry, new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/> and
    /// <see cref="IFileSystem"/> when doing tests.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="fileSystem"></param>
    public RiotHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var clientFile = _fileSystem.FileInfo.New(_fileSystem.Path.Combine(GetFolderPath(SpecialFolder.CommonApplicationData), "Riot Games", "RiotClientInstalls.json"));
        var metaDir = _fileSystem.DirectoryInfo.New(_fileSystem.Path.Combine(GetFolderPath(SpecialFolder.CommonApplicationData), "Riot Games", "Metadata"));
        if (!clientFile.Exists)
        {
            yield return Result.FromError<Game>($"The client install file {clientFile.FullName} does not exist!");
            yield break;
        }

        // could also get client path from HKEY_CLASSES_ROOT\riotclient\shell\open\command\(Default) though I'm not sure how to use "--app-command=" properly
        var clientPath = "";

        using var stream = clientFile.OpenRead();
        var client = JsonSerializer.Deserialize<ClientInstallFile>(stream, _jsonSerializerOptions);
        if (client is not null && client.RcLive is not null)
            clientPath = client.RcLive;

        var settingsFiles = metaDir
            .EnumerateFiles("*.yaml", SearchOption.AllDirectories)
            .ToArray();

        if (settingsFiles.Length == 0)
        {
            yield return Result.FromError<Game>($"The metadata directory {metaDir.FullName} does not contain any .yaml files");
            yield break;
        }

        foreach (var file in settingsFiles)
        {
            yield return DeserializeGame(file, clientPath);
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game);
    }

    private Result<Game> DeserializeGame(IFileInfo settingsFile, string clientPath)
    {
        using var stream = settingsFile.OpenRead();

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var game = deserializer.Deserialize<SettingsFile>(new StreamReader(stream).ReadToEnd());

            if (game is null)
            {
                return Result.FromError<Game>($"Unable to deserialize file {settingsFile.FullName}");
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (game.UserDataPaths is not null) // This should only be true for "Riot Client.settings.yaml"
            {
                return Result.FromError<Game>($"Not a game file {settingsFile.FullName}");
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (game.ProductInstallFullPath is null || game.ProductInstallRoot is null)
            {
                return Result.FromError<Game>($"No \"product_install_full_path\" property in file {settingsFile.FullName}");
            }

            var name = game.ShortcutName ?? settingsFile.Name[..settingsFile.Name.IndexOf('.', StringComparison.Ordinal)];

            var id = game.ProductInstallFullPath[(game.ProductInstallRoot.Length + 1)..];
            if (id.Contains('/', StringComparison.Ordinal))
                id = id[..id.IndexOf('/', StringComparison.Ordinal)];

            var product = settingsFile.Name;
            if (product.Contains('.', StringComparison.Ordinal))
                product = product[..product.IndexOf('.', StringComparison.Ordinal)].ToLower(CultureInfo.InvariantCulture);

            var launch = clientPath;
            var launchArgs = "--launch-product=" + product;
            var uninstall = clientPath;
            var uninstallArgs = "--uninstall-product=" + product;
            if (settingsFile.Name.Contains(".live.", StringComparison.OrdinalIgnoreCase))
            {
                launchArgs += " --launch-patchline=live";
                uninstallArgs += " --uninstall-patchline=live";
            }

            var icon = "";
            if (settingsFile.DirectoryName is not null)
            {
                foreach (var iconFile in _fileSystem.Directory.EnumerateFiles(settingsFile.DirectoryName, "*.ico"))
                {
                    icon = iconFile;
                    break;
                }
            }
            if (string.IsNullOrEmpty(icon))
                icon = launch;

            return Result.FromGame(new Game(
                Id: id,
                Name: _fileSystem.Path.GetFileNameWithoutExtension(game.ShortcutName ?? ""),
                Path: game.ProductInstallFullPath ?? "",
                Launch: launch,
                LaunchArgs: launchArgs,
                Icon: icon,
                Uninstall: uninstall,
                UninstallArgs: uninstallArgs,
                Metadata: new(StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception e)
        {
            return Result.FromError<Game>($"Unable to deserialize file {settingsFile.FullName}:\n{e}");
        }
    }
}
