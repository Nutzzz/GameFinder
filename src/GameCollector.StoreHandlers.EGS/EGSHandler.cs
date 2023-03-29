using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.EGS;

/// <summary>
/// Represents a game installed with the Epic Games Store.
/// </summary>
/// <param name="CatalogItemId"></param>
/// <param name="DisplayName"></param>
/// <param name="InstallLocation"></param>
[PublicAPI]
public record EGSGame(string CatalogItemId, string DisplayName, AbsolutePath InstallLocation);

record ManifestFile(string CatalogItemId, string DisplayName, string InstallLocation);

/// <summary>
/// Handler for finding games installed with the Epic Games Store.
/// </summary>
[PublicAPI]
public class EGSHandler : AHandler<Game, string>
{
    internal const string RegKey = @"Software\Epic Games\EOS";
    internal const string ModSdkMetadataDir = "ModSdkMetadataDir";

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new()
        {
            AllowTrailingCommas = true,
        };

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="fileSystem"></param>
    public EGSHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var manifestDir = GetManifestDir();
        if (!_fileSystem.DirectoryExists(manifestDir))
        {
            yield return Result.FromError<Game>($"The manifest directory {manifestDir.GetFullPath()} does not exist!");
            yield break;
        }

        var itemFiles = _fileSystem
            .EnumerateFiles(manifestDir, "*.item")
            .ToArray();

        if (itemFiles.Length == 0)
        {
            yield return Result.FromError<Game>($"The manifest directory {manifestDir.GetFullPath()} does not contain any .item files");
            yield break;
        }

        foreach (var itemFile in itemFiles)
        {
            yield return DeserializeGame(itemFile);
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }

    private Result<Game> DeserializeGame(AbsolutePath itemFile)
    {
        using var stream = _fileSystem.ReadFile(itemFile);

        try
        {
            var game = JsonSerializer.Deserialize<ManifestFile>(stream, _jsonSerializerOptions);

            if (game is null)
            {
                return Result.FromError<Game>($"Unable to deserialize file {itemFile.GetFullPath()}");
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (game.CatalogItemId is null)
            {
                return Result.FromError<Game>($"Manifest {itemFile.GetFullPath()} does not have a value \"CatalogItemId\"");
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (game.DisplayName is null)
            {
                return Result.FromError<Game>($"Manifest {itemFile.GetFullPath()} does not have a value \"DisplayName\"");
            }

            if (string.IsNullOrEmpty(game.InstallLocation))
            {
                return Result.FromError<Game>($"Manifest {itemFile.GetFullPath()} does not have a value \"InstallLocation\"");
            }

            string launch = "";
            if (game.LaunchExecutable is not null) // DLCs won't have a LaunchExecutable
            {
                launch = _fileSystem.Path.Combine(game.InstallLocation, game.LaunchExecutable);
            }

            return Result.FromGame(new Game(
                Id: game.CatalogItemId,
                Name: game.DisplayName,
                Path: _fileSystem.FromFullPath(game.InstallLocation),
                Launch: launch,
                Icon: launch,
                Metadata: new(StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception e)
        {
            return Result.FromError<Game>($"Unable to deserialize file {itemFile.GetFullPath()}:\n{e}");
        }
    }

    private AbsolutePath GetManifestDir()
    {
        return TryGetManifestDirFromRegistry(out var manifestDir)
            ? manifestDir
            : GetDefaultManifestsPath(_fileSystem);
    }

    internal static AbsolutePath GetDefaultManifestsPath(IFileSystem fileSystem)
    {
        return fileSystem
            .GetKnownPath(KnownPath.CommonApplicationDataDirectory)
            .CombineUnchecked("Epic/EpicGamesLauncher/Data/Manifests");
    }

    private bool TryGetManifestDirFromRegistry(out AbsolutePath manifestDir)
    {
        manifestDir = default;

        try
        {
            var currentUser = _registry.OpenBaseKey(RegistryHive.CurrentUser);
            using var regKey = currentUser.OpenSubKey(RegKey);

            if (regKey is null || !regKey.TryGetString("ModSdkMetadataDir",
                    out var registryMetadataDir)) return false;

            manifestDir = _fileSystem.FromFullPath(registryMetadataDir);
            return true;

        }
        catch (Exception)
        {
            return false;
        }
    }
}
