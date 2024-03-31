using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Text.Json;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using NexusMods.Paths;
using OneOf;

namespace GameCollector.StoreHandlers.GOG;

public partial class GOGHandler : AHandler<GOGGame, GOGGameId>
{
    internal IDictionary<GOGGameId, OneOf<GOGGame, ErrorMessage>> FindGamesFromDatabase(
        Dictionary<GOGGameId, OneOf<GOGGame, ErrorMessage>> regGames, Settings? settings)
    {
        /*
        productId from ProductAuthorizations
        productId, installationPath from InstalledBaseProducts
        productId, images, title from LimitedDetails
        //images = icon from json
        id, gameReleaseKey from PlayTasks
        playTaskId, executablePath, commandLineArgs from PlayTaskLaunchParameters
        limitedDetailsId, releaseDate from Details
        */

        Dictionary<GOGGameId, OneOf<GOGGame, ErrorMessage>> games = new();
        var database = GetDatabaseFile(_fileSystem);

        if (!database.FileExists)
        {
            _ = games.TryAdd(default, new ErrorMessage("GOG database not found."));
            return games;
        }

        var launcherPath = "";

        var localMachine32 = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using (var key = localMachine32.OpenSubKey(@"SOFTWARE\GOG.com\GalaxyClient\paths"))
        {
            if (key != null && key.TryGetString("client", out launcherPath))
            {
                launcherPath = Path.Combine(launcherPath, "GalaxyClient.exe");
                if (!File.Exists(launcherPath))
                    launcherPath = "";
            }
        }

        try
        {
            using SQLiteConnection con = new($"Data Source={database}");
            con.Open();

            var i = 0;

            // Get installed, owned not-installed, and unowned games

            using SQLiteCommand cmdLtdDetails = new($"SELECT links, images, productId, title FROM LimitedDetails;", con);
            using var rdrLtdDetails = cmdLtdDetails.ExecuteReader();
            while (rdrLtdDetails.Read())
            {
                var linksJson = rdrLtdDetails.GetString(0);
                var imagesJson = rdrLtdDetails.GetString(1);
                var iId = rdrLtdDetails.GetInt32(2);
                var name = rdrLtdDetails.GetString(3);

                var gogId = $"gog_{iId.ToString(CultureInfo.InvariantCulture)}";
                var id = GOGGameId.From(iId);

                var imageUrl = "";
                var imageWideUrl = "";
                var iconUrl = "";

                using (var document = JsonDocument.Parse(@linksJson, new() { AllowTrailingCommas = true, }))
                {
                    if (document.RootElement.TryGetProperty("logo", out var logo) && logo.TryGetProperty("href", out var url))
                        imageWideUrl = url.GetString() ?? "";
                    if (document.RootElement.TryGetProperty("boxArtImage", out var boxart) && boxart.TryGetProperty("href", out var url2))
                        imageUrl = url2.GetString() ?? "";
                    if (document.RootElement.TryGetProperty("iconSquare", out var icon) && icon.TryGetProperty("href", out var url3))
                        iconUrl = url3.GetString() ?? "";
                }
                using (var document2 = JsonDocument.Parse(@imagesJson, new() { AllowTrailingCommas = true, }))
                {
                    if (string.IsNullOrEmpty(imageWideUrl) && document2.RootElement.TryGetProperty("logo2x", out var url))
                        imageWideUrl = url.GetString() ?? "";
                }

                using SQLiteCommand cmdBuilds = new("SELECT productId FROM Builds " +
                    $"WHERE productId = {iId.ToString(CultureInfo.InvariantCulture)};", con);
                using var rdrBuilds = cmdBuilds.ExecuteReader();
                while (rdrBuilds.Read())
                {
                    using SQLiteCommand cmdKeys = new("SELECT releaseKey FROM ProductsToReleaseKeys " +
                        $"WHERE gogId = {iId.ToString(CultureInfo.InvariantCulture)};", con);
                    using var rdrKeys = cmdKeys.ExecuteReader();
                    while (rdrKeys.Read())
                    {
                        gogId = rdrKeys.GetString(0);
                        break;
                    }

                    GOGGameId parentId = default;
                    var parent = "";
                    var isDlc = false;
                    ushort myRating = 0;

                    // grab the DLC status and user's rating
                    using (SQLiteCommand cmdPieces = new($"SELECT value FROM GamePieces WHERE releaseKey = '{gogId}';", con))
                    using (var rdrPieces = cmdPieces.ExecuteReader())
                    {
                        while (rdrPieces.Read())
                        {
                            var pieces = rdrPieces.GetString(0);
                            using var document3 = JsonDocument.Parse(@pieces, new() { AllowTrailingCommas = true, });
                            if (document3.RootElement.TryGetProperty("parentGrk", out var jParent))
                            {
                                parent = jParent.GetString() ?? "";
                                if (settings?.BaseOnly == true && !string.IsNullOrEmpty(parent))
                                {
                                    isDlc = true;
                                    break;
                                }
                                if (parent.StartsWith("gog_", StringComparison.Ordinal))
                                    parent = parent[4..];
                                if (!string.IsNullOrEmpty(parent) && long.TryParse(
                                    parent,
                                    NumberStyles.Integer,
                                    CultureInfo.InvariantCulture,
                                    out var lParent))
                                {
                                    parentId = GOGGameId.From(lParent);
                                }
                            }
                            else if (document3.RootElement.TryGetProperty("myRating", out var jRating) && jRating.ValueKind != JsonValueKind.Null)
                                jRating.TryGetUInt16(out myRating);
                        }
                        if (isDlc && settings?.BaseOnly == true)
                        {
                            i++;
                            games.TryAdd(GOGGameId.From(i), new ErrorMessage($"{gogId} is a DLC"));
                            continue;
                        }
                    }

                    var launch = "";
                    var launchParam = "";
                    var exe = "";
                    var path = "";
                    DateTime? installDate = null;
                    var isHidden = false;
                    List<string> tags = new();
                    var lastRun = DateTime.MinValue;
                    DateTime? releaseDate = null;

                    using (SQLiteCommand cmdInstBase = new(
                        "SELECT installationPath, installationDate FROM InstalledBaseProducts " +
                        $"WHERE productId = {iId.ToString(CultureInfo.InvariantCulture)};", con))
                    using (var rdrInstBase = cmdInstBase.ExecuteReader())
                    {
                        while (rdrInstBase.Read())
                        {
                            path = rdrInstBase.GetString(0);
                            if (DateTime.TryParseExact(
                                rdrInstBase.GetString(1),
                                "yyyy-MM-dd HH:mm:ss",
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal,
                                out var dtInstallDate))
                            {
                                installDate = dtInstallDate;
                            }
                            using SQLiteCommand cmdPlayTasks = new(
                                $"SELECT id FROM PlayTasks WHERE gameReleaseKey = '{gogId}';", con);
                            using var rdrPlayTasks = cmdPlayTasks.ExecuteReader();
                            while (rdrPlayTasks.Read())
                            {
                                var task = rdrPlayTasks.GetInt32(0);

                                using SQLiteCommand cmdPlayParams = new(
                                    "SELECT executablePath, commandLineArgs FROM PlayTaskLaunchParameters " +
                                    $"WHERE playTaskId = {task.ToString(CultureInfo.InvariantCulture)};", con);
                                using var rdrPlayParams = cmdPlayParams.ExecuteReader();
                                while (rdrPlayParams.Read())
                                {
                                    // Add installed games
                                    exe = rdrPlayParams.GetString(0);
                                    if (string.IsNullOrEmpty(launcherPath))
                                    {
                                        launch = exe;
                                        launch = rdrPlayParams.GetString(1);
                                    }
                                    else
                                    {
                                        launch = launcherPath;
                                        launchParam = "/command=runGame /gameId=" + iId.ToString(CultureInfo.InvariantCulture) +
                                            " /path=" + "\"" + Path.GetDirectoryName(exe) + "\"";
                                        if (launch.Length + launchParam.Length > 8190)
                                            launchParam = "/command=runGame /gameId=" + iId.ToString(CultureInfo.InvariantCulture);
                                    }

                                    // grab hidden status
                                    using (SQLiteCommand cmdUserProps = new("SELECT isHidden FROM UserReleaseProperties " +
                                        $"WHERE releaseKey = '{gogId}';", con))
                                    using (var rdrUserProps = cmdUserProps.ExecuteReader())
                                    {
                                        while (rdrUserProps.Read())
                                        {
                                            isHidden = rdrUserProps.GetBoolean(0);
                                            break;
                                        }
                                    }

                                    // grab user tags
                                    using (SQLiteCommand cmdUserTags = new(
                                        $"SELECT tag FROM UserReleaseTags WHERE releaseKey = '{gogId}';", con))
                                    using (var rdrUserTags = cmdUserTags.ExecuteReader())
                                    {
                                        while (rdrUserTags.Read())
                                        {
                                            tags.Add(rdrUserTags.GetString(0));
                                        }
                                    }

                                    // grab last run date
                                    using (SQLiteCommand cmdLastPlayed = new("SELECT lastPlayedDate FROM LastPlayedDates " +
                                        $"WHERE gameReleaseKey = '{gogId}';", con))
                                    using (var rdrLastPlayed = cmdLastPlayed.ExecuteReader())
                                    {
                                        while (rdrLastPlayed.Read())
                                        {
                                            if (!rdrLastPlayed.IsDBNull(0))
                                            {
                                                _ = DateTime.TryParseExact(rdrLastPlayed.GetString(0), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out lastRun);
                                                break;
                                            }
                                        }
                                    }

                                    // Details table only applies to installed GOG games
                                    using (SQLiteCommand cmdDetails = new("SELECT releaseDate FROM Details " +
                                        $"WHERE limitedDetailsId = '{iId.ToString(CultureInfo.InvariantCulture)}';", con))
                                    using (var rdrDetails = cmdDetails.ExecuteReader())
                                    {
                                        while (rdrDetails.Read())
                                        {
                                            if (!rdrDetails.IsDBNull(0))
                                            {
                                                if (DateTime.TryParseExact(
                                                    rdrDetails.GetString(0),
                                                    "yyyy-MM-dd'T'HH:mm:ss'+0300'",
                                                    CultureInfo.InvariantCulture,
                                                    DateTimeStyles.AssumeUniversal,
                                                    out var dtReleaseDate))
                                                {
                                                    releaseDate = dtReleaseDate;
                                                }
                                                break;
                                            }
                                        }
                                    }

                                    AbsolutePath launchPath = new();
                                    AbsolutePath exePath = new();
                                    AbsolutePath installPath = new();
                                    if (Path.IsPathRooted(launch))
                                        launchPath = _fileSystem.FromUnsanitizedFullPath(launch);
                                    if (Path.IsPathRooted(path))
                                        installPath = _fileSystem.FromUnsanitizedFullPath(path);
                                    if (Path.IsPathRooted(exe))
                                    {
                                        exePath = _fileSystem.FromUnsanitizedFullPath(exe);
                                        if (installPath == default && !string.IsNullOrEmpty(exePath.Directory))
                                            installPath = _fileSystem.FromUnsanitizedFullPath(exePath.Directory);
                                    }

                                    games.TryAdd(id, new GOGGame(
                                        Id: id,
                                        Name: name,
                                        Path: installPath,
                                        Launch: launchPath,
                                        LaunchParam: launchParam,
                                        Exe: exePath,
                                        InstallDate: installDate,
                                        LastPlayedDate: lastRun,
                                        IsInstalled: true,
                                        IsOwned: true,
                                        IsHidden: isHidden,
                                        Tags: tags,
                                        MyRating: myRating,
                                        ParentId: parentId,
                                        ReleaseDate: releaseDate,
                                        BoxArtUrl: imageUrl,
                                        LogoUrl: imageWideUrl,
                                        IconUrl: iconUrl));
                                }
                                if (isDlc) break;
                            }
                            if (isDlc) break;
                        }
                        if (isDlc) break;
                    }

                    // Add not-installed games
                    if (settings?.InstalledOnly != true && string.IsNullOrEmpty(launch))
                    {
                        games.TryAdd(id, new GOGGame(
                            Id: id,
                            Name: name,
                            Path: new(),
                            LaunchUrl: $"goggalaxy://openGameView/{iId.ToString(CultureInfo.InvariantCulture)}",
                            IsInstalled: false,
                            IsOwned: true,
                            BoxArtUrl: imageUrl,
                            LogoUrl: imageWideUrl,
                            IconUrl: iconUrl));
                        break;
                    }
                }

                // Add unowned games
                if (settings?.OwnedOnly != true)
                {
                    games.TryAdd(id, new GOGGame(
                        Id: id,
                        Name: name,
                        Path: new(),
                        IsInstalled: false,
                        IsOwned: false,
                        BoxArtUrl: imageUrl,
                        LogoUrl: imageWideUrl,
                        IconUrl: iconUrl));
                }
            }

            con.Close();
            return games;
        }
        catch (Exception e)
        {
            games.TryAdd(default, new ErrorMessage(e, "Malformed GOG database output!"));
            return games;
        }
    }

    internal static AbsolutePath GetDatabaseFile(IFileSystem fileSystem)
    {
        return fileSystem.GetKnownPath(KnownPath.CommonApplicationDataDirectory)
            .Combine("GOG.com")
            .Combine("Galaxy")
            .Combine("storage")
            .Combine("galaxy-2.0.db");
    }
}
