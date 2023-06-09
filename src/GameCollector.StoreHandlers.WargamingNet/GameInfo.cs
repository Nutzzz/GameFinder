using System.Collections.Generic;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.WargamingNet;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[XmlRoot(ElementName = "protocol")]
public class GameInfo
{
    [property: XmlAttribute("version")]
    public string? Version { get; set; } = null!;

    [property: XmlElement("game")]
    public Game? Game { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Game
{
    [property: XmlElement("id")]
    public string? Id { get; set; } = null!;

    [property: XmlElement("installed")]
    public string? Installed { get; set; } = null!;

    [property: XmlElement("client_type")]
    public string? ClientType { get; set; } = null!;
}
