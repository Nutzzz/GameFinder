using System.Collections.Generic;
using YamlDotNet.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.Riot;

[UsedImplicitly]
internal class SettingsFile
{
    public List<string>? UserDataPaths { get; init; }
    public string? ProductInstallFullPath { get; init; }
    public string? ProductInstallRoot { get; init; }
    public string? ShortcutName { get; init; }
}
