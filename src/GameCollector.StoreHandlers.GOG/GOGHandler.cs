using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Versioning;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.GOG;

/// <summary>
/// Represents a game installed with GOG Galaxy.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Path"></param>
[PublicAPI]
public record GOGGame(long Id, string Name, AbsolutePath Path);

/// <summary>
/// Handler for finding games installed with GOG Galaxy.
/// </summary>
[PublicAPI]
public class GOGHandler : AHandler<Game, string>
{
    internal const string GOGRegKey = @"Software\GOG.com\Games";

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="fileSystem"></param>
    public GOGHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        try
        {
            var localMachine = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            using var gogKey = localMachine.OpenSubKey(GOGRegKey);
            if (gogKey is null)
            {
                return new[]
                {
                    Result.FromError<Game>($"Unable to open HKEY_LOCAL_MACHINE\\{GOGRegKey}"),
                };
            }

            var subKeyNames = gogKey.GetSubKeyNames().ToArray();
            if (subKeyNames.Length == 0)
            {
                return new[]
                {
                    Result.FromError<Game>($"Registry key {gogKey.GetName()} has no sub-keys"),
                };
            }

            return subKeyNames
                .Select(subKeyName => ParseSubKey(gogKey, subKeyName))
                .ToArray();
        }
        catch (Exception e)
        {
            return new[] { Result.FromException<Game>("Exception looking for GOG games", e) };
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game);
    }

    private Result<Game> ParseSubKey(IRegistryKey gogKey, string subKeyName)
    {
        try
        {
            using var subKey = gogKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return Result.FromError<Game>($"Unable to open {gogKey}\\{subKeyName}");
            }

            if (!subKey.TryGetString("gameID", out var sId))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"gameID\"");
            }

            if (!long.TryParse(sId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                return Result.FromError<Game>($"The value \"gameID\" of {subKey.GetName()} is not a number: \"{sId}\"");
            }

            if (!subKey.TryGetString("gameName", out var name))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"gameName\"");
            }

            if (!subKey.TryGetString("path", out var path))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"path\"");
            }

            path = path.Replace("\\\\", "\\", StringComparison.Ordinal);
            subKey.TryGetString("launchCommand", out var launch);
            launch ??= "";
            subKey.TryGetString("exe", out var icon);
            icon ??= "";
            subKey.TryGetString("uninstallCommand", out var uninst);
            uninst ??= "";

            return Result.FromGame(new Game(
                Id: sId,
                Name: name,
                Path: _fileSystem.FromFullPath(path),
                Launch: launch,
                Icon: icon,
                Uninstall: uninst,
                Metadata: new(StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception e)
        {
            return Result.FromException<Game>($"Exception while parsing registry key {gogKey}\\{subKeyName}", e);
        }
    }
}
