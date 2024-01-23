using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GameFinder.Common;
using GameCollector.DataHandlers.TheGamesDb.Properties;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using OneOf;
using TheGamesDBApiWrapper.Data.ApiClasses;
using TheGamesDBApiWrapper.Models.Entities;
using TheGamesDBApiWrapper.Models.Enums;
using TheGamesDBApiWrapper.Models.Responses.Games;
using FluentResults;
using TheGamesDBApiWrapper.Domain.ApiClasses;

namespace GameCollector.DataHandlers.TheGamesDb;

/// <summary>
/// Handler for finding game metadata from TheGamesDb.net.
/// </summary>
[PublicAPI]
public partial class TheGamesDbHandler : AHandler<TheGamesDbGame, TheGamesDbGameId>
{
    // SQL from TGDB http://cdn.thegamesdb.net/tgdb_dump.zip
    // Json from TGDB https://cdn.thegamesdb.net/json/database-latest.json

    private const string EDIT_URL = "https://github.com/Nutzzz/GameCollector/releases/download/TheGamesDb_database/last_edit_id.txt";
    private const string DB_URL = "https://github.com/Nutzzz/GameCollector/releases/download/TheGamesDb_database/database-latest.zip";
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private static int _lastEditId;

    /*
    public class Lists
    {
        private static bool filled = false;
        private static IReadOnlyDictionary<int, DeveloperModel>? developers;
        private static IReadOnlyDictionary<int, GenreModel>? genres;
        private static IReadOnlyDictionary<int, PlatformModel>? platforms;
        private static IReadOnlyDictionary<int, PublisherModel>? publishers;
    }
    */

    private readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.Strict,
            TypeInfoResolver = SourceGenerationContext.Default,
    };

    private static HttpClient Client = new HttpClient();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileSystem"></param>
    public TheGamesDbHandler(IFileSystem fileSystem, ILogger? logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Func<TheGamesDbGame, TheGamesDbGameId> IdSelector => game => game.Id;

    /// <inheritdoc/>
    public override IEqualityComparer<TheGamesDbGameId>? IdEqualityComparer => null;

    /// <inheritdoc/>
    public override AbsolutePath FindClient()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>> FindAllGames(bool installedOnly = false, bool baseOnly = false)
    {
        var taskDb = LoadDatabaseFile();
        taskDb.Wait();

        //var gamesTask = FindAllGamesByPlatform(Platform.PC, progress => Progress = progress);
        //gamesTask.Wait();
        //return gamesTask.Result.ToList();
        return new List<OneOf<TheGamesDbGame, ErrorMessage>>();
    }

    private async Task<Dictionary<string, string>> GetImagesById(int gameId, Action<int>? progressCallback = null)
    {
        Dictionary<string, string> images = new(StringComparer.OrdinalIgnoreCase);

        if (_api is null)
            return images;

        var imageTask = _api.Games.Images(gameId);
        imageTask.Wait();
        var imageResponse = imageTask.Result;
        while (imageResponse.Code == 200)
        {
            foreach (var image in imageResponse.Data.Images[gameId])
            {
                var type = image.Type;
                if (type == GameImageType.Boxart && image.Side.Equals("back", StringComparison.OrdinalIgnoreCase))
                    images.Add("boxartback", image.FileName);
                else
                    images.Add(image.Type.ToString(), image.FileName);
            }

            imageTask = imageResponse.NextPage();
            imageTask.Wait();
            imageResponse = imageTask.Result;
        }

        return images;
    }

    private OneOf<TheGamesDbGame, ErrorMessage> ParseOneGame(GameModel? gameModel, List<GameImageModel>? imageModel)
    {
        if (gameModel is not null)
        {
            var games = ParseGames(new() { gameModel }, new() { [gameModel.Id] = imageModel?.ToArray() });
            if (games.Any())
                return games.FirstOrDefault();
            return new ErrorMessage($"{gameModel.GameTitle} could not be parsed.");
        }
        return new ErrorMessage("GameModel is null.");
    }

    private IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>> ParseGames(List<GameModel> gameModels, Dictionary<int, GameImageModel[]?>? imageModels)
    {
        List<OneOf<TheGamesDbGame, ErrorMessage>> games = new();

        foreach (var game in gameModels)
        {
            string? banner = null;
            string? boxart = null;
            string? boxartBack = null;
            string? clearlogo = null;
            string? fanart = null;
            string? screenshot = null;
            string? titlescreen = null;
            if (imageModels is not null && imageModels.ContainsKey(game.Id))
            {
                var images = imageModels[game.Id];
                if (images is not null)
                {
                    foreach (var image in images)
                    {
                        switch (image.Type.ToString().ToLower(CultureInfo.InvariantCulture))
                        {
                            case "banner":
                                banner ??= image.FileName;
                                break;
                            case "boxart":
                                if (image.Side.Equals("back", StringComparison.OrdinalIgnoreCase))
                                    boxartBack ??= image.FileName;
                                else
                                    boxart ??= image.FileName;
                                break;
                            case "clearlogo":
                                clearlogo ??= image.FileName;
                                break;
                            case "fanart":
                                fanart ??= image.FileName;
                                break;
                            case "screenshot":
                                screenshot ??= image.FileName;
                                break;
                            case "titlescreen":
                                titlescreen ??= image.FileName;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            games.Add(new TheGamesDbGame(
                Id: TheGamesDbGameId.From((ulong)game.Id), GameTitle: game.GameTitle, ReleaseDate: game.ReleaseDate,
                PlatformId: game.Platform, PlatformName: GetPlatformName(game.Platform ?? -1), Overview: game.Overview,
                Youtube: game.YouTube, Players: (ushort?)game.Players, Coop: game.Coop, Rating: game.Rating,
                Developers: GetDeveloperNames(game.Developers), Genres: GetGenreNames(game.Genres), Publishers: GetPublisherNames(game.Publishers),
                Alternates: new List<string>(game.Alternates), BannerUrl: banner, BoxartUrl: boxart, BoxartBackUrl: boxartBack,
                ClearlogoUrl: clearlogo, FanartUrl: fanart, ScreenshotUrl: screenshot, TitlescreenUrl: titlescreen, HDD: game.HDD,
                OS: game.OS, Processor: game.Processor, RAM: game.RAM, Sound: game.Sound, Video: game.Video));
        }

        return games;
    }

    public async Task<OneOf<TheGamesDbGame, ErrorMessage>> FindOneGameById(int gameId)
    {
        GameModel? model = new();
        List<GameImageModel>? images = new();

        if (_api is null)
            return new ErrorMessage("ITheGamesDBAPI is null");

        var gameTask = _api.Games.ByGameID(gameId);
        gameTask.Wait();
        var gameResponse = gameTask.Result;
        if (gameResponse.Code == 200)
        {
            model = gameResponse.Data.Games.FirstOrDefault();
            images = gameResponse.Include.BoxArt.Data[gameId].ToList();
            return ParseOneGame(model, images);
        }

        return new ErrorMessage($"Game {gameId} not found.");
    }

    public async Task<IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>>> FindAllGamesByPlatform(
        Platform platform = Platform.PC,
        CancellationToken cancelToken = default,
        Action<int>? progressCallback = null)
    {
        IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>> games;

        //_ = DateTime.TryParseExact(releaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, out var dtReleaseDate);
        //return allGames.TryGetValue(id, out var game) ? game : null;

        games = await FindAllGamesByPlatform((int)platform, cancelToken, progress => Progress = progress);
        return games;
    }

    public async Task<IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>>> FindAllGamesByPlatform(
        int platformId,
        CancellationToken cancelToken = default,
        Action<int>? progressCallback = null)
    {
        List<GameModel> models = new();
        List<OneOf<TheGamesDbGame, ErrorMessage>> games = new();
        Dictionary<int, GameImageModel[]?>? imageData = new();
        var lastGame = 0;
        var lastTime = DateTime.MinValue;

        if (_api is null)
            return new List<OneOf<TheGamesDbGame, ErrorMessage>>() { new ErrorMessage("ITheGamesDBAPI is null") };

        var taskDb = LoadDatabaseFile(cancelToken);
        taskDb.Wait();

        _logger?.LogDebug("Last game: {game} [{edit} @ {time}]", lastGame, _lastEditId, lastTime);
        var gameTask = _api.Games.ByPlatformID(platformId);
        gameTask.Wait();
        var gameResponse = gameTask.Result;
        if (gameResponse.Code != 200)
            _logger?.LogDebug("Games error {code}: {status}", gameResponse.Code, gameResponse.Status);
        /*
        else
        {
            imageData = gameResponse.Include.BoxArt.Data;
            _logger?.LogDebug("imageData count: {count}", imageData.Count);
        }
        */

        games = ParseGames(models, imageData).ToList();
        _logger?.LogDebug("games count: {count}", games.Count);

        return games;
    }

    public async Task<IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>>> FindAllGamesByName(string name, int[] platforms, Action<int>? progressCallback = null)
    {
        List<GameModel> models = new();

        if (_api is null)
            return new List<OneOf<TheGamesDbGame, ErrorMessage>>() { new ErrorMessage("ITheGamesDBAPI is null") };
        var gameTask = _api.Games.ByGameName(name, 1, platforms);
        gameTask.Wait();
        var gameResponse = gameTask.Result;

        var imageData = gameResponse.Include.BoxArt.Data;
        while (gameResponse.Code == 200)
        {
            models.AddRange(new List<GameModel>(gameResponse.Data.Games));

            gameTask = gameResponse.NextPage();
            gameTask.Wait();
            gameResponse = gameTask.Result;
        }

        return ParseGames(models, imageData);
    }

    /// <summary>
    /// Returns one game.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="platform"></param>
    /// <returns></returns>
    public async Task<OneOf<TheGamesDbGame, ErrorMessage>> FindOneGameByName(string name, Platform platform = Platform.PC)
    {
        var allGames = await FindAllGamesByPlatform(platform, CancellationToken.None, progress => Progress = progress);
        foreach (var game in allGames)
        {
            var title = game.AsGame().GameTitle;
            if (title is not null)
            {
                if (title.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    // check if the title ends with a parenthetical (e.g., a year) and remove it
                    title.Contains('(', StringComparison.Ordinal) &&
                    title[..title.IndexOf('(', StringComparison.Ordinal)].TrimEnd().Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return game;
                }
            }
        }
        return new ErrorMessage($"{name} not found in TheGamesDb.net database.");
    }

    private async Task LoadDatabaseFile(CancellationToken cancelToken = default)
    {
        HttpClientHandler handler = new() { AllowAutoRedirect = true };
        ProgressMessageHandler progress = new(handler);
        var lastProgressAt = DateTime.Now;

        progress.HttpReceiveProgress += (_, args) =>
        {
            if (args.TotalBytes is not null && args.TotalBytes > 0)
            {
                var percentage = (int)Math.Round(args.BytesTransferred / (decimal)args.TotalBytes * 100);
                if (percentage < 100 && lastProgressAt.AddSeconds(2) < DateTime.Now)
                {
                    _logger?.LogDebug("TheGamesDb.net database download: {percent}%", percentage);
                    lastProgressAt = DateTime.Now;
                }
            }
        };

        var dbPathUser = _fileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("database-latest.json");
        if (_fileSystem.FileExists(dbPathUser))
        {
            _logger?.LogDebug("{chars}", new string(ReadChars(dbPathUser.GetFullPath(), 256)));
            _lastEditId = GetLastEditId(new string(ReadChars(dbPathUser.GetFullPath(), 256)));
            _logger?.LogDebug("Current Last Edit: {current}", _lastEditId);
        }
        try
        {
            var newLastEdit = 0;
            //if (_lastEditId > 0)
            {
                using var editIdTask = Client.GetStringAsync(EDIT_URL, cancelToken);
                editIdTask.Wait(cancelToken);
                _ = int.TryParse(editIdTask.Result, out newLastEdit);
                _logger?.LogDebug("New Last Edit: {new}", newLastEdit);
            }
            /*
            if (_lastEditId < 1 || newLastEdit > _lastEditId)
            {
                var stream = await Client.GetStreamAsync(DB_URL, cancelToken);
                using FileStream fileStream = new("database-new.json", FileMode.Create);
                var buffer = Array.Empty<byte>();
                stream.CopyTo(fileStream);
            }
            */
        }
        catch (WebException e) when (e.Status == WebExceptionStatus.RequestCanceled) { }
        catch (Exception) { }
    }

    private static int GetLastEditId(string? block)
    {
        if (block is null)
            return 0;

        if (block.Contains("last_edit_id", StringComparison.Ordinal))
        {
            var snip = block[(block.IndexOf("last_edit_id", StringComparison.Ordinal) + 14)..];
            if (snip.Contains(',', StringComparison.Ordinal))
            {
                if (int.TryParse(snip[..snip.IndexOf(',', StringComparison.Ordinal)],
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out var lastEdit))

                    return lastEdit;
            }
        }
        return 0;
    }

    public static char[] ReadChars(string filename, int count)
    {
        using var stream = File.OpenRead(filename);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var buffer = new char[count];
        var n = reader.ReadBlock(buffer, 0, count);

        var result = new char[n];

        Array.Copy(buffer, result, n);

        return result;
    }

    internal List<string> GetDeveloperNames(int[] devs)
    {
        List<string> devNames = new();

        if (devs is null)
            return devNames;

        foreach (var dev in devs)
        {
            devNames.Add(GetDeveloperDescription(dev) ?? "");
        }

        return devNames;
    }

    internal static List<string> GetGenreNames(int[] genres)
    {
        List<string> genreNames = new();

        if (genres is null)
            return genreNames;

        foreach (var genre in genres)
        {
            genreNames.Add(GetEnumDescription((Genre)genre));
        }

        return genreNames;
    }

    internal static string? GetPlatformName(int platform)
    {
        return GetEnumDescription((Platform)platform);
    }

    internal List<string> GetPublisherNames(int?[] pubs)
    {
        List<string> pubNames = new();

        if (pubs is null)
            return pubNames;

        foreach (var pub in pubs)
        {
            pubNames.Add(GetPublisherDescription(pub) ?? "");
        }

        return pubNames;
    }

    public static string GetEnumDescription(Enum value)
    {
        return
            value
                .GetType()
                .GetMember(value.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<DescriptionAttribute>()
                ?.Description
            ?? value.ToString();
    }

    public string? GetDeveloperDescription(int dev)
    {
        var devName = GetDeveloperDescriptions(new[] { dev });
        if (devName is not null)
            return devName.FirstOrDefault();

        return null;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(JsonSerializerOptions)} uses {nameof(SourceGenerationContext)} for type information.")]
    public IEnumerable<string> GetDeveloperDescriptions(IEnumerable<int> devList)
    {
        List<string> developers = new();

        if (_api is not null)
        {
            var devTask = _api.Developers.All();
            devTask.Wait();
            var devResponse = devTask.Result;
            if (devResponse.Code == 200)
            {
                foreach (var dev in devList)
                {
                    devResponse.Data.Developers.TryGetValue(dev, out var devOut);
                    if (devOut is not null)
                        developers.Add(devOut.Name);
                }
                //return developers;
            }
        }

        /*
        if (Resources.tgdb_developers is null)
        {
            return developers;
        }

        try
        {
            var devData = JsonSerializer.Deserialize<Companies>(Resources.tgdb_developers, JsonSerializerOptions);
            if (devData is null || devData.Data is null || devData.Data.Developers is null)
                return developers;

            foreach (var dev in devList)
            {
                if (devData.Data.Developers.ContainsKey(dev))
                    developers.Add(devData.Data.Developers[dev].Name ?? dev.ToString());
            }
        }
        catch (Exception) { }
        */

        return developers;
    }

    public string? GetPublisherDescription(int? pub)
    {
        if (pub is null)
            return null;

        var pubName = GetPublisherDescriptions(new[] { (int?)pub });
        if (pubName is not null)
            return pubName.FirstOrDefault();

        return null;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code",
        Justification = $"{nameof(JsonSerializerOptions)} uses {nameof(SourceGenerationContext)} for type information.")]
    public IEnumerable<string> GetPublisherDescriptions(IEnumerable<int?> pubList)
    {
        List<string> publishers = new();

        if (_api is not null)
        {
            var pubTask = _api.Publishers.All();
            pubTask.Wait();
            var pubResponse = pubTask.Result;
            if (pubResponse.Code == 200)
            {
                foreach (var pub in pubList)
                {
                    if (pub is not null)
                    {
                        pubResponse.Data.Publishers.TryGetValue((int)pub, out var pubOut);
                        if (pubOut is not null)
                            publishers.Add(pubOut.Name);
                    }
                }
                //return publishers;
            }
        }

        /*
        if (Resources.tgdb_publishers is null)
            return publishers;

        try
        {
            var pubData = JsonSerializer.Deserialize<Companies>(Resources.tgdb_publishers, JsonSerializerOptions);
            if (pubData is null || pubData.Data is null || pubData.Data.Publishers is null)
                return publishers;

            foreach (var pub in pubList)
            {
                if (pub is null)
                    continue;
                if (pubData.Data.Publishers.ContainsKey((uint)pub))
                    publishers.Add(pubData.Data.Publishers[(uint)pub].Name ?? ((uint)pub).ToString());
            }
        }
        catch (Exception) { }
        */

        return publishers;
    }
}
