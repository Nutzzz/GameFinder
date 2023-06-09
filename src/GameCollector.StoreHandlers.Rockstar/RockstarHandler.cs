using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using JetBrains.Annotations;
using NexusMods.Paths;
using OneOf;

namespace GameCollector.StoreHandlers.Rockstar;

// TODO: Confirm this handler works

/// <summary>
/// Handler for finding games installed with Rockstar Games Launcher.
/// </summary>
[PublicAPI]
public class RockstarHandler : AHandler<RockstarGame, string>
{
    internal const string RockstarKey = @"SOFTWARE\Rockstar Games";
    internal const string UninstallRegKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

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
    public RockstarHandler(IRegistry registry, IFileSystem fileSystem)
    {
        _registry = registry;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEqualityComparer<string>? IdEqualityComparer => null;

    /// <inheritdoc/>
    public override Func<RockstarGame, string> IdSelector => game => game.GameId;

    /// <inheritdoc/>
    public override AbsolutePath FindClient()
    {
        if (_registry is not null)
        {
            var localMachine32 = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            using var regKey = localMachine32.OpenSubKey(Path.Combine(UninstallRegKey, "Rockstar Games Launcher"));
            if (regKey is not null)
            {
                if (regKey.TryGetString("DisplayIcon", out var icon) && Path.IsPathRooted(icon))
                    return _fileSystem.FromFullPath(SanitizeInputPath(icon));
            }
        }

        return default;
    }

    /// <inheritdoc/>
    public override IEnumerable<OneOf<RockstarGame, ErrorMessage>> FindAllGames(bool installedOnly = false, bool baseOnly = false)
    {
        var localMachine32 = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

        using var rockstarKey = localMachine32.OpenSubKey(RockstarKey);
        if (rockstarKey is null)
        {
            yield return new ErrorMessage($@"Unable to open HKEY_LOCAL_MACHINE\{RockstarKey}");
            yield break;
        }

        using var unKey = localMachine32.OpenSubKey(UninstallRegKey);

        var subKeyNames = rockstarKey.GetSubKeyNames().ToArray();
        if (subKeyNames.Length == 0)
        {
            yield return new ErrorMessage($"Registry key {rockstarKey.GetName()} has no sub-keys");
            yield break;
        }

        foreach (var subKeyName in subKeyNames)
        {
            if (!subKeyName.Equals("Launcher", StringComparison.OrdinalIgnoreCase) &&
                !subKeyName.Equals("Rockstar Games Launcher", StringComparison.OrdinalIgnoreCase) &&
                !subKeyName.Equals("Rockstar Games Social Club", StringComparison.OrdinalIgnoreCase))
            {
                yield return ParseRockstarKey(rockstarKey, unKey, subKeyName, _fileSystem);
            }
        }
    }

    private static OneOf<RockstarGame, ErrorMessage> ParseRockstarKey(IRegistryKey rockstarKey, IRegistryKey? unKey, string subKeyName, IFileSystem fileSystem)
    {
        try
        {
            using var subKey = rockstarKey.OpenSubKey(subKeyName);
            if (subKey is null)
                return new ErrorMessage($"Unable to open {rockstarKey}\\{subKeyName}");

            if (!subKey.TryGetString("InstallFolder", out var strPath))
                return new ErrorMessage($"{subKey.GetName()} doesn't have a string value \"InstallFolder\"");

            if (!Path.IsPathRooted(strPath))
                return new ErrorMessage($"{strPath} is not a valid path");

            var id = "";
            var path = fileSystem.FromFullPath(SanitizeInputPath(strPath));
            AbsolutePath exe = new();
            AbsolutePath uninst = new();
            var uninstArgs = "";

            var (name, strExe, strUninst) = ParseUninstallKey(unKey, path);

            if (!string.IsNullOrEmpty(strUninst))
            {
                if (strUninst.Contains("\" ", StringComparison.Ordinal))
                {
                    var uninstExe = SanitizeInputPath(strUninst[..strUninst.IndexOf("\" ", StringComparison.Ordinal)].Trim('\"'));
                    if (Path.IsPathRooted(uninstExe))
                    {
                        uninst = fileSystem.FromFullPath(uninstExe);
                        uninstArgs = strUninst[(strUninst.IndexOf("\" ", StringComparison.Ordinal) + 2)..];
                    }
                    if (strUninst.Contains("-uninstall=", StringComparison.Ordinal))
                        id = strUninst[..(strUninst.LastIndexOf("-uninstall=", StringComparison.Ordinal) + 11)];
                }
            }
            else
                id = path.FileName;

            if (string.IsNullOrEmpty(name))
                name = subKeyName;
            if (string.IsNullOrEmpty(strExe))
                exe = Utils.FindExe(path, fileSystem, name);
            
            return new RockstarGame(
                Id: RockstarGameId.From(id),
                Name: subKeyName,
                InstallFolder: path,
                Launch: exe,
                Uninstall: uninst,
                UninstallArgs: uninstArgs);
        }
        catch (Exception e)
        {
            return new ErrorMessage(e, $"Exception while parsing registry key {rockstarKey}\\{subKeyName}");
        }
    }

    private static (string? name, string? exe, string? uninst) ParseUninstallKey(IRegistryKey? uninstallKey, AbsolutePath rockstarPath)
    {
        try
        {
            if (uninstallKey is null)
                return new();

            var subKeyNames = uninstallKey.GetSubKeyNames().ToArray();
            foreach (var subKeyName in subKeyNames)
            {
                using var subKey = uninstallKey.OpenSubKey(subKeyName);
                if (subKey is null)
                    continue;

                if (subKey.TryGetString("InstallLocation", out var loc) &&
                    loc.Equals(rockstarPath.GetFullPath(), StringComparison.OrdinalIgnoreCase))
                {
                    subKey.TryGetString("DisplayName", out var name);
                    subKey.TryGetString("DisplayIcon", out var exe);
                    subKey.TryGetString("UninstallString", out var uninst);
                    return (name, exe, uninst);
                }
            }
        }
        catch (Exception) { }

        return new();
    }
}
