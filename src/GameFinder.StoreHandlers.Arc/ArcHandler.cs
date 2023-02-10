using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using JetBrains.Annotations;

namespace GameFinder.StoreHandlers.Arc;

/// <summary>
/// Represents a game installed with Arc.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Path"></param>
[PublicAPI]
public record ArcGame(long Id, string Name, string Path);

/// <summary>
/// Handler for finding games installed with Arc.
/// </summary>
[PublicAPI]
public class ArcHandler : AHandler<ArcGame, long>
{
    internal const string ArcRegKey = @"Software\Perfect World Entertainment\Core";

    private readonly IRegistry _registry;

    /// <summary>
    /// Default constructor. This uses the <see cref="WindowsRegistry"/> implementation
    /// of <see cref="IRegistry"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public ArcHandler() : this(new WindowsRegistry()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/>.
    /// </summary>
    /// <param name="registry"></param>
    public ArcHandler(IRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<ArcGame>> FindAllGames()
    {
        var games = new List<Result<ArcGame>>();
        foreach (var gameEx in FindAllGamesEx(installedOnly: true).OnlyGames())
        {
            _ = long.TryParse(gameEx.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id);
            games.Add(Result.FromGame(new ArcGame(id, gameEx.Name, gameEx.Path)));
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
        var localMachine =_registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

        using var arcKey = localMachine.OpenSubKey(ArcRegKey);
        if (arcKey is null)
        {
            yield return Result.FromError<GameEx>($"Unable to open HKEY_LOCAL_MACHINE\\{ArcRegKey}");
            yield break;
        }

        var subKeyNames = arcKey.GetSubKeyNames().ToArray();
        if (subKeyNames.Length == 0)
        {
            yield return Result.FromError<GameEx>($"Registry key {arcKey.GetName()} has no sub-keys");
            yield break;
        }

        foreach (var subKeyName in subKeyNames)
        {
            yield return ParseSubKeyEx(arcKey, subKeyName);
        }
    }

    /// <inheritdoc/>
    public override IDictionary<long, ArcGame> FindAllGamesById(out string[] errors)
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

    private static Result<ArcGame> ParseSubKey(IRegistryKey arcKey, string subKeyName)
    {
        var gameEx = ParseSubKeyEx(arcKey, subKeyName);
        if (gameEx.Game is null)
        {
            if (gameEx.Error is not null)
            {
                return Result.FromError<ArcGame>(gameEx.Error);
            }
            return Result.FromException<ArcGame>(new());
        }
        _ = long.TryParse(gameEx.Game.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id);
        return Result.FromGame(new ArcGame(id, gameEx.Game.Name, gameEx.Game.Path));
    }

    private static Result<GameEx> ParseSubKeyEx(IRegistryKey arcKey, string subKeyName)
    {
        try
        {
            using var subKey = arcKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return Result.FromError<GameEx>($"Unable to open {arcKey}\\{subKeyName}");
            }

            int i = subKeyName.IndexOf("en", StringComparison.OrdinalIgnoreCase);
            if (i < 2)
            {
                return Result.FromError<GameEx>($"The subkey name of {subKey.GetName()} does not end in \"en\"");
            }

            var sId = subKeyName[..i];
            if (!long.TryParse(sId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                return Result.FromError<GameEx>($"The value \"gameID\" of {subKey.GetName()} is not a number: \"{sId}\"");
            }

            if (!subKey.TryGetString("INSTALL_PATH", out var path))
            {
                return Result.FromError<GameEx>($"{subKey.GetName()} doesn't have a string value \"INSTALL_PATH\"");
            }

            string name = "";
            if (path.Contains("_en", StringComparison.OrdinalIgnoreCase))
            {
                name = Path.GetFileName(path[..path.IndexOf("_en", StringComparison.OrdinalIgnoreCase)]);
            }
            else
            {
                name = Path.GetFileName(path);
            }
            if (string.IsNullOrEmpty(name))
            {
                return Result.FromError<GameEx>($"Name could not be generated from path: \"{path}\"");
            }

            if (!subKey.TryGetString("LAUNCHER_PATH", out var launch)) launch = "";
            if (!subKey.TryGetString("CLIENT_PATH", out var icon)) icon = launch;

            var gameEx = new GameEx(sId, name, path, launch, icon, "", new(StringComparer.OrdinalIgnoreCase));
            return Result.FromGame(gameEx);
        }
        catch (Exception e)
        {
            return Result.FromException<GameEx>($"Exception while parsing registry key {arcKey}\\{subKeyName}", e);
        }
    }
}
