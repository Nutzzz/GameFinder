using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using NexusMods.Paths;
using OneOf;
using Steam.Models.SteamCommunity;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using ValveKeyValue;

namespace GameFinder.StoreHandlers.Steam;

public partial class SteamHandler : AHandler<SteamGame, SteamGameId>
{
    internal const string SteamMediaUrl = "http://media.steampowered.com/steamcommunity/public/images/apps/";

    private readonly string? _apiKey;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileSystem">
    /// The implementation of <see cref="IFileSystem"/> to use. For a shared instance use
    /// <see cref="FileSystem.Shared"/>. For tests either use <see cref="InMemoryFileSystem"/>,
    /// a custom implementation or just a mock of the interface.
    /// </param>
    /// <param name="registry">
    /// The implementation of <see cref="IRegistry"/> to use. For a shared instance
    /// use <see cref="WindowsRegistry.Shared"/> on Windows. On Linux use <c>null</c>.
    /// For tests either use <see cref="InMemoryRegistry"/>, a custom implementation or just a mock
    /// of the interface.
    /// </param>
    /// <param name="apiKey"></param>
    public SteamHandler(IFileSystem fileSystem, IRegistry? registry, string? apiKey)
    {
        _fileSystem = fileSystem;
        _registry = registry;
        _apiKey = apiKey;
    }

    internal IEnumerable<OneOf<SteamGame, ErrorMessage>> FindOwnedGamesFromAPI(
        Dictionary<SteamGameId, OneOf<SteamGame, ErrorMessage>> installedGames,
        ulong userId = 0)
    {
        List<OneOf<SteamGame, ErrorMessage>> allGames = new();
        List<OneOf<SteamGame, ErrorMessage>> ownedGames = new();
        foreach (var game in GetOwnedGames(userId))
        {
            ownedGames.Add(game);
        }
        foreach (var game in ownedGames)
        {
            if (game.IsT1)
            {
                allGames.Add(game);
                continue;
            }

            var id = game.AsT0.AppId;
            if (!installedGames.ContainsKey(id))
            {
                allGames.Add(game);
                continue;
            }
            allGames.Add(new SteamGame(
                game.AsT0.AppId,
                game.AsT0.Name,
                installedGames[id].AsT0.Path,
                installedGames[id].AsT0.CloudSavesDirectory,
                installedGames[id].AsT0.DisplayIcon,
                installedGames[id].AsT0.IsInstalled,
                installedGames[id].AsT0.PlaytimeForever,
                game.AsT0.IconUrl));
        }
        return allGames;
    }

    private static AbsolutePath GetAppDataFile(AbsolutePath steamDirectory)
    {
        return steamDirectory
            .CombineUnchecked("config")
            .CombineUnchecked("SteamAppData.vdf");
    }

    private static AbsolutePath GetLoginUsersFile(AbsolutePath steamDirectory)
    {
        return steamDirectory
            .CombineUnchecked("config")
            .CombineUnchecked("loginusers.vdf");
    }

    private List<(ulong userId, uint timeStamp, bool autoLogin)>? ParseLoginUsersFile()
    {
        List<(ulong, uint, bool)> userList = new();
        try
        {
            KVValue autoUser = "";

            var defaultSteamDirs = GetDefaultSteamDirectories(_fileSystem)
                .ToArray();

            var appDataFile = defaultSteamDirs.Select(GetAppDataFile).FirstOrDefault(_fileSystem.FileExists);
            var loginUsersFile = defaultSteamDirs.Select(GetLoginUsersFile).FirstOrDefault(_fileSystem.FileExists);
            if (_registry is not null)
            {
                var steamDir = FindSteamInRegistry(_registry);
                if (steamDir != default)
                {
                    if (appDataFile == default)
                        appDataFile = GetAppDataFile(steamDir);
                    if (loginUsersFile == default)
                        loginUsersFile = GetLoginUsersFile(steamDir);
                }
            }

            using (var stream = _fileSystem.ReadFile(appDataFile))
            {
                var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
                var data = kv.Deserialize(stream, KvSerializerOptions);

                if (data is null) return null;
                if (!data.Name.Equals("SteamAppData", StringComparison.OrdinalIgnoreCase)) return null;

                autoUser = data["AutoLoginUser"];
            }

            using (var stream = _fileSystem.ReadFile(loginUsersFile))
            {
                var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
                var data = kv.Deserialize(stream, KvSerializerOptions);

                if (data is null) return null;
                if (!data.Name.Equals("users", StringComparison.OrdinalIgnoreCase)) return null;

                var users = data.Children.ToList();
                foreach (var user in users)
                {
                    if (!ulong.TryParse(user.Name, out var id))
                        continue;

                    var auto = false;
                    if (user.Value["AccountName"] is not null && (user.Value["AccountName"].ToString() ?? "")
                        .Equals(autoUser.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        auto = true;
                    }
                    _ = uint.TryParse(user.Value["Timestamp"].ToString(), out var time);

                    userList.Add((id, time, auto));
                }
            }
        }
        catch (Exception)
        {
            return null;
        }

        return userList;
    }

    private async Task<OneOf<OwnedGamesResultModel, ErrorMessage>> ParseAPIGames(ulong userId, bool continueOnCapturedContext = false)
    {
        if (userId < 1)
        {
            return new ErrorMessage("Can't get Steam not-installed owned games. A Steam ID was not found. \n" +
                "To find your ID, go to <https://store.steampowered.com/account>.");
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            return new ErrorMessage("Can't get Steam not-installed owned games. An API key must be provided. \n" +
                "To get a key, go to <https://steamcommunity.com/dev/apikey>.");
        }

        try
        {
            SteamWebInterfaceFactory apiFactory = new(_apiKey);

            var userInterface = apiFactory.CreateSteamWebInterface<SteamUser>(new HttpClient());
            var userResponse = await userInterface.GetPlayerSummaryAsync(userId)
                .ConfigureAwait(continueOnCapturedContext);
            //DateTimeOffset? userLastModified = userResponse.LastModified;
            var userData = userResponse.Data;
            var visibility = userData.ProfileVisibility;
            //var profile = userData.ProfileUrl;

            if (visibility != ProfileVisibility.Public)
            {
                return new ErrorMessage("Can't get Steam not-installed owned games. Profile must be public. \n" +
                    "To change this, go to <https://steamcommunity.com/my/edit/settings>.");
            }

            var playerInterface = apiFactory.CreateSteamWebInterface<PlayerService>();
            var ownedGames = await playerInterface.GetOwnedGamesAsync(
                userId,
                includeAppInfo: true,
                includeFreeGames: true)
                .ConfigureAwait(continueOnCapturedContext);
            return ownedGames.Data;
        }
        catch (Exception e)
        {
            return new ErrorMessage(e, "Exception looking for Steam owned games");
        }
    }

    private List<OneOf<SteamGame, ErrorMessage>> GetOwnedGames(ulong userId)
    {
        List<OneOf<SteamGame, ErrorMessage>> games = new();

        if (userId < 1)
        {
            var userList = ParseLoginUsersFile();
            if (userList is not null)
            {
                userId = userList.Find(auto => auto.autoLogin).userId; // Get auto-login user
                if (userId < 1)
                    userId = userList.MaxBy(time => time.timeStamp).userId; // Get most recent user
            }
        }

        if (userId > 0)
        {
            var t = ParseAPIGames(userId);
            t.Wait();
            if (t.Result.IsError())
            {
                games.Add(t.Result.AsError());
                return games;
            }

            foreach (var owned in t.Result.AsT0.OwnedGames)
            {
                if (owned is null)
                    continue;

                games.Add(new SteamGame(
                    AppId: SteamGameId.From((int)owned.AppId),
                    Name: owned.Name,
                    Path: new(),
                    CloudSavesDirectory: null,
                    IsInstalled: false,
                    PlaytimeForever: owned.PlaytimeForever,
                    IconUrl: $"{SteamMediaUrl}{owned.AppId}/{owned.ImgIconUrl}.jpg"));
            }
        }
        return games;
    }
}
