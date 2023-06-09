using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.BattleNet;

[UsedImplicitly]
internal record CacheFile(
    CacheFileConfig? All,
    [property: JsonPropertyName("enus")]
    CacheFileConfig? DefaultLanguage,
    CacheFilePlatform? Platform
);

[UsedImplicitly]
internal record CacheFileConfig(
    CacheConfig? Config
);

[UsedImplicitly]
internal record CacheConfig(
    CacheConfigBin? Binaries,
    CacheConfigForm? Form,
    List<CacheConfigInstall>? Install,
    string? Product,
    [property: JsonPropertyName("shared_container_default_subfolder")]
    string? SharedContainerDefaultSubfolder,
    [property: JsonPropertyName("supported_locales")]
    List<string>? SupportedLocales
);

[UsedImplicitly]
internal record CacheConfigBin(
    CacheBinGame? Game
);

[UsedImplicitly]
internal record CacheBinGame(
    [property: JsonPropertyName("launch_arguments")]
    List<string>? LaunchArguments,
    [property: JsonPropertyName("relative_path")]
    string? RelativePath,
    [property: JsonPropertyName("relative_path_64")]
    string? RelativePath64
);

[UsedImplicitly]
internal record CacheConfigInstall(
    [property: JsonPropertyName("start_menu_shortcut")]
    CacheInstallCut? StartMenuShortcut,
    [property: JsonPropertyName("add_remove_programs_key")]
    CacheInstallUninst? AddRemoveProgramsKey,
    [property: JsonPropertyName("program_associations")]
    CacheInstallAssoc? ProgramAssociations
);

[UsedImplicitly]
internal record CacheInstallCut(
    string? Args
);

[UsedImplicitly]
internal record CacheInstallUninst(
    [property: JsonPropertyName("display_name")]
    string? DisplayName
);

[UsedImplicitly]
internal record CacheInstallAssoc(
    [property: JsonPropertyName("application_description")]
    string? ApplicationDescription,
    [property: JsonPropertyName("application_display_name")]
    string? ApplicationDisplayName
);

[UsedImplicitly]
internal record CacheFilePlatform(
    CacheFileConfig? Win
);

[UsedImplicitly]
internal record CacheConfigForm(
    [property: JsonPropertyName("game_dir")]
    CacheGameDir? GameDir
);

[UsedImplicitly]
internal record CacheGameDir(
    string? Dirname
);
