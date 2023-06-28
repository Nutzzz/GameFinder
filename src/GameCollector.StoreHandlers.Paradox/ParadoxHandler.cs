using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using JetBrains.Annotations;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using OneOf;

namespace GameCollector.StoreHandlers.Paradox;

/// <summary>
/// Handler for finding games installed with Paradox Launcher.
/// </summary>
[PublicAPI]
public class ParadoxHandler : AHandler<ParadoxGame, ParadoxGameId>
{
    internal const string ParadoxRegKey = @"Software\Paradox Interactive\Paradox Launcher v2";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.Strict,
        TypeInfoResolver = SourceGenerationContext.Default,
    };

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="registry">
    /// The implementation of <see cref="IRegistry"/> to use. For a shared instance
    /// use <see cref="WindowsRegistry.Shared"/> on Windows. On Linux use <c>null</c>.
    /// For tests either use <see cref="InMemoryRegistry"/>, a custom implementation or just a mock
    /// of the interface.
    /// </param>
    /// <param name="fileSystem">
    /// The implementation of <see cref="IFileSystem"/> to use. For a shared instance use
    /// <see cref="FileSystem.Shared"/>. For tests either use <see cref="InMemoryFileSystem"/>,
    /// a custom implementation or just a mock of the interface.
    /// </param>
    public ParadoxHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEqualityComparer<ParadoxGameId>? IdEqualityComparer => ParadoxGameIdComparer.Default;

    /// <inheritdoc/>
    public override Func<ParadoxGame, ParadoxGameId> IdSelector => game => game.Id;

    /// <inheritdoc/>
    public override AbsolutePath FindClient()
    {
        if (_registry is not null)
        {
            var currentUser = _registry.OpenBaseKey(RegistryHive.CurrentUser);

            using var regKey = currentUser.OpenSubKey(ParadoxRegKey);
            if (regKey is not null)
            {
                if (regKey.TryGetString("LauncherInstallation", out var launcher) && Path.IsPathRooted(launcher))
                    return _fileSystem.FromFullPath(SanitizeInputPath(launcher)).CombineUnchecked("bootstrapper-v2.exe");
            }
        }

        return default;
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code",
    Justification = $"{nameof(JsonSerializerOptions)} uses {nameof(SourceGenerationContext)} for type information.")]
    public override IEnumerable<OneOf<ParadoxGame, ErrorMessage>> FindAllGames(bool installedOnly = false, bool baseOnly = false)
    {
        var pdxPath = GetParadoxV2Path();
        var userFile = pdxPath.CombineUnchecked("userSettings.json");
        using var userStream = userFile.Read();
        var userSettings = JsonSerializer.Deserialize<UserSettings>(userStream, JsonSerializerOptions);
        if (userSettings is null)
        {
            yield return new ErrorMessage($"Unable to deserialize file {userFile.GetFullPath()}");
            yield break;
        }

        Dictionary<string, string?> instPaths = new();
        foreach (var libPath in userSettings.GameLibraryPaths)
        {
            if (libPath.ValueKind == JsonValueKind.String)
                instPaths["default"] = libPath.ToString();
            else if (libPath.ValueKind == JsonValueKind.Object)
            {
                if (libPath.TryGetProperty("gameId", out var id) &&
                    libPath.TryGetProperty("installationPath", out var path))

                    instPaths[id.ToString()] = path.ToString();
            }
        }
        Dictionary<string, ulong?> runDates = new();
        var gamesLaunched = userSettings.GamesLaunched;
        foreach (var obj in gamesLaunched.EnumerateObject())
        {
            _ = ulong.TryParse(obj.Value.ToString(), out var date);
            runDates.Add(obj.Name, date > 0 ? date : null);
        }

        var metaFile = pdxPath.CombineUnchecked("game-metadata").CombineUnchecked("game-metadata");
        using var metaStream = metaFile.Read();
        var metadata = JsonSerializer.Deserialize<GameMetadata>(metaStream, JsonSerializerOptions);
        if (metadata is not null && metadata.Data is not null && metadata.Data.Games is not null)
        {
            foreach (var game in metadata.Data.Games)
            {
                var id = game.Id;
                var name = game.Name ?? (game.Id is null ? "" : game.Id.Replace('_', ' '));
                var strExe = SanitizeInputPath(game.ExePath ?? "");
                var args = game.ExeArgs;
                var strIcon = "";
                var strTaskIcon = "";
                var strBg = "";
                var strLogo = "";
                var strPath = "";
                ulong? lastLaunch = 0;
                if (game.ThemeSettings is not null)
                {
                    strIcon = SanitizeInputPath(game.ThemeSettings.AppIcon ?? "");
                    strTaskIcon = SanitizeInputPath(game.ThemeSettings.AppTaskbarIcon ?? "");
                    strBg = SanitizeInputPath(game.ThemeSettings.Background ?? "");
                    strLogo = SanitizeInputPath(game.ThemeSettings.Logo ?? "");
                }

                if (id is not null && instPaths.ContainsKey(id))
                {
                    strPath = SanitizeInputPath(instPaths[id] ?? "");
                    if (runDates.ContainsKey(id))
                        lastLaunch = runDates[id];
                }
                else if (instPaths.ContainsKey("default"))
                    strPath = SanitizeInputPath(instPaths["default"] ?? "");

                AbsolutePath path = new();
                if (!string.IsNullOrEmpty(strPath))
                {
                    if (Path.IsPathRooted(strPath))
                        path = _fileSystem.FromFullPath(strPath);
                    else
                        path = GetParadoxV2Path().CombineUnchecked(strPath.ToRelativePath());
                }
                AbsolutePath exe = new();
                if (Path.IsPathRooted(strExe))
                    exe = _fileSystem.FromFullPath(strExe);

                AbsolutePath dataPath = new();
                if (path != default && path.DirectoryExists() && (exe == default || !exe.FileExists))
                {
                    var settingsFile = path.CombineUnchecked("launcher-settings.json");
                    using var settingsStream = settingsFile.Read();
                    var settings = JsonSerializer.Deserialize<LauncherSettings>(settingsStream, JsonSerializerOptions);
                    if (settings is not null && settings.ExePath is not null)
                    {
                        exe = path.CombineUnchecked(SanitizeInputPath(settings.ExePath));
                        if (settings.GameDataPath is not null)
                        {
                            var strDataPath = SanitizeInputPath(settings.GameDataPath.Replace("%USER_DOCUMENTS%", _fileSystem.GetKnownPath(KnownPath.MyDocumentsDirectory).GetFullPath()));
                            dataPath = _fileSystem.FromFullPath(strDataPath);
                        }
                    }
                    if (exe == default || !exe.FileExists)
                        exe = Utils.FindExe(path, _fileSystem, name);
                }

                yield return new ParadoxGame(
                    Id: ParadoxGameId.From(id),
                    Name: name,
                    InstallationPath: path,
                    GameDataPath: dataPath,
                    ExePath: exe,
                    ExeArgs: args,
                    AppIcon: Path.IsPathRooted(strIcon) ? _fileSystem.FromFullPath(strIcon) : default,
                    LastLaunch: lastLaunch,
                    AppTaskbarIcon: Path.IsPathRooted(strTaskIcon) ? _fileSystem.FromFullPath(strTaskIcon) : default,
                    Background: Path.IsPathRooted(strBg) ? _fileSystem.FromFullPath(strBg) : default,
                    Logo: Path.IsPathRooted(strLogo) ? _fileSystem.FromFullPath(strLogo) : default
                );
            }
        }
    }
    public AbsolutePath GetParadoxV1Path()
    {
        return _fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
        .CombineUnchecked("Paradox Interactive")
        .CombineUnchecked("launcher");
    }
    public AbsolutePath GetParadoxV2Path()
    {
        return _fileSystem.GetKnownPath(KnownPath.ApplicationDataDirectory)
        .CombineUnchecked("Paradox Interactive")
        .CombineUnchecked("launcher-v2");
    }
}
