using System;
using System.Collections.Generic;
using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameFinder.StoreHandlers.EGS;

/// <summary>
/// Represents a game installed with the Epic Games Store.
/// </summary>
/// <param name="CatalogItemId"></param>
/// <param name="DisplayName"></param>
/// <param name="InstallLocation"></param>
/// <param name="CloudSaveFolder"></param>
/// <param name="InstallLaunch"></param>
/// <param name="IsInstalled"></param>
/// <param name="MainGame"></param>
/// <param name="ImageTallUrl"></param>
/// <param name="ImageUrl"></param>
/// <param name="Developer"></param>
/// <param name="Namespace"></param>
/// <param name="AppId"></param>
[PublicAPI]
public record EGSGame(EGSGameId CatalogItemId,
                      string DisplayName,
                      AbsolutePath InstallLocation,
                      AbsolutePath CloudSaveFolder = default,
                      AbsolutePath InstallLaunch = default,
                      bool IsInstalled = true,
                      string? MainGame = null,
                      string ImageTallUrl = "",
                      string ImageUrl = "",
                      string Developer = "",
                      string Namespace = "",
                      string AppId = "") :
    GameData(GameId: CatalogItemId.ToString(),
             GameName: DisplayName,
             GamePath: InstallLocation,
             SavePath: CloudSaveFolder,
             Launch: InstallLaunch,
             IsInstalled: IsInstalled,
             BaseGame: MainGame,
             Metadata: new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
             {
                 ["ImageUrl"] = new() { ImageTallUrl },
                 ["ImageWideUrl"] = new() { ImageUrl },
                 ["Developers"] = new() { Developer },
                 ["Namespace"] = new() { Namespace },
                 ["AppId"] = new() { AppId },
             });
