using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using JetBrains.Annotations;
using NexusMods.Paths;
using OneOf;

namespace GameFinder.StoreHandlers.GOG;

/// <summary>
/// Handler for finding games installed with GOG Galaxy.
/// </summary>
[PublicAPI]
public partial class GOGHandler : AHandler<GOGGame, GOGGameId>
{
    internal const string GOGRegKey = @"Software\GOG.com\Games";

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="registry">
    /// The implementation of <see cref="IRegistry"/> to use. For a shared instance
    /// use <see cref="WindowsRegistry.Shared"/> on Windows. For tests either use
    /// <see cref="InMemoryRegistry"/>, a custom implementation or just a mock
    /// of the interface. See the README for more information if you want to use
    /// Wine.
    /// </param>
    /// <param name="fileSystem">
    /// The implementation of <see cref="IFileSystem"/> to use. For a shared instance use
    /// <see cref="FileSystem.Shared"/>. For tests either use <see cref="InMemoryFileSystem"/>,
    /// a custom implementation or just a mock of the interface. See the README for more information
    /// if you want to use Wine.
    /// </param>
    public GOGHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override Func<GOGGame, GOGGameId> IdSelector => game => game.Id;

    /// <inheritdoc/>
    public override IEqualityComparer<GOGGameId>? IdEqualityComparer => null;

    /// <inheritdoc/>
    public override AbsolutePath FindClient()
    {
        if (_registry is not null)
        {
            var localMachine = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            using var regKey = localMachine.OpenSubKey(@"SOFTWARE\GOG.com\GalaxyClient");
            if (regKey is null) return default;

            if (regKey.TryGetString("clientExecutable", out var clientExe))
            {
                using var regKey2 = regKey.OpenSubKey("paths");
                if (regKey2 is null) return default;

                if (regKey2.TryGetString("client", out var clientPath) && Path.IsPathRooted(clientPath))
                    return _fileSystem.FromFullPath(SanitizeInputPath(clientPath)).CombineUnchecked(clientExe);
            }
        }

        return default;
    }

    /// <inheritdoc/>
    public override IEnumerable<OneOf<GOGGame, ErrorMessage>> FindAllGames(bool installedOnly = false, bool baseOnly = false)
    {
        try
        {
            var localMachine = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            using var gogKey = localMachine.OpenSubKey(GOGRegKey);
            if (gogKey is null)
            {
                return new OneOf<GOGGame, ErrorMessage>[]
                {
                    new ErrorMessage($"Unable to open HKEY_LOCAL_MACHINE\\{GOGRegKey}"),
                };
            }

            var subKeyNames = gogKey.GetSubKeyNames().ToArray();
            if (subKeyNames.Length == 0)
            {
                return new OneOf<GOGGame, ErrorMessage>[]
                {
                    new ErrorMessage($"Registry key {gogKey.GetName()} has no sub-keys"),
                };
            }

            var games = FindGamesFromDatabase(installedOnly).ToList();
            games.AddRange(subKeyNames
                .Select(subKeyName => ParseSubKey(gogKey, subKeyName))
                .ToArray());

            return games;
        }
        catch (Exception e)
        {
            return new OneOf<GOGGame, ErrorMessage>[]
            {
                new ErrorMessage(e, "Exception looking for GOG games"),
            };
        }
    }

    private OneOf<GOGGame, ErrorMessage> ParseSubKey(IRegistryKey gogKey, string subKeyName)
    {
        try
        {
            using var subKey = gogKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return new ErrorMessage($"Unable to open {gogKey}\\{subKeyName}");
            }

            if (!subKey.TryGetString("gameID", out var sId))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"gameID\"");
            }

            if (!long.TryParse(sId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                return new ErrorMessage($"The value \"gameID\" of {subKey.GetName()} is not a number: \"{sId}\"");
            }

            if (!subKey.TryGetString("gameName", out var name))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"gameName\"");
            }

            if (!subKey.TryGetString("path", out var path))
            {
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"path\"");
            }

            //subKey.TryGetString("launchCommand", out var launch);
            subKey.TryGetString("exe", out var exe);
            subKey.TryGetString("launchParam", out var launchParam);
            subKey.TryGetString("uninstallCommand", out var uninst);

            var game = new GOGGame(
                Id: GOGGameId.From(id),
                Name: name,
                Path: Path.IsPathRooted(path) ? _fileSystem.FromUnsanitizedFullPath(SanitizeInputPath(path)) : new(),
                Exe: Path.IsPathRooted(exe) ? _fileSystem.FromUnsanitizedFullPath(SanitizeInputPath(exe)) : new(),
                LaunchParam: launchParam ?? "",
                UninstallCommand: Path.IsPathRooted(uninst) ? _fileSystem.FromUnsanitizedFullPath(SanitizeInputPath(uninst)) : new()
            );

            return game;
        }
        catch (Exception e)
        {
            return new ErrorMessage(e, $"Exception while parsing registry key {gogKey}\\{subKeyName}");
        }
    }
}
