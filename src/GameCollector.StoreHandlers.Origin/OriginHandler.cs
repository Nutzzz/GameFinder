using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using GameCollector.Common;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace GameCollector.StoreHandlers.Origin;

/// <summary>
/// Represents a game installed with Origin.
/// </summary>
/// <param name="Id"></param>
/// <param name="InstallPath"></param>
[PublicAPI]
public record OriginGame(string Id, AbsolutePath InstallPath);

/// <summary>
/// Handler for finding games install with Origin.
/// </summary>
[PublicAPI]
public class OriginHandler : AHandler<Game, string>
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileSystem"></param>
    public OriginHandler(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    internal static AbsolutePath GetManifestDir(IFileSystem fileSystem)
    {
        return fileSystem.GetKnownPath(KnownPath.CommonApplicationDataDirectory)
            .CombineUnchecked("Origin")
            .CombineUnchecked("LocalContent");
    }

    /// <inheritdoc/>
    public override IEnumerable<Result<Game>> FindAllGames(bool installedOnly = false)
    {
        var manifestDir = GetManifestDir(_fileSystem);

        if (!_fileSystem.DirectoryExists(manifestDir))
        {
            yield return Result.FromError<Game>($"Manifest folder {manifestDir} does not exist!");
            yield break;
        }

        var mfstFiles = _fileSystem.EnumerateFiles(manifestDir, "*.mfst").ToList();
        if (mfstFiles.Count == 0)
        {
            yield return Result.FromError<Game>($"Manifest folder {manifestDir} does not contain any .mfst files");
            yield break;
        }

        foreach (var mfstFile in mfstFiles)
        {
            var (game, error) = ParseMfstFile(_fileSystem, mfstFile);
            if (error is not null)
            {
                yield return Result.FromError<Game>(error);
                continue;
            }

            // ignored game
            if (game is null) continue;
            yield return Result.FromGame(game);
        }
    }

    /// <inheritdoc/>
    public override IDictionary<string, Game> FindAllGamesById(out string[] errors)
    {
        var (games, allErrors) = FindAllGames().SplitResults();
        errors = allErrors;

        return games.CustomToDictionary(game => game.Id, game => game, StringComparer.OrdinalIgnoreCase);
    }

    private Result<Game> ParseMfstFile(AbsolutePath filePath)
    {
        try
        {
            using var stream = _fileSystem.ReadFile(filePath);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var contents = reader.ReadToEnd();

            var query = HttpUtility.ParseQueryString(contents, Encoding.UTF8);

            // using GetValues because some manifest have duplicate key-value entries for whatever reason
            var ids = query.GetValues("id");
            if (ids is null || ids.Length == 0)
            {
                return Result.FromError<Game>($"Manifest {filePath} does not have a value \"id\"");
            }

            var id = ids[0];
            if (id.EndsWith("@steam", StringComparison.OrdinalIgnoreCase))
                return new Result<Game>();

            var installPaths = query.GetValues("dipInstallPath");
            if (installPaths is null || installPaths.Length == 0)
            {
                return Result.FromError<Game>($"Manifest {filePath} does not have a value \"dipInstallPath\"");
            }

            var path = installPaths
                .OrderByDescending(x => x.Length)
                .First();
            
            return Result.FromGame(new Game(
                Id: id,
                Name: fileSystem.Path.GetFileName(installPaths[0]),
                Path: _fileSystem.FromFullPath(path)));
        }
        catch (Exception e)
        {
            return Result.FromException<Game>($"Exception while parsing {filePath}", e);
        }
    }
}
