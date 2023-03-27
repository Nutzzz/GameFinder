using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace GameCollector.Common;

/// <summary>
/// Generic representation of a game.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Path"></param>
/// <param name="Launch"></param>
/// <param name="LaunchArgs"></param>
/// <param name="Icon"></param>
/// <param name="Uninstall"></param>
/// <param name="UninstallArgs"></param>
/// <param name="LastRunDate"></param>
/// <param name="IsInstalled"></param>
/// <param name="HasProblem"></param>
/// <param name="IsClone"></param>
/// <param name="Metadata"></param>
[PublicAPI]
public record Game(
    string Id,
    string Name,
    string Path,
    string Launch = "",
    string LaunchArgs = "",
    string Icon = "",
    string Uninstall = "",
    string UninstallArgs = "",
    DateTime? LastRunDate = null,
    bool IsInstalled = true,
    bool HasProblem = false,
    bool IsClone = false,
    Dictionary<string, List<string>>? Metadata = null);
