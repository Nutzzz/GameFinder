using System.Collections.Generic;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.EADesktop;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[XmlRoot(ElementName = "DiPManifest")]
public class InstallerDataManifest
{
    [property: XmlArray("contentIDs")]
    [property: XmlArrayItem("contentID", Type = typeof(string))]
    public List<string> ContentIds { get; set; } = null!;

    [property: XmlArray("gameTitles")]
    [property: XmlArrayItem("gameTitle", Type = typeof(GameTitle))]
    public List<GameTitle> GameTitles { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GameTitle
{
    [XmlAttribute(AttributeName = "locale")]
    public string TitleLocale { get; set; } = null!;

    [XmlText]
    public string TitleText { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[XmlRoot(ElementName = "game")]
public class InstallerDataGame
{
    [property: XmlArray("contentIDs")]
    [property: XmlArrayItem("contentID", Type = typeof(string))]
    public List<string> ContentIds { get; set; } = null!;

    [XmlElement(ElementName = "metadata")]
    public Metadata Metadata { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Metadata
{
    [XmlElement(ElementName = "localeInfo")]
    public LocaleInfo LocaleInfo { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class LocaleInfo
{
    [XmlAttribute(AttributeName = "locale")]
    public string InfoLocale { get; set; } = null!;

    [XmlElement(ElementName = "title")]
    public string InfoTitle { get; set; } = null!;
}
