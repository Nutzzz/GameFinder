using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.BattleNet;

[UsedImplicitly]
internal class CacheFile
{
    [JsonPropertyName("all")]
    public CacheFileConfig? All { get; init; }
    [JsonPropertyName("enus")]
    public CacheFileConfig? DefaultLanguage { get; init; }
    [JsonPropertyName("platform")]
    public CacheFilePlatform? Platform { get; init; }
}

[UsedImplicitly]
internal class CacheFileConfig
{
    [JsonPropertyName("config")]
    public CacheConfig? Config { get; init; }
}

[UsedImplicitly]
internal class CacheConfig
{
    [JsonPropertyName("binaries")]
    public CacheConfigBin? Binaries { get; init; }
    [JsonPropertyName("form")]
    public CacheConfigForm? Form { get; init; }
    [JsonPropertyName("install")]
    public List<CacheConfigInstall>? Install { get; init; }
    [JsonPropertyName("product")]
    public string? Product { get; init; }
    [JsonPropertyName("shared_container_default_subfolder")]
    public string? SharedContainerDefaultSubfolder { get; init; }
    [JsonPropertyName("supported_locales")]
    public List<string>? SupportedLocales { get; init; }
}

[UsedImplicitly]
internal class CacheConfigBin
{
    [JsonPropertyName("game")]
    public CacheBinGame? Game { get; init; }
}

[UsedImplicitly]
internal class CacheBinGame
{
    [JsonPropertyName("launch_arguments")]
    public List<string>? LaunchArguments { get; init; }
    [JsonPropertyName("relative_path")]
    public string? RelativePath { get; init; }
    [JsonPropertyName("relative_path_64")]
    public string? RelativePath64 { get; init; }
}

[UsedImplicitly]
internal class CacheConfigInstall
{
    [JsonPropertyName("start_menu_shortcut")]
    public CacheInstallCut? StartMenuShortcut { get; init; }
    [JsonPropertyName("add_remove_programs_key")]
    public CacheInstallUninst? AddRemoveProgramsKey { get; init; }
    [JsonPropertyName("program_associations")]
    public CacheInstallAssoc? ProgramAssociations { get; init; }
}

[UsedImplicitly]
internal class CacheInstallCut
{
    [JsonPropertyName("args")]
    public string? Args { get; init; }
}

[UsedImplicitly]
internal class CacheInstallUninst
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }
}

[UsedImplicitly]
internal class CacheInstallAssoc
{
    [JsonPropertyName("application_description")]
    public string? ApplicationDescription { get; init; }
    [JsonPropertyName("application_display_name")]
    public string? ApplicationDisplayName { get; init; }
}

[UsedImplicitly]
internal class CacheFilePlatform
{
    [JsonPropertyName("win")]
    public CacheFileConfig? Win { get; init; }
}

[UsedImplicitly]
internal class CacheConfigForm
{
    [JsonPropertyName("game_dir")]
    public CacheGameDir? GameDir { get; init; }
}

[UsedImplicitly]
internal class CacheGameDir
{
    [JsonPropertyName("dirname")]
    public string? Dirname { get; init; }
}
