using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using JetBrains.Annotations;
using NexusMods.Paths;
using OneOf;

namespace GameCollector.StoreHandlers.Plarium;

/// <summary>
/// Handler for finding games installed with Plarium Play.
/// </summary>
[PublicAPI]
public class PlariumHandler : AHandler<PlariumGame, string>
{
    internal const string PlariumRegKey = @"Software\PlariumPlayInstaller";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.Strict,
        TypeInfoResolver = SourceGenerationContext.Default,
    };

    private readonly IRegistry? _registry;
    private readonly IFileSystem _fileSystem;

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
    public PlariumHandler(IFileSystem fileSystem, IRegistry? registry)
    {
        _fileSystem = fileSystem;
        _registry = registry;
    }

    /// <inheritdoc/>
    public override IEqualityComparer<string>? IdEqualityComparer => null;

    /// <inheritdoc/>
    public override Func<PlariumGame, string> IdSelector => game => game.GameId;

    /// <inheritdoc/>
    public override AbsolutePath FindClient()
    {
        if (_registry is not null)
        {
            var currentUser = _registry.OpenBaseKey(RegistryHive.CurrentUser);

            using var regKey = currentUser.OpenSubKey(PlariumRegKey);
            if (regKey is not null)
            {
                if (regKey.TryGetString("InstallFolder", out var folder) && Path.IsPathRooted(folder))
                {
                    var play = _fileSystem.FromFullPath(SanitizeInputPath(folder)).CombineUnchecked("PlariumPlay.exe");
                    if (play.FileExists) return play;  // NB: The registry isn't always right
                }
            }
        }

        return GetPlariumPlayPath().CombineUnchecked("PlariumPlay.exe");
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code",
    Justification = $"{nameof(JsonSerializerOptions)} uses {nameof(SourceGenerationContext)} for type information.")]
    public override IEnumerable<OneOf<PlariumGame, ErrorMessage>> FindAllGames(bool installedOnly = false, bool baseOnly = false)
    {
        var jsonFile = GetPlariumPlayPath().CombineUnchecked("gamestorage.gsfn");
        using var stream = jsonFile.Read();
        var gameStorage = JsonSerializer.Deserialize<GameStorage>(stream, JsonSerializerOptions);
        if (gameStorage is null)
        {
            yield return new ErrorMessage($"Unable to deserialize file {jsonFile.GetFullPath()}");
            yield break;
        }
        ulong id = 0;
        var gameId = "";
        var gameName = "";
        var name = "";
        var strPath = "";
        AbsolutePath path = new();
        AbsolutePath gamePath = new();
        AbsolutePath exe = new();
        var args = "";
        var company = "";
        foreach (var game in gameStorage.InstalledGames)
        {
            id = game.Value.Id ?? 0;
            var games = game.Value.InsalledGames; // [sic]
            if (games is not null && games.Count > 0)
            {
                gameName = games.Keys.ToArray()[0];
                gameId = games.Values.ToArray()[0];
            }
            strPath = SanitizeInputPath(game.Value.InstallationPath ?? "");
            if (string.IsNullOrEmpty(strPath) || strPath.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                exe = GetPlariumPlayPath().CombineUnchecked("PlariumPlay.exe");
                args = $"-gameid={id} -tray-start";
            } else {
                path = Path.IsPathRooted(strPath) ? _fileSystem.FromFullPath(strPath) : default;
                if (!string.IsNullOrEmpty(gameName) && !string.IsNullOrEmpty(gameId))
                {
                    gamePath = path.CombineUnchecked(gameName).CombineUnchecked(gameId);
                    if (gamePath.DirectoryExists())
                    {
                        exe = Utils.FindExe(gamePath, _fileSystem, gameName);
                        var settingsFile = gamePath.CombineUnchecked("settings.json");
                        var appInfoFile = gamePath.CombineUnchecked($"{gameName}_Data").CombineUnchecked("app.info");
                        if (settingsFile.FileExists)
                        {
                            using var settingsStream = settingsFile.Read();
                            var settings = JsonSerializer.Deserialize<Settings>(settingsStream);
                            if (settings is not null)
                            {
                                company = settings.CompanyName;
                                name = settings.ProductName;
                            }
                        }
                        if (string.IsNullOrEmpty(name) && appInfoFile.FileExists)
                        {
                            using var appInfoStream = appInfoFile.Read();
                            using var reader = new StreamReader(appInfoStream);
                            var line = reader.ReadLine();
                            if (line is not null)
                            {
                                company = line;
                                reader.ReadLine();
                                if (line is not null)
                                    name = line;
                            }
                        }
                    }
                }
            }
            yield return new PlariumGame(
                ProductId: PlariumGameId.From(id),
                ProductName: string.IsNullOrEmpty(name) ? gameName : name,
                InstallationPath: gamePath == default ? path : gamePath,
                Launch: exe,
                LaunchArgs: args,
                GameId: gameId,
                GameName: gameName,
                CompanyName: company);
        }
    }

    public AbsolutePath GetPlariumPlayPath()
    {
        // The path changed slightly between versions, and the registry isn't always right, so we'll look in both places
        var jsonFile = _fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
            .CombineUnchecked("PlariumPlay");
        if (!jsonFile.DirectoryExists())
            jsonFile = _fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
            .CombineUnchecked("Plarium")
            .CombineUnchecked("PlariumPlay");
        return jsonFile;
    }
}
