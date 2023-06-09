using System.Xml.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.BigFish;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[XmlRoot(ElementName = "state")]
public class GameState
{
    [property: XmlElement("name")]
    public string? Name { get; set; } = null!;

    [property: XmlElement("executablePath")]
    public string? ExecutablePath { get; set; } = null!;

    [property: XmlElement("productID")]
    public string? ProductId { get; set; } = null!;

    [property: XmlElement("icon")]
    public string? Icon { get; set; } = null!;

    [property: XmlElement("thumbnail")]
    public string? Thumbnail { get; set; } = null!;

    [property: XmlElement("feature")]
    public string? Feature { get; set; } = null!;
}
