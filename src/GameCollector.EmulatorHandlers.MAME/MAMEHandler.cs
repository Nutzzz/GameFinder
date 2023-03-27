using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using GameCollector.Common;
using System.Reflection.PortableExecutable;

namespace GameCollector.EmulatorHandlers.MAME;

// Originally based on https://github.com/mika76/mamesaver
// Copyright (c) 2007 Contributors

/// <summary>
/// Handler for finding MAME ROMs.
/// </summary>
[PublicAPI]
public partial class MAMEHandler : AHandler<Game, string>
{
    /// <summary>
    ///     Number of ROMs to process per batch. ROM files are batched when passing as arguments to MAME both 
    ///     to minimize processing time and to allow a visual indicator that games are being processed.
    /// </summary>
    private const int ROMsPerBatch = 500;

    public enum MachineStatus
    {
        Unknown = -1,
        Preliminary,
        Imperfect,
        Good,
    }

    private static ILogger logger = new NLogLoggerProvider().CreateLogger("MAME");
    private readonly IFileSystem _fileSystem;
    private XmlReaderSettings _readerSettings = new();
    private event PropertyChangedEventHandler? _propertyChanged = null;
    //private PropertyChangedEventArgs _propertyChangedArgs = new("");
    private string _exePath = "";
    private int _progress;

    /// <summary>
    /// Default constructor that uses the real filesystem <see cref="FileSystem"/>.
    /// of <see cref="IFileSystem"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public MAMEHandler() : this(new FileSystem()) { }

    /// <summary>
    /// Constructor for specifying the <see cref="IFileSystem"/> implementation to use.
    /// </summary>
    /// <param name="fileSystem"></param>
    public MAMEHandler(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        return FindAllGames(installedOnly);
    }

    /// <summary>
    /// Finds all ROMs supported by this emulator. The return type <see cref="Result{TGame}"/>
    /// will always be a non-null game or a non-null error message.
    /// </summary>
    /// <param name="availableOnly">ROM file exists (and if doVerify is true, ROM is correct)</param>
    /// <param name="exePath">Path to MAME executable</param>
    /// <param name="parentsOnly">Do not add clones</param>
    /// <param name="minimumStatus">Minimum emulation status (from "good", "imperfect", or "preliminary")</param>
    /// <param name="doVerify">Verify ROM is correct for MAME version (this is very slow)</param>
    /// <returns></returns>
    public IEnumerable<Result<Game>> FindAllGames(
        bool availableOnly,
        string exePath = "",
        bool parentsOnly = false,
        MachineStatus minimumStatus = MachineStatus.Imperfect,
        bool doVerify = true)
    {
        //var commandLineArgs = "-skip_gameinfo -nowindow -noswitchres -sleep -triplebuffer";
        var commandLineArgs = "";
        _readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };

        if (string.IsNullOrEmpty(exePath))
        {
            logger.LogWarning("MAME exePath not specified");
            exePath = @"C:\MAME\mame64.exe";
        }

        if (_fileSystem.Path.GetExtension(exePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            _exePath = exePath;
        else
            _exePath = _fileSystem.Path.Combine(exePath, "mame64.exe");

        if (!_fileSystem.File.Exists(_exePath))
        {
            yield return Result.FromError<Game>($"The file {_exePath} does not exist!");
            yield break;
        }

        foreach (var game in GetGameList(progress => Progress = progress, availableOnly, parentsOnly, minimumStatus, doVerify))
        {
            if (!string.IsNullOrEmpty(game.Name) &&
                game.Machine is not null &&
                !string.IsNullOrEmpty(game.Machine.Description))
            {
                var displayType = "";
                var displayRotation = "";
                var players = "";
                var driverStatus = "";
                var hasProblem = false;
                var gameCategory1 = "";
                var gameCategory2 = "";
                var gameCategory3 = "";
                var isMature = false;
                var verAdded = "";
                var isInstalled = false;
                var isClone = false;

                if (!string.IsNullOrEmpty(game.Path))
                {
                    if (!doVerify)
                        isInstalled = true;
                    else
                    {
                        if (game.IsVerified)
                            isInstalled = true;
                        else if (!string.IsNullOrEmpty(game.Path))
                            hasProblem = true;
                    }
                }

                isClone = !string.IsNullOrEmpty(game.Machine.CloneOf);

                /*
                // these should have already been checked
                if (availableOnly && !isInstalled)
                    continue;
                if (parentsOnly && isClone)
                    continue;
                */

                if (game.Machine.Display is not null)
                {
                    displayType = game.Machine.Display.Type;
                    displayRotation = game.Machine.Display.Rotate;
                }
                if (game.Machine.Input is not null)
                    players = game.Machine.Input.Players;

                if (game.Machine.Driver is not null)
                    driverStatus = game.Machine.Driver.Status;
                else
                    continue; // this should have already been checked

                // minimumStatus was already checked, but HasProblem flag should be set regardless
                if (!string.IsNullOrEmpty(driverStatus) &&
                    ToMachineStatus(driverStatus) < MachineStatus.Imperfect)
                    hasProblem = true;

                if (game.Category is not null)
                {
                    gameCategory1 = game.Category.One;
                    gameCategory2 = game.Category.Two;
                    gameCategory3 = game.Category.Three;
                    isMature = game.Category.Mature;
                    verAdded = game.VerAdded;
                }
                
                yield return Result.FromGame(new Game(
                    Id: game.Name,
                    Name: game.Machine.Description,
                    Path: game.Path,
                    Launch: _exePath,
                    LaunchArgs: $"{game.Name} {commandLineArgs}",
                    IsInstalled: isInstalled,
                    IsClone: isClone,
                    HasProblem: hasProblem,
                    Metadata: new(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ReleaseDate"] = new() { game.Machine.Year ?? "" },
                        ["Manufacturer"] = new() { game.Machine.Manufacturer ?? "" },
                        ["Genres"] = new() { gameCategory1 ?? "Unknown", gameCategory2 ?? "", gameCategory3 ?? "" },
                        ["AgeRating"] = new() { isMature ? "adults_only" : "" },
                        ["Players"] = new() { players ?? "" },
                        ["DriverStatus"] = new() { driverStatus ?? "" },
                        ["DisplayType"] = new() { displayType ?? "" },
                        ["DisplayRotation"] = new() { displayRotation ?? "" },
                        ["VersionAdded"] = new() { verAdded ?? "" },
                        ["Parent"] = new() { game.Machine.CloneOf ?? "" },
                    }
                ));
            }
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game);
    }

    /// <summary>
    ///     Returns a <see cref="List{T}" /> of <see cref="Machine" />s which are read from
    ///     the full list and then merged with the verified ROMs list. The games which are returned
    ///     all have a "good" status on their drivers. This check also eliminates BIOS ROMS.
    /// </summary>
    /// <param name="progressCallback">Callback invoked with percentage complete</param>
    /// <param name="availableOnly">ROM file exists (and if doVerify is true, ROM is correct)</param>
    /// <param name="parentsOnly">Do not add clones</param>
    /// <param name="minimumStatus">Minimum emulation status (good, imperfect, preliminary, unknown)</param>
    /// <param name="doVerify">Verify ROM is correct for MAME version (this is very slow)</param>
    /// <returns>Returns a <see cref="List{T}" /> of <see cref="Machine" />s</returns>
    private List<GameData> GetGameList(Action<int> progressCallback, bool availableOnly, bool parentsOnly, MachineStatus minimumStatus, bool doVerify)
    {
        var gameList = new List<GameData>();

        XmlSerializer serializer = new(typeof(GameList));
        using var stream = MAMERunner.Run(_fileSystem, _exePath, "-listxml").StandardOutput;
        var allGames = (GameList?)serializer.Deserialize(stream);
        if (allGames is null || allGames.Machines is null)
        {
            logger.LogError("Unable to retrieve game list");
            return gameList;
        }

        var build = allGames.Build;
        if (!string.IsNullOrEmpty(build) && build.Contains(' ', StringComparison.Ordinal))
            logger.LogInformation("MAME {build}", build[..build.IndexOf(' ', StringComparison.Ordinal)]);

        var romFiles = GetROMFiles();

        var verifiedGames = GetVerifiedSets(progressCallback, romFiles, availableOnly, doVerify).Keys;

        foreach (var game in allGames.Machines)
        {
            var path = "";
            if (string.IsNullOrEmpty(game.Name))
                continue;
            var name = game.Name;
            var isVerified = false;

            // Perform initial validity checks
            if (!IsGameValid(game, parentsOnly, minimumStatus))
                continue;

            foreach (var romFile in romFiles)
            {
                if (romFile.name.Equals(game.Name, StringComparison.OrdinalIgnoreCase))
                {
                    path = romFile.path;
                    break;
                }
            }

            if (availableOnly && string.IsNullOrEmpty(path))
            {
                //logger.LogInformation("{name} not added to game list because a zip file could not be found", machine.Name);
                continue;
            }
            if (doVerify)
            {
                if (verifiedGames.Contains(game.Name, StringComparer.Ordinal))
                    isVerified = true;
                else if (availableOnly)
                {
                    //logger.LogInformation("{name} not added to game list because its files could not be verified", machine.Name);
                    continue;
                }
            }

            // Enrich game metadata
            gameList.Add(new(name, game, CategoryParser.GetCategory(name, _exePath, _fileSystem), CategoryParser.GetVersion(name, _exePath, _fileSystem), path, isVerified));
        }

        return gameList;
    }

    /// <summary>
    ///     Performs preliminary validation checks against a game, verifying that it's runnable, isn't a BIOS
    ///     or mechanical, and matches whether clones are permitted and whether it has minimum emulation status
    /// </summary>
    /// <param name="machine">Class based on elements from MAME's <c>-listxml</c> output</param>
    /// <param name="parentsOnly">Do not add clones</param>
    /// <param name="minimumStatus">Minimum emulation status (from "good", "imperfect", or "preliminary")</param>
    private static bool IsGameValid(Machine machine, bool parentsOnly, MachineStatus minimumStatus)
    {
        if (!string.IsNullOrEmpty(machine.IsDevice) &&
            machine.IsDevice.Equals("yes", StringComparison.Ordinal))
            return false;

        // Verify that the ROM isn't a BIOS
        if (!string.IsNullOrEmpty(machine.IsBIOS) &&
            machine.IsBIOS.Equals("yes", StringComparison.Ordinal))
        {
            //logger.LogDebug("{name} not added to game list as it is a BIOS", game.Name);
            return false;
        }

        // Verify that the game isn't mechanical
        if (!string.IsNullOrEmpty(machine.IsMechanical) &&
            machine.IsMechanical.Equals("yes", StringComparison.Ordinal))
        {
            //logger.LogDebug("{name} not added to game list as it is mechanical", game.Name);
            return false;
        }

        // Verify that the game is runnable
        if (!string.IsNullOrEmpty(machine.Runnable) &&
            !machine.Runnable.Equals("yes", StringComparison.Ordinal))
        {
            //logger.LogDebug("{name} not added to game list as it is not runnable", game.Name);
            return false;
        }

        // Retrieve and verify driver and emulation status
        if (machine.Driver is null)
        {
            //logger.LogInformation("{name} not added to game list as it has no driver specified", machine.Name);
            return false;
        }

        // Skip games which aren't sufficiently emulated
        var status = machine.Driver.Status;
        if (minimumStatus > MachineStatus.Preliminary &&
            !string.IsNullOrEmpty(status) &&
            ToMachineStatus(status) > minimumStatus)
        {
            //logger.LogInformation("{name} not added to game list because it has a status of {status}", machine.Name, machine.Driver.Status);
            return false;
        }

        // Skip clones if parentsOnly flag
        if (parentsOnly && !string.IsNullOrEmpty(machine.CloneOf))
        {
            //logger.LogInformation("{name} not added to game list as it is a clone of {parent}", machine.Name, machine.CloneOf);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns a <see cref="Dictionary{TKey,TValue}" /> filled with the names of games which are
    ///     verified to work. Only the ones marked as good are returned. The clone names
    ///     are returned in the value of the dictionary while the name is used as the key.
    /// </summary>
    private Dictionary<string, string> GetVerifiedSets(Action<int> progressCallback, List<(string, string)> romFiles, bool availableOnly, bool doVerify)
    {
        var verifiedROMs = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!doVerify)
            return verifiedROMs;

        // Verify each ROM in directory
        var index = 0;
        var filesProcessed = 0;

        List<string> filesToVerify;
        while ((filesToVerify = romFiles.Select(n => n.Item1).ToList().Skip(index).Take(ROMsPerBatch).ToList()).Any())
        {
            var arguments = new List<string> { "-verifyroms" }.Concat(filesToVerify).ToArray();
            using (var stream = MAMERunner.Run(_fileSystem, _exePath, arguments).StandardOutput)
            {
                var output = stream.ReadToEnd();

                var matches = GoodRegex().Matches(output);
                for (var i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];
                    verifiedROMs[match.Groups[1].Value] = match.Groups[2].Value;
                }
            }

            index += ROMsPerBatch;
            filesProcessed += filesToVerify.Count;

            var percentage = Callback(progressCallback, filesProcessed, romFiles.Count);
            if (percentage < 100)
                logger.LogDebug("{progress}%", percentage.ToString(CultureInfo.InvariantCulture));
        }

        return verifiedROMs;
    }

    /// <summary>
    ///     Invokes the callback with the percentage of game list construction completed.
    /// </summary>
    /// <param name="callback">callback to invoke</param>
    /// <param name="processed">number of ROM files processed</param>
    /// <param name="romCount">total number of ROM files</param>
    private int Callback(Action<int> callback, float processed, int romCount)
    {
        var percentage = (int)Math.Round(processed / romCount * 100);
        
        callback(percentage);
        return percentage;
    }

    /// <summary>
    ///     Returns a list of the base name of ROMs in the MAME ROM directories.
    /// </summary>
    /// <remarks>
    ///     It is assumed that ROMs are zipped.
    /// </remarks>
    private List<(string name, string path)> GetROMFiles()
    {
        List<(string name, string path)> roms = new();

        foreach (var path in GetROMPaths())
        {
            logger.LogDebug("rompath: {path}", path);
            List<string> romFiles = new();
            if (_fileSystem.Directory.Exists(path))
            {
                romFiles = _fileSystem.Directory.GetFiles(path, "*.zip").ToList();
                logger.LogDebug("{romcount} ROMs found", romFiles.Count);
                foreach (var romFile in romFiles)
                {
                    roms.Add((name: _fileSystem.Path.GetFileNameWithoutExtension(romFile), path));
                }
            }
        }

        return roms;
    }

    /// <summary>
    ///     Retrieves absolute paths to the ROM directories as configured by MAME.
    /// </summary>
    /// <remarks>
    ///     Multiple ROM directories can be specified in MAME by separating directories by semicolons.
    /// </remarks>
    private List<string> GetROMPaths() => GetConfigPaths("rompath");

    /// <summary>
    ///     Returns the value of a path element in the <c>mame.ini</c> file.
    /// </summary>
    /// <param name="key">key name</param>
    /// <returns>list of absolute paths</returns>
    private List<string> GetConfigPaths(string key)
    {
        using (var stream = MAMERunner.Run(_fileSystem, _exePath, "-showconfig").StandardOutput)
        {
            var line = stream.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                if (line.StartsWith(key, StringComparison.Ordinal))
                    return new(ExtractConfigPaths(key, line));

                line = stream.ReadLine();
            }
        }

        logger.LogError("Unable to retrieve {key} path", key);
        return new();
    }

    /// <summary>
    ///     Emulation status of games which are added to the game list.
    /// </summary>
    private static MachineStatus ToMachineStatus(string status)
    {
        return status switch
        {
            "good" => MachineStatus.Good,
            "imperfect" => MachineStatus.Imperfect,
            "preliminary" => MachineStatus.Preliminary,
            _ => MachineStatus.Unknown,
        };
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

    /// <summary>
    ///     Parses a MAME configuration line into a collection of directories.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="line">Entire configuration line to parse</param>
    /// <returns>Absolute directories extracted from <see cref="line"/>, or <c>null</c> if no match</returns>
    private List<string> ExtractConfigPaths(string key, string line)
    {
        // Extract path string from configuration line
        var directoryRegex = new Regex($@"{key}\s+(.+)");
        var match = directoryRegex.Match(line);

        // Remove any quotation marks around entire path collection and split into individual paths
        var rawPaths = CleansePath(match.Groups[1].Value);
        var paths = rawPaths.Split(';');

        // Construct list of absolute paths
        var absolutePaths = new List<string>();
        foreach (var path in paths.Select(CleansePath))
        {
            // If path is absolute, add raw path
            if (_fileSystem.Path.IsPathRooted(path))
                absolutePaths.Add(path);
            else
            {
                // If path is relative, construct absolute path relative to MAME executable
                var workingDir = _fileSystem.Path.GetDirectoryName(_exePath);
                if (!string.IsNullOrEmpty(workingDir))
                    absolutePaths.Add(_fileSystem.Path.Combine(workingDir, path));
            }
        }

        return absolutePaths;
    }

    /// <summary>
    ///     Cleanses a single path or list of paths, trimming whitespace and removing surrounding quotation marks. 
    /// </summary>
    private string CleansePath(string path)
    {
        path = path.Trim();

        // Remove surrounding quotation marks from path string. These are added by MAME if spaces are 
        // present in any part of the path.
        return QuotedRegex().IsMatch(path) ? QuotedRegex().Match(path).Groups[1].Value : path;
    }

    [GeneratedRegex(@"romset (\w*)(?:\s\[(\w*)\])? is good")]
    private static partial Regex GoodRegex();
    [GeneratedRegex(@"^\""(.*)\""$")]
    private static partial Regex QuotedRegex();
}
