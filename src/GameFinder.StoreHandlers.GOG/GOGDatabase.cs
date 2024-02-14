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
    internal IEnumerable<OneOf<GOGGame, ErrorMessage>> FindGamesFromDatabase(bool installedOnly = false, bool baseOnly = false)
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

        List<OneOf<GOGGame, ErrorMessage>> games = new();
        var database = GetDatabaseFile(_fileSystem);

        if (!database.FileExists)
        {
            games.Add(new ErrorMessage("GOG database not found."));
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

            // Get both installed and not-installed games

            using SQLiteCommand cmdBuilds = new("SELECT productId FROM Builds", con);
            using var rdrBuilds = cmdBuilds.ExecuteReader();
            while (rdrBuilds.Read())
            {
                var id = rdrBuilds.GetInt32(0);
                var strID = $"gog_{id.ToString(CultureInfo.InvariantCulture)}";
                using SQLiteCommand cmdKeys = new(
                    $"SELECT releaseKey FROM ProductsToReleaseKeys WHERE gogId = {id.ToString(CultureInfo.InvariantCulture)};", con);
                using var rdrKeys = cmdKeys.ExecuteReader();
                while (rdrKeys.Read())
                {
                    strID = rdrKeys.GetString(0);
                    break;
                }

                using SQLiteCommand cmdLtdDetails = new(
                    $"SELECT links, images, title FROM LimitedDetails WHERE productId = {id.ToString(CultureInfo.InvariantCulture)};", con);
                using var rdrLtdDetails = cmdLtdDetails.ExecuteReader();
                while (rdrLtdDetails.Read())
                {
                    var linksJson = rdrLtdDetails.GetString(0);
                    var imagesJson = rdrLtdDetails.GetString(1);
                    var name = rdrLtdDetails.GetString(2);

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
                    using (var document = JsonDocument.Parse(@imagesJson, new() { AllowTrailingCommas = true, }))
                    {
                        if (string.IsNullOrEmpty(imageWideUrl) && document.RootElement.TryGetProperty("logo2x", out var url))
                            imageWideUrl = url.GetString() ?? "";
                    }

                    var launch = "";
                    var launchParam = "";
                    var exe = "";
                    var path = "";
                    DateTime? installDate = null;
                    var isHidden = false;
                    List<string> tags = new();
                    var lastRun = DateTime.MinValue;
                    ushort myRating = 0;
                    DateTime? releaseDate = null;

                    using (SQLiteCommand cmdInstBase = new(
                        $"SELECT installationPath, installationDate FROM InstalledBaseProducts WHERE productId = {id.ToString(CultureInfo.InvariantCulture)};", con))
                    using (var rdrInstBase = cmdInstBase.ExecuteReader())
                    {
                        while (rdrInstBase.Read())
                        {
                            path = rdrInstBase.GetString(0);
                            if (DateTime.TryParseExact(rdrInstBase.GetString(1), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dtInstallDate))
                                installDate = dtInstallDate;
                            using SQLiteCommand cmdPlayTasks = new($"SELECT id FROM PlayTasks WHERE gameReleaseKey = '{strID}';", con);
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
                                        launchParam = "/command=runGame /gameId=" + id.ToString(CultureInfo.InvariantCulture) + " " + "/path=" + "\"" + Path.GetDirectoryName(exe) + "\"";
                                        if (launch.Length + launchParam.Length > 8190)
                                            launchParam = "/command=runGame /gameId=" + id.ToString(CultureInfo.InvariantCulture);
                                    }

                                    // grab hidden status
                                    using (SQLiteCommand cmdUserProps = new($"SELECT isHidden FROM UserReleaseProperties WHERE releaseKey = '{strID}';", con))
                                    using (var rdrUserProps = cmdUserProps.ExecuteReader())
                                    {
                                        while (rdrUserProps.Read())
                                        {
                                            isHidden = rdrUserProps.GetBoolean(0);
                                            break;
                                        }
                                    }

                                    // grab user tags
                                    using (SQLiteCommand cmdUserTags = new($"SELECT tag FROM UserReleaseTags WHERE releaseKey = '{strID}';", con))
                                    using (var rdrUserTags = cmdUserTags.ExecuteReader())
                                    {
                                        while (rdrUserTags.Read())
                                        {
                                            tags.Add(rdrUserTags.GetString(0));
                                        }
                                    }

                                    // grab last run date
                                    using (SQLiteCommand cmdLastPlayed = new($"SELECT lastPlayedDate FROM LastPlayedDates " +
                                        $"WHERE gameReleaseKey = '{strID}';", con))
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

                                    // grab the user's rating
                                    using (SQLiteCommand cmdPieces = new($"SELECT value FROM GamePieces WHERE releaseKey = '{strID}';", con))
                                    using (var rdrPieces = cmdPieces.ExecuteReader())
                                    {
                                        while (rdrPieces.Read())
                                        {
                                            var pieces = rdrPieces.GetString(0);
                                            using var document2 = JsonDocument.Parse(@pieces, new() { AllowTrailingCommas = true, });
                                            if (document2.RootElement.TryGetProperty("myRating", out var jRating))
                                            {
                                                if (jRating.ValueKind != JsonValueKind.Null)
                                                    jRating.TryGetUInt16(out myRating);
                                                break;
                                            }
                                        }
                                    }

                                    // Details table only applies to installed GOG games
                                    using (SQLiteCommand cmdDetails = new(
                                        $"SELECT releaseDate FROM Details WHERE limitedDetailsId = '{id.ToString(CultureInfo.InvariantCulture)}';", con))
                                    using (var rdrDetails = cmdDetails.ExecuteReader())
                                    {
                                        while (rdrDetails.Read())
                                        {
                                            if (!rdrDetails.IsDBNull(0))
                                            {
                                                if (DateTime.TryParseExact(rdrDetails.GetString(0), "yyyy-MM-dd'T'HH:mm:ss'+0300'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dtReleaseDate))
                                                    releaseDate = dtReleaseDate;
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

                                    games.Add(new GOGGame(
                                        Id: GOGGameId.From(id),
                                        Name: name,
                                        Path: installPath,
                                        Launch: launchPath,
                                        LaunchParam: launchParam,
                                        Exe: exePath,
                                        InstallDate: installDate,
                                        LastPlayedDate: lastRun,
                                        IsInstalled: true,
                                        IsHidden: isHidden,
                                        Tags: tags,
                                        MyRating: myRating,
                                        ReleaseDate: releaseDate,
                                        BoxArtUrl: imageUrl,
                                        LogoUrl: imageWideUrl,
                                        IconUrl: iconUrl));
                                }
                            }
                        }
                    }

                    // Add not-installed games
                    if (!installedOnly && string.IsNullOrEmpty(launch))
                    {
                        games.Add(new GOGGame(
                            Id: GOGGameId.From(id),
                            Name: name,
                            Path: new(),
                            LaunchUrl: $"goggalaxy://openGameView/{id.ToString(CultureInfo.InvariantCulture)}",
                            IsInstalled: false,
                            BoxArtUrl: imageUrl,
                            LogoUrl: imageWideUrl,
                            IconUrl: iconUrl));
                    }
                }
            }
            con.Close();
            return games;
        }
        catch (Exception e)
        {
            games.Add(new ErrorMessage(e, "Malformed GOG database output!"));
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
