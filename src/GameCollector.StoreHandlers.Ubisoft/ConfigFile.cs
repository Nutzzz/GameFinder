using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace GameCollector.StoreHandlers.Ubisoft;

[UsedImplicitly]
internal record ConfigFile
{
    [DefaultValue("2.0")]
    public string? Version { get; init; } = "2.0";
    public ConfigRoot? Root { get; init; }
    public ConfigLocalize? Localizations { get; init; }
}

[UsedImplicitly]
internal record ConfigRoot
{
    public string? Name { get; init; }
    public ConfigUplay? Uplay { get; init; }
    public string? DisplayName { get; init; }
    public string? ThumbImage { get; init; }
    public string? IconImage { get; init; }
    public ConfigStart? StartGame { get; init; }
    public ConfigInstall? Installer { get; init; }
    public ConfigThirdParty? ThirdPartyPlatform { get; init; }
    public string? AppId { get; init; }
    [DefaultValue("false")]
    public string? IsDlc { get; init; } = "false";
    [DefaultValue("false")]
    public string? IsUlc { get; init; } = "false";
    [DefaultValue("false")]
    public string? OptionalAddonEnabledByDefault { get; init; } = "false";
}

[UsedImplicitly]
internal record ConfigUplay
{
    public string? GameCode { get; init; }
    public string? AchievementsSyncId { get; init; }
}

[UsedImplicitly]
internal record ConfigStart
{
    public ConfigGame? Online { get; init; }
    public ConfigGame? Offline { get; init; }
    public ConfigGameSteam? Steam { get; init; }
}

[UsedImplicitly]
internal record ConfigGame
{
    public List<ConfigGameExe>? Executables { get; init; }
}

[UsedImplicitly]
internal record ConfigGameSteam
{
    public string? SteamAppId { get; init; }
    public string? SteamInstallationStatusRegister { get; init; }
    public string? GameInstallationStatusRegister { get; init; }
}

[UsedImplicitly]
internal record ConfigGameExe
{
    public string? ShortcutName { get; init; }
    public string? Description { get; init; }
    public ConfigExe? Path { get; init; }
    public ConfigExe? Trial { get; init; }
    public ConfigExe? WorkingDirectory { get; init; }
    public string? Arguments { get; init; }
    public string? PlayArguments { get; init; }
    public string? IconImage { get; init; }
    public string? UplayPipeRequired { get; init; }
}

[UsedImplicitly]
internal record ConfigExe
{
    public string? Register { get; init; }
    public string? Relative { get; init; }
    public string? Arguments { get; init; }
    public string? Append { get; init; }
}

[UsedImplicitly]
internal record ConfigInstall
{
    public string? GameIdentifier { get; init; }
}

[UsedImplicitly]
internal record ConfigThirdParty
{
    public string? Name { get; init; }
}

[UsedImplicitly]
internal record ConfigLocalize
{
    [YamlMember(Alias = "default")]
    public ConfigLocalizeLang? Default { get; init; }
}

[UsedImplicitly]
internal record ConfigLocalizeLang
{
    [YamlMember(Alias = "NAME")]
    public string? Name { get; init; }
    [YamlMember(Alias = "GAMENAME")]
    public string? Gamename { get; init; }
    [YamlMember(Alias = "THUMBIMAGE")]
    public string? Thumbimage { get; init; }
    [YamlMember(Alias = "ICONIMAGE")]
    public string? Iconimage { get; init; }
    [YamlMember(Alias = "DESCRIPTION")]
    public string? Description { get; init; }
    [YamlMember(Alias = "l1")]
    public string? Localize1 { get; init; }
    [YamlMember(Alias = "l2")]
    public string? Localize2 { get; init; }
    [YamlMember(Alias = "l3")]
    public string? Localize3 { get; init; }
    [YamlMember(Alias = "l4")]
    public string? Localize4 { get; init; }
    [YamlMember(Alias = "l5")]
    public string? Localize5 { get; init; }
}
