using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.EADesktop.Crypto;
using GameFinder.StoreHandlers.EADesktop.Crypto.Windows;
using JetBrains.Annotations;

namespace GameFinder.StoreHandlers.EADesktop;

/// <summary>
/// Handler for finding games installed with EA Desktop.
/// </summary>
[PublicAPI]
public class EADesktopHandler : AHandler<EADesktopGame, string>
{
    internal const string AllUsersFolderName = "530c11479fe252fc5aabc24935b9776d4900eb3ba58fdc271e0d6229413ad40e";
    internal const string InstallInfoFileName = "IS";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.Strict,
    };

    private readonly IRegistry _registry;
    private readonly IFileSystem _fileSystem;
    private readonly IHardwareInfoProvider _hardwareInfoProvider;

    /// <summary>
    /// The supported schema version of this handler. You can change the schema policy with
    /// <see cref="SchemaPolicy"/>.
    /// </summary>
    public const int SupportedSchemaVersion = 21;

    /// <summary>
    /// Policy to use when the schema version does not match <see cref="SupportedSchemaVersion"/>.
    /// The default behavior is <see cref="EADesktop.SchemaPolicy.Warn"/>.
    /// </summary>
    public SchemaPolicy SchemaPolicy { get; set; } = SchemaPolicy.Warn;

    /// <summary>
    /// Default constructor that uses the <see cref="WindowsRegistry"/> implementation
    /// of <see cref="IRegistry"/>, real filesystem <see cref="FileSystem"/> and
    /// real hardware information <see cref="HardwareInfoProvider"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public EADesktopHandler() : this(new WindowsRegistry(), new FileSystem(), new HardwareInfoProvider()) { }

    /// <summary>
    /// Constructor for specifying the implementation of <see cref="IRegistry"/>,
    /// <see cref="IFileSystem"/> and <see cref="IHardwareInfoProvider"/>.
    /// Use this constructor if you want to run tests.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="fileSystem"></param>
    /// <param name="hardwareInfoProvider"></param>
    public EADesktopHandler(IRegistry registry, IFileSystem fileSystem, IHardwareInfoProvider hardwareInfoProvider)
    {
        _registry = registry;
        _fileSystem = fileSystem;
        _hardwareInfoProvider = hardwareInfoProvider;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<EADesktopGame>> FindAllGames()
    {
        var games = new List<Result<EADesktopGame>>();
        foreach (var gameEx in FindAllGamesEx(installedOnly: true).OnlyGames())
        {
            var baseSlug = gameEx.Metadata?["baseSlug"][0];
            baseSlug ??= gameEx.Name;
            games.Add(Result.FromGame(new EADesktopGame(gameEx.Id, baseSlug, gameEx.Path)));
        }
        return games;
    }

    /// <summary>
    /// Finds all games installed with this store. The return type <see cref="Result{TGame}"/>
    /// will always be a non-null game or a non-null error message.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Result<GameEx>> FindAllGamesEx(bool installedOnly = false)
    {
        var dataFolder = GetDataFolder(_fileSystem);
        if (!dataFolder.Exists)
        {
            yield return Result.FromError<GameEx>($"Data folder {dataFolder} does not exist!");
            yield break;
        }

        var installInfoFile = GetInstallInfoFile(dataFolder);
        if (!installInfoFile.Exists)
        {
            yield return Result.FromError<GameEx>($"File does not exist: {installInfoFile.FullName}");
            yield break;
        }

        var decryptionResult = DecryptInstallInfoFile(installInfoFile, _hardwareInfoProvider);
        var (plaintext, decryptionError) = decryptionResult;
        if (plaintext is null)
        {
            yield return Result.FromError<GameEx>(decryptionError ?? $"Error decryption file {installInfoFile.FullName}");
            yield break;
        }

        foreach (var result in ParseInstallInfoFileEx(plaintext, installInfoFile, SchemaPolicy, _registry, _fileSystem, installedOnly))
        {
            yield return result;
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, EADesktopGame> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.SoftwareID, game => game,StringComparer.OrdinalIgnoreCase);
    }

    internal static IDirectoryInfo GetDataFolder(IFileSystem fileSystem)
    {
        return fileSystem.DirectoryInfo.New(fileSystem.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EA Desktop"
        ));
    }

    internal static IFileInfo GetInstallInfoFile(IDirectoryInfo dataFolder)
    {
        var fileSystem = dataFolder.FileSystem;

        return fileSystem.FileInfo.New(fileSystem.Path.Combine(
            dataFolder.FullName,
            AllUsersFolderName,
            InstallInfoFileName
        ));
    }

    internal static Result<string> DecryptInstallInfoFile(IFileInfo installInfoFile, IHardwareInfoProvider hardwareInfoProvider)
    {
        try
        {
            var cipherText = installInfoFile.FileSystem.File.ReadAllBytes(installInfoFile.FullName);
            var key = Decryption.CreateDecryptionKey(hardwareInfoProvider);

            var iv = Decryption.CreateDecryptionIV();
            var plainText = Decryption.DecryptFile(cipherText, key, iv);
            return Result.FromGame(plainText);
        }
        catch (Exception e)
        {
            return Result.FromException<string>($"Exception while decrypting file {installInfoFile.FullName}", e);
        }
    }

    internal static IEnumerable<Result<GameEx>> ParseInstallInfoFileEx(string plaintext, IFileInfo installInfoFile, SchemaPolicy schemaPolicy, IRegistry registry, IFileSystem fileSystem, bool installedOnly = false)
    {
        try
        {
            return ParseInstallInfoFileInnerEx(plaintext, installInfoFile, schemaPolicy, registry, fileSystem, installedOnly);
        }
        catch (Exception e)
        {
            return new[]
            {
                Result.FromException<GameEx>($"Exception while parsing InstallInfoFile {installInfoFile.FullName}", e),
            };
        }
    }

    private static IEnumerable<Result<GameEx>> ParseInstallInfoFileInnerEx(string plaintext, IFileInfo installInfoFile, SchemaPolicy schemaPolicy, IRegistry registry, IFileSystem fileSystem, bool installedOnly = false)
    {
        var installInfoFileContents = JsonSerializer.Deserialize<InstallInfoFile>(plaintext, JsonSerializerOptions);

        if (installInfoFileContents is null)
        {
            yield return Result.FromError<GameEx>($"Unable to deserialize InstallInfoFile {installInfoFile.FullName}");
            yield break;
        }

        var schemaVersionNullable = installInfoFileContents.Schema?.Version;
        if (!schemaVersionNullable.HasValue)
        {
            yield return Result.FromError<GameEx>($"InstallInfoFile {installInfoFile.FullName} does not have a schema version!");
            yield break;
        }

        var schemaVersion = schemaVersionNullable.Value;
        var (schemaMessage, isSchemaError) = CreateSchemaVersionMessage(schemaPolicy, schemaVersion, installInfoFile.FullName);
        if (schemaMessage is not null)
        {
            yield return Result.FromError<GameEx>(schemaMessage);
            if (isSchemaError) yield break;
        }

        var installInfos = installInfoFileContents.InstallInfos;
        if (installInfos is null || installInfos.Count == 0)
        {
            yield return Result.FromError<GameEx>($"InstallInfoFile {installInfoFile.FullName} does not have any infos!");
            yield break;
        }

        for (var i = 0; i < installInfos.Count; i++)
        {
            yield return InstallInfoToGameEx(installInfos[i], i, installInfoFile.FullName, registry, fileSystem, installedOnly);
        }
    }

    internal static (string? message, bool isError) CreateSchemaVersionMessage(
        SchemaPolicy schemaPolicy, int schemaVersion, string installInfoFilePath)
    {
        if (schemaVersion == SupportedSchemaVersion) return (null, false);

        return schemaPolicy switch
        {
            SchemaPolicy.Warn => (
                $"InstallInfoFile {installInfoFilePath} has a schema version " +
                $"{schemaVersion.ToString(CultureInfo.InvariantCulture)} but this library only supports schema version " +
                $"{SupportedSchemaVersion.ToString(CultureInfo.InvariantCulture)}. " +
                $"This message is a WARNING because the consumer of this library has set {nameof(SchemaPolicy)} to {nameof(SchemaPolicy.Warn)}",
                false),
            SchemaPolicy.Error => (
                $"InstallInfoFile {installInfoFilePath} has a schema version " +
                $"{schemaVersion.ToString(CultureInfo.InvariantCulture)} but this library only supports schema version " +
                $"{SupportedSchemaVersion.ToString(CultureInfo.InvariantCulture)}. " +
                $"This is an ERROR because the consumer of this library has set {nameof(SchemaPolicy)} to {nameof(SchemaPolicy.Error)}",
                true),
            SchemaPolicy.Ignore => (null, false),
            _ => throw new ArgumentOutOfRangeException(nameof(schemaPolicy), schemaPolicy, message: null),
        };
    }

    internal static Result<GameEx> InstallInfoToGameEx(InstallInfo installInfo, int i, string installInfoFilePath, IRegistry registry, IFileSystem fileSystem, bool installedOnly = true)
    {
        var num = i.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(installInfo.SoftwareID))
        {
            return Result.FromError<GameEx>($"InstallInfo #{num} does not have the value \"softwareId\"");
        }

        var softwareId = installInfo.SoftwareID;

        if (string.IsNullOrEmpty(installInfo.BaseSlug))
        {
            return Result.FromError<GameEx>($"InstallInfo #{num} for {softwareId} does not have the value \"baseSlug\"");
        }

        var baseSlug = installInfo.BaseSlug;

        if (string.IsNullOrEmpty(installInfo.BaseInstallPath) && installedOnly)
        {
            return Result.FromError<GameEx>($"InstallInfo #{num} for {softwareId} ({baseSlug}) does not have the value \"baseInstallPath\"");
        }

        if (string.IsNullOrEmpty(installInfo.ExecutableCheck) && installedOnly)
        {
            return Result.FromError<GameEx>($"InstallInfo #{num} for {softwareId} ({baseSlug}) does not have the value \"executableCheck\"");
        }

        var baseInstallPath = installInfo.BaseInstallPath;
        if (string.IsNullOrEmpty(baseInstallPath))
            baseInstallPath = "";
        else if (baseInstallPath.EndsWith('\\'))
            baseInstallPath = baseInstallPath[..^1];
        var executableCheck = installInfo.ExecutableCheck;
        if (string.IsNullOrEmpty(executableCheck))
            executableCheck = "";
        string executable = "";
        string sRegKey = "";
        if (executableCheck.StartsWith('['))
        {
            int j = executableCheck.IndexOf(']');
            if (j > 1)
                sRegKey = executableCheck.Substring(1, j);
            executable = Path.Combine(baseInstallPath, executableCheck[(j + 1)..]);
        }
        string? uninstall = "";
        var localUninstallProperties = installInfo.LocalUninstallProperties;
        if (localUninstallProperties.TryGetProperty("uninstallCommand", out JsonElement jUninstall))
        {
            uninstall = "\"" + jUninstall.GetString() + "\"";
            if (localUninstallProperties.TryGetProperty("uninstallParameters", out JsonElement jUninstParam))
            {
                string? uninstParam = jUninstParam.GetString();
                if (!string.IsNullOrEmpty(uninstParam))
                    uninstall += " " + uninstParam;
            }
        }
        string? name = "";
        if (!string.IsNullOrEmpty(sRegKey))
        {
            try
            {
                int k = sRegKey.IndexOf('\\', StringComparison.Ordinal);
                if (k > 1)
                {
                    var regRoot = registry.OpenBaseKey(RegistryHelpers.RegistryHiveFromString(sRegKey[..k].ToUpperInvariant()), RegistryView.Registry64);
                    var subKey = regRoot.OpenSubKey(sRegKey[(k + 1)..]);
                    name = subKey?.GetString("DisplayName");
                }
            }
            catch (Exception) { }
        }

        if (string.IsNullOrEmpty(name))
            name = baseSlug;
        var gameEx = new GameEx(
            softwareId,
            name,
            baseInstallPath,
            executable,
            executable,
            uninstall,
            new(StringComparer.Ordinal) { ["baseSlug"] = new() { baseSlug } });

        return Result.FromGame(gameEx);
    }
}
