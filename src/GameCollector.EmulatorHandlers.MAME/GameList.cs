using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GameCollector.EmulatorHandlers.MAME;

[Serializable, XmlRoot("mame")]
public class GameList
{
    [XmlAttribute("build")]
    public string? Build { get; set; }

    [XmlElement("machine")]
    public List<Machine>? Machines { get; set; }
}

public class Machine
{
    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlAttribute("isbios")]
    public string? IsBIOS { get; set; }

    [XmlAttribute("isdevice")]
    public string? IsDevice { get; set; }

    [XmlAttribute("ismechanical")]
    public string? IsMechanical { get; set; }

    [XmlAttribute("runnable")]
    public string? Runnable { get; set; }

    [XmlAttribute("cloneof")]
    public string? CloneOf { get; set; }

    [XmlElement("description")]
    public string? Description { get; set; }

    [XmlElement("year")]
    public string? Year { get; set; }

    [XmlElement("manufacturer")]
    public string? Manufacturer { get; set; }

    [XmlElement("display")]
    public Display? Display { get; set; }

    [XmlElement("input")]
    public Input? Input { get; set; }

    [XmlElement("driver")]
    public Driver? Driver { get; set; }
}

public class Display
{
    [XmlAttribute("type")]
    public string? Type { get; set; }

    [XmlAttribute("rotate")]
    public string? Rotate { get; set; }
}

public class Input
{
    [XmlAttribute("players")]
    public string? Players { get; set; }
}

public class Driver
{
    [XmlAttribute("status")]
    public string? Status { get; set; }
}
