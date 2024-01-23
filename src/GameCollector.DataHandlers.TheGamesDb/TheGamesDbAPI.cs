using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GameFinder.Common;
using GameCollector.DataHandlers.TheGamesDb.Properties;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using OneOf;
using TheGamesDBApiWrapper.Data;
using TheGamesDBApiWrapper.Data.ApiClasses;
using TheGamesDBApiWrapper.Data.Track;
using TheGamesDBApiWrapper.Domain;
using TheGamesDBApiWrapper.Domain.Track;
using TheGamesDBApiWrapper.Models.Config;
using TheGamesDBApiWrapper.Models.Entities;
using TheGamesDBApiWrapper.Models.Enums;
using TheGamesDBApiWrapper.Models.Responses.Games;

namespace GameCollector.DataHandlers.TheGamesDb;

public partial class TheGamesDbHandler : AHandler<TheGamesDbGame, TheGamesDbGameId>
{
    private readonly string? _apiKey;
    private readonly IServiceProvider? _provider;
    private ITheGamesDBAPI? _api;
    private int _progress;
    private event PropertyChangedEventHandler? _propertyChanged = null;
    //private PropertyChangedEventArgs _propertyChangedArgs = new("");

    public class Lists
    {
        internal static bool filled = false;
        internal static IReadOnlyDictionary<int, DeveloperModel>? developers;
        internal static IReadOnlyDictionary<int, GenreModel>? genres;
        internal static IReadOnlyDictionary<int, PlatformModel>? platforms;
        internal static IReadOnlyDictionary<int, PublisherModel>? publishers;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <param name="apiKey"></param>
    public TheGamesDbHandler(IFileSystem fileSystem, string? apiKey, ILogger? logger)
    {
        _fileSystem = fileSystem;
        _apiKey = apiKey;
        if (apiKey is not null)
            _provider = ConfigureApiServices();
        _logger = logger;
    }

    public async Task<IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>>> FindAllGamesFromAPI()
    {
        IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>> games;

        games = await FindAllGamesByPlatformFromAPI(Platform.PC, progress => Progress = progress);
        return games;
    }

    private IServiceProvider? ConfigureApiServices()
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger?.LogWarning("An API Key is required for certain features of the TheGamesDbHandler");
            return null;
        }

        var services = new ServiceCollection();
        services.AddTheGamesDBApiWrapper(new TheGamesDBApiConfigModel()
        {
            Version = 1.0, //Indicate the Version to use of the API (Defaults to 1.0)
            ApiKey = _apiKey,  // The API Key to use (either the one lifetime private key or the public key received by the API Team of "TheGamesDb" see "Requirements",
            ForceVersion = false // Indicates if version is forces to use - see Configure Readme Section for more
        });

        var provider = services.BuildServiceProvider();
        services.AddSingleton<IAllowanceTracker, AllowanceTracker>();
        services.AddScoped<ITheGamesDBAPI, TheGamesDBAPI>();
        _api = provider.GetService<ITheGamesDBAPI>();

        if (_api is null)
            _logger?.LogDebug("ITheGamesDBAPI is null");

        return _provider;
    }

    private async Task FillListsFromAPI()
    {
        if (Lists.filled || _api is null)
            return;

        var devResponse = _api.Developers.All().Result;
        if (devResponse.Code == 200)
            Lists.developers = devResponse.Data.Developers;
        else
            _logger?.LogDebug("Developers error {code}: {status}", devResponse.Code, devResponse.Status);
        var genResponse = _api.Genres.All().Result;
        if (genResponse.Code == 200)
            Lists.genres = genResponse.Data.Genres;
        else
            _logger?.LogDebug("Genres error {code}: {status}", genResponse.Code, genResponse.Status);
        var platResponse = _api.Platform.All().Result;
        if (platResponse.Code == 200)
            Lists.platforms = platResponse.Data.Platforms;
        else
            _logger?.LogDebug("Platforms error {code}: {status}", platResponse.Code, platResponse.Status);
        var pubResponse = _api.Publishers.All().Result;
        if (pubResponse.Code == 200)
            Lists.publishers = pubResponse.Data.Publishers;
        else
            _logger?.LogDebug("Publishers error {code}: {status}", pubResponse.Code, pubResponse.Status);

        Lists.filled = true;
    }

    public (int, ErrorMessage?) GetRemainingAPIRequests()
    {
        int remaining;
        DateTime resetDate;

        if (_api?.AllowanceTrack is not null)
        {
            remaining = _api.AllowanceTrack.Remaining;
            resetDate = _api.AllowanceTrack.ResetAt;
            if (remaining > 0)
                return (remaining, null);
            return (remaining, $"No remaining API requests available until {resetDate}");
        }

        return (0, "Could not read TheGamesDb.net API tracker");
    }

    private async Task<Dictionary<string, string>> GetImagesByIdFromAPI(int gameId, Action<int>? progressCallback = null)
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

            var (remaining, trackError) = GetRemainingAPIRequests();
            if (trackError is null)
                _logger?.LogDebug("Requests remaining: {requests}", remaining);

            imageTask = imageResponse.NextPage();
            imageTask.Wait();
            imageResponse = imageTask.Result;
        }

        return images;
    }

    public async Task<IEnumerable<OneOf<int, ErrorMessage>>> FindUpdatedGamesFromAPI(int lastEditId, Action<int>? progressCallback = null)
    {
        List<OneOf<int, ErrorMessage>> updatedGames = new();

        if (_api is null)
            return new List<OneOf<int, ErrorMessage>>() { new ErrorMessage("ITheGamesDBAPI is null") };

        var updateTask = _api.Games.Updates(lastEditId);
        updateTask.Wait();
        var updateResponse = updateTask.Result;
        while (updateResponse.Code == 200)
        {
            foreach (var p in updateResponse.Data.Updates)
            {
                if (lastEditId < p.EditID)
                    lastEditId = p.EditID;
                updatedGames.Add(p.GameID);
            }

            updateTask = updateResponse.NextPage();
            updateTask.Wait();
            updateResponse = updateTask.Result;
        }

        var (remaining, trackError) = GetRemainingAPIRequests();
        if (trackError is not null)
            updatedGames.Add(new ErrorMessage(trackError.ToString() ?? ""));
        _logger?.LogDebug("Requests remaining: {requests}", remaining);

        return updatedGames;
    }

    public async Task<OneOf<TheGamesDbGame, ErrorMessage>> FindOneGameByIdFromAPI(int gameId)
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

        var (remaining, trackError) = GetRemainingAPIRequests();
        if (trackError is null)
            _logger?.LogDebug("Requests remaining: {requests}", remaining);

        return new ErrorMessage($"Game {gameId} not found.");
    }

    public async Task<IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>>> FindAllGamesByPlatformFromAPI(Platform platform = Platform.PC, Action<int>? progressCallback = null)
    {
        IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>> games;

        //_ = DateTime.TryParseExact(releaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, out var dtReleaseDate);
        //return allGames.TryGetValue(id, out var game) ? game : null;

        games = await FindAllGamesByPlatformFromAPI((int)platform, progress => Progress = progress);
        return games;
    }

    public async Task<IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>>> FindAllGamesByPlatformFromAPI(int platformId, Action<int>? progressCallback = null)
    {
        List<GameModel> models = new();
        List<OneOf<TheGamesDbGame, ErrorMessage>> games = new();
        Dictionary<int, GameImageModel[]?>? imageData = new();
        var lastGame = 0;
        var lastEdit = 0;
        var lastTime = DateTime.MinValue;
        var gamesProcessed = 0;

        if (_api is null)
            return new List<OneOf<TheGamesDbGame, ErrorMessage>>() { new ErrorMessage("ITheGamesDBAPI is null") };
        var timeInMin = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60 - 1440; // (less a day)
        _logger?.LogDebug("{time}", timeInMin);
        //return new List<OneOf<TheGamesDbGame, ErrorMessage>>();

        // Get the last edited game id, so we can get a rough percentage for the progress callback
        // TODO: Are there missing numbers, i.e., for deleted entries?
        var lastTask = _api.Games.Updates(-1, timeInMin);
        lastTask.Wait();
        var lastResponse = lastTask.Result;
        foreach (var model in lastResponse.Data.Updates)
        {
            if (model.GameID > lastGame)
            {
                lastGame = model.GameID;
                lastEdit = model.EditID;
                lastTime = model.Timestamp;
            }
        }
        _logger?.LogDebug("Last game: {game} [{edit} @ {time}]", lastGame, lastEdit, lastTime);
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
        
        while (gameResponse.Code == 200)
        {
            models.AddRange(new List<GameModel>(gameResponse.Data.Games));
            gamesProcessed += gameResponse.Data.Count;
            var percentage = Callback(progressCallback, gamesProcessed, lastGame);
            if (percentage < 100)
                _logger?.LogDebug("{progress}%", ((int)percentage).ToString(CultureInfo.InvariantCulture));
            gameTask = gameResponse.NextPage();
            gameTask.Wait();
            gameResponse = gameTask.Result;
        }
        
        var parseTask = ParseGames(models, imageData);
        _logger?.LogDebug("games count: {count}", games.Count);

        var (remaining, trackError) = GetRemainingAPIRequests();
        if (trackError is not null)
            games.Add(new ErrorMessage(trackError.ToString() ?? ""));
        _logger?.LogDebug("Requests remaining: {requests}", remaining);

        return games;
    }

    public async Task<IEnumerable<OneOf<TheGamesDbGame, ErrorMessage>>> FindAllGamesByNameFromAPI(string name, int[] platforms, Action<int>? progressCallback = null)
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

        var (remaining, trackError) = GetRemainingAPIRequests();
        if (trackError is null)
            _logger?.LogDebug("Requests remaining: {requests}", remaining);

        return ParseGames(models, imageData);
    }

    /// <summary>
    /// Returns one game.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="platform"></param>
    /// <returns></returns>
    public async Task<OneOf<TheGamesDbGame, ErrorMessage>> FindOneGameByNameFromAPI(string name, Platform platform = Platform.PC)
    {
        var allGames = await FindAllGamesByPlatformFromAPI(platform, progress => Progress = progress);
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

    /// <summary>
    ///     Invokes the callback with the percentage of games processed.
    /// </summary>
    /// <param name="callback">callback to invoke</param>
    /// <param name="processed">number of games processed</param>
    /// <param name="gameCount">total number of games</param>
    private int? Callback(Action<int>? callback, float processed, int gameCount)
    {
        if (callback is not null)
        {
            var percentage = (int)Math.Round(processed / gameCount * 100);

            callback(percentage);
            return percentage;
        }
        return null;
    }

    /// <summary>
    ///     Game list rebuilding percentage completion.
    /// </summary>
    public int Progress
    {
        get => _progress;
        set
        {
            if (value == _progress) return;
            _progress = value;
            OnPropertyChanged();
        }
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnAllPropertiesChanged()
    {
        _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
    }

    internal List<string> GetDeveloperNamesFromAPI(int[] devs)
    {
        List<string> devNames = new();

        if (devs is null)
            return devNames;

        foreach (var dev in devs)
        {
            devNames.Add(GetDeveloperDescriptionFromAPI(dev) ?? "");
        }

        return devNames;
    }

    internal static List<string> GetGenreNamesFromAPI(int[] genres)
    {
        List<string> genreNames = new();

        if (Lists.genres is null)
            return genreNames;

        foreach (var genre in genres)
        {
            if (Lists.genres.ContainsKey(genre))
                genreNames.Add(Lists.genres[genre].Name);
        }

        return genreNames;
    }

    internal static string? GetPlatformNameFromAPI(int platform)
    {
        if (Lists.platforms is null)
            return null;

        if (Lists.platforms.ContainsKey(platform))
            return Lists.platforms[platform].Name;

        return null;
    }

    internal List<string> GetPublisherNamesFromAPI(int?[] pubs)
    {
        List<string> pubNames = new();

        if (pubs is null)
            return pubNames;

        foreach (var pub in pubs)
        {
            pubNames.Add(GetPublisherDescriptionFromAPI(pub) ?? "");
        }

        return pubNames;
    }

    public string? GetDeveloperDescriptionFromAPI(int dev)
    {
        var devName = GetDeveloperDescriptionsFromAPI(new[] { (uint)dev });
        if (devName is not null)
            return devName.FirstOrDefault();

        return null;
    }

    public IEnumerable<string> GetDeveloperDescriptionsFromAPI(IEnumerable<uint> devList)
    {
        List<string> developers = new();

        if (Lists.developers is null)
            return developers;

        foreach (var dev in devList)
        {
            if (Lists.developers.ContainsKey((int)dev))
                developers.Add(Lists.developers[(int)dev].Name);
            else
                developers.Add(((int)dev).ToString());
        }

        return developers;
    }

    public string? GetPublisherDescriptionFromAPI(int? pub)
    {
        if (pub is null)
            return null;

        var pubName = GetPublisherDescriptionsFromAPI(new[] { (uint?)pub });
        if (pubName is not null)
            return pubName.FirstOrDefault();

        return null;
    }

    public IEnumerable<string> GetPublisherDescriptionsFromAPI(IEnumerable<uint?> pubList)
    {
        List<string> publishers = new();

        if (Lists.publishers is null)
            return publishers;

        foreach (var pub in pubList)
        {
            if (pub is not null)
            {
                if (Lists.publishers.ContainsKey((int)pub))
                    publishers.Add(Lists.publishers[(int)pub].Name);
                else
                    publishers.Add(((int)pub).ToString());
            }
        }

        return publishers;
    }
}
