using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using JetBrains.Annotations;

namespace GameFinder.StoreHandlers.GOG;

/// <summary>
/// Represents a game installed with GOG Galaxy.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Path"></param>
[PublicAPI]
public record GOGGame(long Id, string Name, string Path);

/// <summary>
/// Handler for finding games installed with GOG Galaxy.
/// </summary>
[PublicAPI]
public class GOGHandler : AHandler<GOGGame, long>
{
    internal const string GOGRegKey = @"Software\GOG.com\Games";

    private readonly IRegistry _registry;

    /// <summary>
    /// Default constructor. This uses the <see cref="WindowsRegistry"/> implementation
    /// of <see cref="IRegistry"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public GOGHandler() : this(new WindowsRegistry()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/>.
    /// </summary>
    /// <param name="registry"></param>
    public GOGHandler(IRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<GOGGame>> FindAllGames()
    {
        var games = new List<Result<GOGGame>>();
        foreach (var gameEx in FindAllGamesEx(installedOnly: true).OnlyGames())
        {
            _ = long.TryParse(gameEx.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id);
            games.Add(Result.FromGame(new GOGGame(id, gameEx.Name, gameEx.Path)));
        }
        return games;
    }

    /// <summary>
    /// Finds all games installed with this store. The return type <see cref="Result{TGame}"/>
    /// will always be a non-null game or a non-null error message.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Result<GameEx>> FindAllGamesEx(bool installedOnly = true)
    {
        try
        {
            var localMachine =_registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            using var gogKey = localMachine.OpenSubKey(GOGRegKey);
            if (gogKey is null)
            {
                return new[]
                {
                    Result.FromError<GameEx>($"Unable to open HKEY_LOCAL_MACHINE\\{GOGRegKey}"),
                };
            }

            var subKeyNames = gogKey.GetSubKeyNames().ToArray();
            if (subKeyNames.Length == 0)
            {
                return new[]
                {
                    Result.FromError<GameEx>($"Registry key {gogKey.GetName()} has no sub-keys"),
                };
            }

            return subKeyNames
                .Select(subKeyName => ParseSubKey(gogKey, subKeyName))
                .ToArray();
        }
        catch (Exception e)
        {
            return new[] { Result.FromException<GameEx>("Exception looking for GOG games", e) };
        }
    }

    /// <inheritdoc/>
    public override IDictionary<long, GOGGame> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game);
    }

    /// <summary>
    /// Calls <see cref="FindAllGamesEx"/> and converts the result into a dictionary where
    /// the key is the id of the game.
    /// </summary>
    /// <param name="errors"></param>
    /// <returns></returns>
    public IDictionary<string, GameEx> FindAllGamesByIdEx(out string[] errors)
    {
        var (gamesEx, allErrors) = FindAllGamesEx().SplitResults();
        errors = allErrors;

        return gamesEx.CustomToDictionary(gameEx => gameEx.Id, gameEx => gameEx);
    }

    private static Result<GameEx> ParseSubKey(IRegistryKey gogKey, string subKeyName)
    {
        try
        {
            using var subKey = gogKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return Result.FromError<GameEx>($"Unable to open {gogKey}\\{subKeyName}");
            }

            if (!subKey.TryGetString("gameID", out var sId))
            {
                return Result.FromError<GameEx>($"{subKey.GetName()} doesn't have a string value \"gameID\"");
            }

            if (!long.TryParse(sId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                return Result.FromError<GameEx>($"The value \"gameID\" of {subKey.GetName()} is not a number: \"{sId}\"");
            }

            if (!subKey.TryGetString("gameName", out var name))
            {
                return Result.FromError<GameEx>($"{subKey.GetName()} doesn't have a string value \"gameName\"");
            }

            if (!subKey.TryGetString("path", out var path))
            {
                return Result.FromError<GameEx>($"{subKey.GetName()} doesn't have a string value \"path\"");
            }

            subKey.TryGetString("launchCommand", out var launch);
            launch ??= "";
            subKey.TryGetString("exe", out var icon);
            icon ??= "";
            subKey.TryGetString("uninstallCommand", out var uninst);
            uninst ??= "";

            var game = new GameEx(sId, name, path, launch, icon, uninst);
            return Result.FromGame(game);
        }
        catch (Exception e)
        {
            return Result.FromException<GameEx>($"Exception while parsing registry key {gogKey}\\{subKeyName}", e);
        }
    }
}
