using System;
using System.Collections.Generic;

namespace GameCollector.EmulatorHandlers.Dolphin;

public enum DolphinPlatform
{
    Unknown = -1,
    GameCube,
    Wii,
}

public class GameList
{
    public string? File { get; set; }
    public string? Gameid { get; set; }
    public string? Title { get; set; }
    public string? ApploaderDate { get; set; }
    public string? Maker { get; set; }
    public string? Description { get; set; }
    public DolphinPlatform Platform { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
}

