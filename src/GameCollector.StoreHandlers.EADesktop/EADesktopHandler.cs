using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using GameCollector.StoreHandlers.EADesktop.Crypto;
using GameCollector.StoreHandlers.EADesktop.Crypto.Windows;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.EADesktop;

/// <summary>
/// Handler for finding games installed with EA Desktop.
/// </summary>
[PublicAPI]
public class EADesktopHandler : AHandler<Game, string>
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
    public const int SupportedSchemaVersion = 22;

    /// <summary>
    /// Policy to use when the schema version does not match <see cref="SupportedSchemaVersion"/>.
    /// The default behavior is <see cref="EADesktop.SchemaPolicy.Warn"/>.
    /// </summary>
    public SchemaPolicy SchemaPolicy { get; set; } = SchemaPolicy.Warn;

    /// <summary>
    /// Constructor.
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
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var dataFolder = GetDataFolder(_fileSystem);
        if (!_fileSystem.DirectoryExists(dataFolder))
        {
            yield return Result.FromError<Game>($"Data folder {dataFolder} does not exist!");
            yield break;
        }

        var installInfoFile = GetInstallInfoFile(dataFolder);
        if (!_fileSystem.FileExists(installInfoFile))
        {
            yield return Result.FromError<Game>($"File does not exist: {installInfoFile.GetFullPath()}");
            yield break;
        }

        var decryptionResult = DecryptInstallInfoFile(_fileSystem, installInfoFile, _hardwareInfoProvider);
        var (plaintext, decryptionError) = decryptionResult;
        if (plaintext is null)
        {
            yield return Result.FromError<Game>(decryptionError ?? $"Error decryption file {installInfoFile.GetFullPath()}");
            yield break;
        }

        foreach (var result in ParseInstallInfoFile(plaintext, installInfoFile, SchemaPolicy, _registry, _fileSystem, installedOnly))
        {
            yield return result;
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }

    internal static AbsolutePath GetDataFolder(IFileSystem fileSystem)
    {
        return fileSystem
            .GetKnownPath(KnownPath.CommonApplicationDataDirectory)
            .CombineUnchecked("EA Desktop");
    }

    internal static AbsolutePath GetInstallInfoFile(AbsolutePath dataFolder)
    {
        return dataFolder
            .CombineUnchecked(AllUsersFolderName)
            .CombineUnchecked(InstallInfoFileName);
    }

    internal static Result<string> DecryptInstallInfoFile(IFileSystem fileSystem, AbsolutePath installInfoFile, IHardwareInfoProvider hardwareInfoProvider)
    {
        try
        {
            using var stream = fileSystem.ReadFile(installInfoFile);
            var cipherText = new byte[stream.Length];
            var count = stream.Read(cipherText);

            var key = Decryption.CreateDecryptionKey(hardwareInfoProvider);

            var iv = Decryption.CreateDecryptionIV();
            var plainText = Decryption.DecryptFile(cipherText, key, iv);
            return Result.FromGame(plainText);
        }
        catch (Exception e)
        {
            return Result.FromException<string>($"Exception while decrypting file {installInfoFile.GetFullPath()}", e);
        }
    }

    internal IEnumerable<Result<Game>> ParseInstallInfoFile(string plaintext, AbsolutePath installInfoFile, SchemaPolicy schemaPolicy)
    {
        try
        {
            return ParseInstallInfoFileInner(plaintext, installInfoFile, schemaPolicy, registry, fileSystem, installedOnly);
        }
        catch (Exception e)
        {
            return new[]
            {
                Result.FromException<Game>($"Exception while parsing InstallInfoFile {installInfoFile.GetFullPath()}", e),
            };
        }
    }

    private IEnumerable<Result<Game>> ParseInstallInfoFileInner(string plaintext, AbsolutePath installInfoFile, SchemaPolicy schemaPolicy)
    {
        var installInfoFileContents = JsonSerializer.Deserialize<InstallInfoFile>(plaintext, JsonSerializerOptions);

        if (installInfoFileContents is null)
        {
            yield return Result.FromError<Game>($"Unable to deserialize InstallInfoFile {installInfoFile.GetFullPath()}");
            yield break;
        }

        var schemaVersionNullable = installInfoFileContents.Schema?.Version;
        if (!schemaVersionNullable.HasValue)
        {
            yield return Result.FromError<Game>($"InstallInfoFile {installInfoFile.GetFullPath()} does not have a schema version!");
            yield break;
        }

        var schemaVersion = schemaVersionNullable.Value;
        var (schemaMessage, isSchemaError) = CreateSchemaVersionMessage(schemaPolicy, schemaVersion, installInfoFile);
        if (schemaMessage is not null)
        {
            yield return Result.FromError<Game>(schemaMessage);
            if (isSchemaError) yield break;
        }

        var installInfos = installInfoFileContents.InstallInfos;
        if (installInfos is null || installInfos.Count == 0)
        {
            yield return Result.FromError<Game>($"InstallInfoFile {installInfoFile.GetFullPath()} does not have any infos!");
            yield break;
        }

        for (var i = 0; i < installInfos.Count; i++)
        {
            yield return InstallInfoToGame(_fileSystem, installInfos[i], i, installInfoFile);
        }
    }

    internal static (string? message, bool isError) CreateSchemaVersionMessage(
        SchemaPolicy schemaPolicy, int schemaVersion, AbsolutePath installInfoFilePath)
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

    internal static Result<Game> InstallInfoToGame(IFileSystem fileSystem, InstallInfo installInfo, int i, AbsolutePath installInfoFilePath)
    {
        var isInstalled = true;
        var num = i.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(installInfo.SoftwareID))
        {
            return Result.FromError<Game>($"InstallInfo #{num} does not have the value \"softwareId\"");
        }

        var softwareId = installInfo.SoftwareID;

        if (string.IsNullOrEmpty(installInfo.BaseSlug))
        {
            return Result.FromError<Game>($"InstallInfo #{num} for {softwareId} does not have the value \"baseSlug\"");
        }

        var baseSlug = installInfo.BaseSlug;

        if (string.IsNullOrEmpty(installInfo.BaseInstallPath))
        {
            if (installedOnly)
                return Result.FromError<Game>($"InstallInfo #{num} for {softwareId} ({baseSlug}) does not have the value \"baseInstallPath\"");
            isInstalled = false;
        }

        if (string.IsNullOrEmpty(installInfo.ExecutableCheck))
        {
            if (installedOnly)
                return Result.FromError<Game>($"InstallInfo #{num} for {softwareId} ({baseSlug}) does not have the value \"executableCheck\"");
            isInstalled = false;
        }

        var baseInstallPath = installInfo.BaseInstallPath;
        if (string.IsNullOrEmpty(baseInstallPath))
            baseInstallPath = "";
        else if (baseInstallPath.EndsWith('\\'))
            baseInstallPath = baseInstallPath[..^1];
        var executableCheck = installInfo.ExecutableCheck;
        if (string.IsNullOrEmpty(executableCheck))
            executableCheck = "";
        var executable = "";
        var sRegKey = "";
        if (executableCheck.StartsWith('['))
        {
            int j = executableCheck.IndexOf(']');
            if (j > 1)
                sRegKey = executableCheck.Substring(1, j - 1);
            executable = fileSystem.Path.Combine(baseInstallPath, executableCheck[(j + 1)..]);
        }
        var uninstall = "";
        var uninstallArgs = "";
        var localUninstallProperties = installInfo.LocalUninstallProperties;
        if (localUninstallProperties.TryGetProperty("uninstallCommand", out JsonElement jUninstall))
        {
            uninstall = jUninstall.GetString() ?? "";
            if (localUninstallProperties.TryGetProperty("uninstallParameters", out JsonElement jUninstParam))
            {
                uninstallArgs = jUninstParam.GetString() ?? "";
            }
        }
        string? name = "";
        if (!string.IsNullOrEmpty(sRegKey))
        {
            try
            {
                int k = sRegKey.IndexOf('\\', StringComparison.Ordinal);
                int l = sRegKey.LastIndexOf('\\');
                if (k > 1 && l > k)
                {
                    var regRoot = registry.OpenBaseKey(RegistryHelpers.RegistryHiveFromString(sRegKey[..k].ToUpperInvariant()), RegistryView.Registry32);
                    string sSubKey = sRegKey[(k + 1)..l];
                    var subKey = regRoot.OpenSubKey(sSubKey);
                    subKey?.TryGetString("DisplayName", out name);
                }
            }
            catch (Exception) { }
        }

        if (string.IsNullOrEmpty(name))
            name = baseSlug;
        Game game = new(
            Id: softwareId,
            Name: name,
            Path: fileSystem.FromFullPath(baseInstallPath),
            Launch: executable,
            Icon: executable,
            Uninstall: uninstall,
            UninstallArgs: uninstallArgs,
            IsInstalled: isInstalled,
            Metadata: new(StringComparer.OrdinalIgnoreCase) { ["baseSlug"] = new() { baseSlug } });

        return Result.FromGame(game);
    }
}
