using YamlDotNet.RepresentationModel;
using JetBrains.Annotations;
using System.ComponentModel;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace GameCollector.StoreHandlers.Ubisoft;

[UsedImplicitly]
internal class ConfigFile
{
    [DefaultValue(2.0)]
    public string? Version { get; init; }
    public ConfigRoot? Root { get; init; }
    public ConfigLocal? Localizations { get; init; }
}

[UsedImplicitly]
internal class ConfigRoot
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
    public string? IsDlc { get; init; }
    public string? IsUlc { get; init; }
    public string? OptionalAddonEnabledByDefault { get; init; }
}

[UsedImplicitly]
internal class ConfigUplay
{
    public string? GameCode { get; init; }
    public string? AchievementsSyncId { get; init; }
}

[UsedImplicitly]
internal class ConfigStart
{
    public ConfigGame? Online { get; init; }
    public ConfigGame? Offline { get; init; }
    public ConfigGameSteam? Steam { get; init; }
}

internal class ConfigGame
{
    public List<ConfigGameExe>? Executables { get; init; }
}

internal class ConfigGameSteam
{
    public string? SteamAppId { get; init; }
    public string? SteamInstallationStatusRegister { get; init; }
    public string? GameInstallationStatusRegister { get; init; }
}

internal class ConfigGameExe
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

internal class ConfigExe
{
    public string? Register { get; init; }
    public string? Relative { get; init; }
    public string? Arguments { get; init; }
    public string? Append { get; init; }
}

[UsedImplicitly]
internal class ConfigInstall
{
    public string? GameIdentifier { get; init; }
}

[UsedImplicitly]
internal class ConfigThirdParty
{
    public string? Name { get; init; }
}

[UsedImplicitly]
internal class ConfigLocal
{
    public ConfigLocalLang? Default { get; init; }
}

[UsedImplicitly]
internal class ConfigLocalLang
{
    public string? NAME { get; init; }
    public string? GAMENAME { get; init; }
    public string? THUMBIMAGE { get; init; }
    public string? ICONIMAGE { get; init; }
    public string? DESCRIPTION { get; init; }
    public string? l1 { get; init; }
    public string? l2 { get; init; }
    public string? l3 { get; init; }
    public string? l4 { get; init; }
    public string? l5 { get; init; }
}
