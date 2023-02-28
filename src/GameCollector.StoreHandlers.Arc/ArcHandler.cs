using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Arc;

/// <summary>
/// Handler for finding games installed with Arc.
/// </summary>
[PublicAPI]
public class ArcHandler : AHandler<Game, string>
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
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var localMachine = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

        using var arcKey = localMachine.OpenSubKey(ArcRegKey);
        if (arcKey is null)
        {
            yield return Result.FromError<Game>($"Unable to open HKEY_LOCAL_MACHINE\\{ArcRegKey}");
            yield break;
        }

        var subKeyNames = arcKey.GetSubKeyNames().ToArray();
        if (subKeyNames.Length == 0)
        {
            yield return Result.FromError<Game>($"Registry key {arcKey.GetName()} has no sub-keys");
            yield break;
        }

        foreach (var subKeyName in subKeyNames)
        {
            // TODO: Multiple languages often exist for each game, but just picking English isn't the best solution
            if (subKeyName.EndsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                yield return ParseSubKey(arcKey, subKeyName);
            }
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game);
    }

    private static Result<Game> ParseSubKey(IRegistryKey arcKey, string subKeyName)
    {
        try
        {
            using var subKey = arcKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                return Result.FromError<Game>($"Unable to open {arcKey}\\{subKeyName}");
            }

            int i = subKeyName.IndexOf("en", StringComparison.OrdinalIgnoreCase);
            if (i < 2)
            {
                return Result.FromError<Game>($"The subkey name of {subKey.GetName()} does not end in \"en\"");
            }

            var sId = subKeyName[..i];
            if (!long.TryParse(sId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                return Result.FromError<Game>($"The subkey name of {subKey.GetName()} does not start with a number: \"{sId}\"");
            }

            if (!subKey.TryGetString("INSTALL_PATH", out var path))
            {
                return Result.FromError<Game>($"{subKey.GetName()} doesn't have a string value \"INSTALL_PATH\"");
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
                return Result.FromError<Game>($"Name could not be generated from path: \"{path}\"");
            }

            if (!subKey.TryGetString("LAUNCHER_PATH", out var launch)) launch = "";
            if (!subKey.TryGetString("CLIENT_PATH", out var icon)) icon = launch;

            return Result.FromGame(new Game(
                Id: sId,
                Name: name,
                Path: path,
                Launch: launch,
                Icon: icon,
                Metadata: new(StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception e)
        {
            return Result.FromException<Game>($"Exception while parsing registry key {arcKey}\\{subKeyName}", e);
        }
    }
}
