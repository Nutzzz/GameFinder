using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GameCollector.EmuHandlers.MAME;

[Serializable, XmlRoot("mame")]
public class GameList
{
    [XmlAttribute("build")]
    public string? Build { get; set; } = null!;

    [XmlElement("machine")]
    public List<Machine>? Machines { get; set; } = null!;
}

public class Machine
{
    [XmlAttribute("name")]
    public string? Name { get; set; } = null!;

    [XmlAttribute("isbios")]
    public string? IsBIOS { get; set; } = null!;

    [XmlAttribute("isdevice")]
    public string? IsDevice { get; set; } = null!;

    [XmlAttribute("ismechanical")]
    public string? IsMechanical { get; set; } = null!;

    [XmlAttribute("runnable")]
    public string? Runnable { get; set; } = null!;

    [XmlAttribute("cloneof")]
    public string? CloneOf { get; set; } = null!;

    [XmlElement("description")]
    public string? Description { get; set; } = null!;

    [XmlElement("year")]
    public string? Year { get; set; } = null!;

    [XmlElement("manufacturer")]
    public string? Manufacturer { get; set; } = null!;

    [XmlElement("display")]
    public Display? Display { get; set; } = null!;

    [XmlElement("input")]
    public Input? Input { get; set; } = null!;

    [XmlElement("driver")]
    public Driver? Driver { get; set; } = null!;
}

public class Display
{
    [XmlAttribute("type")]
    public string? Type { get; set; } = null!;

    [XmlAttribute("rotate")]
    public string? Rotate { get; set; } = null!;
}

public class Input
{
    [XmlAttribute("players")]
    public string? Players { get; set; } = null!;
}

public class Driver
{
    [XmlAttribute("status")]
    public string? Status { get; set; } = null!;
}
