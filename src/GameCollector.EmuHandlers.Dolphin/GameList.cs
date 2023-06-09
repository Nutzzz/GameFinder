namespace GameCollector.EmuHandlers.Dolphin;

internal record GameList
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public string? GameId { get; set; }
    public string? File { get; set; }
    public DolphinRegion? Region { get; set; }
    public DolphinSystem? System { get; set; }
    public string? ApploaderDate { get; set; }
}
