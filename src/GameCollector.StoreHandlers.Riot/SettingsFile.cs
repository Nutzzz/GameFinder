using System.Collections.Generic;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Riot;

[UsedImplicitly]
internal record SettingsFile
{
    public List<string>? UserDataPaths { get; init; }
    public string? ProductInstallFullPath { get; init; }
    public string? ProductInstallRoot { get; init; }
    public string? ShortcutName { get; init; }
}
