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
/// <param name="Icon"></param>
/// <param name="Uninstall"></param>
/// <param name="LastRunDate"></param>
/// <param name="Metadata"></param>
[PublicAPI]
public record Game(
    string Id,
    string Name,
    string Path,
    string Launch = "",
    string Icon = "",
    string Uninstall = "",
    DateTime? LastRunDate = null,
    Dictionary<string, List<string>>? Metadata = null);
