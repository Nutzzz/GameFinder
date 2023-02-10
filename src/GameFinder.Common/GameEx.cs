using System.Collections.Generic;
using JetBrains.Annotations;

namespace GameFinder.Common;

/// <summary>
/// Extended representation of a game.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Path"></param>
/// <param name="Launch"></param>
/// <param name="Icon"></param>
/// <param name="Uninstall"></param>
/// <param name="Metadata"></param>
[PublicAPI]
public record GameEx(string Id, string Name, string Path, string Launch = "", string Icon = "", string Uninstall = "", Dictionary<string, List<string>>? Metadata = null);
