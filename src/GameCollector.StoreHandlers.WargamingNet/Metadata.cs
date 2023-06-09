using System.Collections.Generic;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace GameCollector.StoreHandlers.WargamingNet;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[XmlRoot(ElementName = "protocol")]
public class Metadata
{
    [property: XmlAttribute("version")]
    public string? Version { get; set; } = null!;

    [property: XmlElement("predefined_section")]
    public PredefinedSection? PredefinedSection { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PredefinedSection
{
    [property: XmlElement("app_id")]
    public string? AppId { get; set; } = null!;

    [property: XmlElement("name")]
    public string? Name { get; set; } = null!;

    [property: XmlElement("fs_name")]
    public string? FsName { get; set; } = null!;

    [property: XmlElement("shortcut_name")]
    public string? ShortcutName { get; set; } = null!;

    [property: XmlArray("executables")]
    [property: XmlArrayItem("executable", Type = typeof(Executable))]
    public List<Executable> Executables { get; set; } = null!;

    [property: XmlArray("client_types")]
    [property: XmlArrayItem("client_type", Type = typeof(ClientType))]
    public List<ClientType> ClientTypes { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Executable
{
    [property: XmlAttribute("arch")]
    public string? Arch { get; set; } = null!;

    [property: XmlAttribute("emul")]
    public string? Emul { get; set; } = null!;

    [property: XmlText]
    public string? Exe { get; set; } = null!;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ClientType
{
    [property: XmlAttribute("id")]
    public string? Id { get; set; } = null!;
}
