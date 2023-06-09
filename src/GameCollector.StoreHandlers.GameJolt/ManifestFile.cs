namespace GameCollector.StoreHandlers.GameJolt;

internal record ManifestFile(
    int? Version,
    GameInfo? GameInfo,
    LaunchOptions? LaunchOptions
);

internal record GameInfo(
    string? Dir
);

internal record LaunchOptions(
    string? Executable
);
