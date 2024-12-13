using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FluentResults;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameCollector.StoreHandlers.Steam.Models.ValueTypes;
using GameCollector.StoreHandlers.Steam.Services;
using JetBrains.Annotations;
using NexusMods.Paths;
using OneOf;
using ValveKeyValue;

namespace GameCollector.StoreHandlers.Steam;

/// <summary>
/// Handler for finding games installed with Steam.
/// </summary>
[PublicAPI]
public partial class SteamHandler : AHandler<SteamGame, AppId>
{
    internal const string RegKey = @"Software\Valve\Steam";
    internal const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private readonly IRegistry? _registry;
    private readonly IFileSystem _fileSystem;

    private static readonly KVSerializerOptions KvSerializerOptions =
        new()
        {
            HasEscapeSequences = true,
            EnableValveNullByteBugBehavior = true,
        };

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileSystem">
    /// The implementation of <see cref="IFileSystem"/> to use. For a shared instance use
    /// <see cref="FileSystem.Shared"/>. For tests either use <see cref="InMemoryFileSystem"/>,
    /// a custom implementation or just a mock of the interface.
    /// </param>
    /// <param name="registry">
    /// The implementation of <see cref="IRegistry"/> to use. For a shared instance
    /// use <see cref="WindowsRegistry.Shared"/> on Windows. On Linux use <c>null</c>.
    /// For tests either use <see cref="InMemoryRegistry"/>, a custom implementation or just a mock
    /// of the interface.
    /// </param>
    public SteamHandler(IFileSystem fileSystem, IRegistry? registry = null)
    {
        _fileSystem = fileSystem;
        _registry = registry;
    }

    /// <inheritdoc/>
    public override Func<SteamGame, AppId> IdSelector => game => game.AppId;

    /// <inheritdoc/>
    public override IEqualityComparer<AppId>? IdEqualityComparer => null;

    /// <inheritdoc/>
    public override AbsolutePath FindClient()
    {
        var steamPathResult = SteamLocationFinder.FindSteam(_fileSystem, _registry);
        if (!steamPathResult.IsFailed)
        {
            return steamPathResult.Value;
        }

        return new();
    }

    /// <inheritdoc/>
    public override IEnumerable<OneOf<SteamGame, ErrorMessage>> FindAllGames(Settings? settings = null)
    {
        return FindAllGames(settings, 0);
    }

    /// <summary>
    /// Finds all Steam games
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="userId"></param>
    public IEnumerable<OneOf<SteamGame, ErrorMessage>> FindAllGames(Settings? settings = null, ulong userId = 0)
    {
        List<OneOf<SteamGame, ErrorMessage>> allGames = new();
        Dictionary<AppId, OneOf<SteamGame, ErrorMessage>> installedGames = new();

        var steamPathResult = SteamLocationFinder.FindSteam(_fileSystem, _registry);
        if (steamPathResult.IsFailed)
        {
            allGames.Add(ConvertResultToErrorMessage(steamPathResult));
            return allGames;
        }

        var steamPath = steamPathResult.Value;
        var libraryFoldersFilePath = SteamLocationFinder.GetLibraryFoldersFilePath(steamPath);

        var libraryFoldersResult = LibraryFoldersManifestParser.ParseManifestFile(libraryFoldersFilePath);
        if (libraryFoldersResult.IsFailed)
        {
            allGames.Add(ConvertResultToErrorMessage(libraryFoldersResult));
            return allGames;
        }

        var libraryFolders = libraryFoldersResult.Value;
        if (libraryFolders.Count == 0) return allGames;

        foreach (var libraryFolder in libraryFolders)
        {
            var libraryFolderPath = libraryFolder.Path;
            if (!_fileSystem.DirectoryExists(libraryFolderPath) ||
                !_fileSystem.DirectoryExists(libraryFolderPath.Combine(Models.LibraryFolder.SteamAppsDirectoryName)))
            {
                allGames.Add(new ErrorMessage($"Steam Library at {libraryFolderPath} doesn't exist!"));
                continue;
            }

            foreach (var acfFilePath in libraryFolder.EnumerateAppManifestFilePaths())
            {
                // skip Steamworks Common Redistributables
                if (acfFilePath.FileName.Equals("228980", StringComparison.Ordinal))
                    continue;

                var appManifestResult = AppManifestParser.ParseManifestFile(acfFilePath);
                if (appManifestResult.IsFailed)
                {
                    allGames.Add(ConvertResultToErrorMessage(appManifestResult));
                    continue;
                }

                var registryEntryResult = RegistryEntryParser.ParseRegistryEntry(appManifestResult.Value.AppId, _fileSystem, _registry);
                /*
                if (registryEntryResult.IsFailed)
                {
                    yield return ConvertResultToErrorMessage(registryEntryResult);
                }
                */

                var steamGame = new SteamGame(
                    steamPath,
                    appManifestResult.Value ?? default,
                    registryEntryResult.Value ?? default,
                    libraryFolder ?? default,
                    OwnedGame: default,
                    IsInstalled: true
                );

                installedGames.TryAdd(steamGame.AppId, steamGame);
            }
        }

        if (settings?.InstalledOnly == true || _apiKey is null)
            return installedGames.Values;

        return FindOwnedGamesFromAPI(installedGames, userId);
    }

    private static ErrorMessage ConvertResultToErrorMessage<T>(Result<T> result)
    {
        // TODO: for compatability, remove this mapping once FindAllGames uses FluentResults
        return new ErrorMessage(result.Errors.Select(x => x.Message).Aggregate((a, b) => $"{a}\n{b}"));
    }
}
