using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using FluentResults;
using GameFinder.Common;
using JetBrains.Annotations;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace GameFinder.StoreHandlers.Xbox;

/// <summary>
/// Handler for finding games installed with Xbox Game Pass.
/// </summary>
[PublicAPI]
[RequiresUnreferencedCode($"Calls System.Xml.Serialization.XmlSerializer.Deserialize(XmlReader) with {nameof(Package)}. Make sure {nameof(Package)} is preserved by using TrimmerRootDescriptor or TrimmerRootAssembly!")]
public class XboxHandler : AHandler<XboxGame, XboxGameId>
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileSystem">
    /// The implementation of <see cref="IFileSystem"/> to use. For a shared instance use
    /// <see cref="FileSystem.Shared"/>. For tests either use <see cref="InMemoryFileSystem"/>,
    /// a custom implementation or just a mock of the interface.
    /// </param>
    public XboxHandler(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public override Func<XboxGame, XboxGameId> IdSelector => game => game.Id;

    /// <inheritdoc/>
    public override IEqualityComparer<XboxGameId> IdEqualityComparer => XboxGameIdComparer.Default;

    /// <inheritdoc/>
    public override IEnumerable<Result<XboxGame>> FindAllGames()
    {
        var (paths, errors) = GetAppFolders(_fileSystem);
        foreach (var error in errors)
        {
            yield return Result.Fail(error);
        }

        if (paths.Count == 0)
        {
            yield return Result.Fail("Unable to find any app folders!");
        }

        foreach (var path in paths)
        {
            if (!_fileSystem.DirectoryExists(path)) continue;
            var directories = _fileSystem
                .EnumerateDirectories(path, recursive: false)
                .ToArray();

            if (directories.Length == 0)
            {
                yield return Result.Fail($"App folder {path} does not contain any sub directories!");
                continue;
            }

            foreach (var directory in directories)
            {
                var appManifestFilePath = directory.Combine("appxmanifest.xml");
                if (!_fileSystem.FileExists(appManifestFilePath))
                {
                    var contentDirectory = directory.Combine("Content");
                    if (_fileSystem.DirectoryExists(contentDirectory))
                    {
                        appManifestFilePath = contentDirectory.Combine("appxmanifest.xml");
                        if (!_fileSystem.FileExists(appManifestFilePath))
                        {
                            yield return Result.Fail($"Manifest file does not exist at {appManifestFilePath}");
                            continue;
                        }
                    }
                    else
                    {
                        yield return Result.Fail($"Manifest file does not exist at {appManifestFilePath} and there is no Content folder at {contentDirectory}");
                        continue;
                    }
                }

                var result = ParseAppManifest(_fileSystem, appManifestFilePath);
                if (result.TryGetGame(out var game))
                {
                    yield return Result.Ok(game);
                }
                else
                {
                    yield return Result.Fail(result.AsErrors());
                }
            }
        }
    }

    internal static (List<AbsolutePath> paths, List<IError> errors) GetAppFolders(IFileSystem fileSystem)
    {
        var paths = new List<AbsolutePath>();
        var errors = new List<IError>();

        foreach (var rootDirectory in fileSystem.EnumerateRootDirectories())
        {
            if (!fileSystem.DirectoryExists(rootDirectory)) continue;

            var modifiableWindowsAppsPath = rootDirectory
                .Combine("Program Files")
                .Combine("ModifiableWindowsApps");

            var gamingRootFilePath = rootDirectory.Combine(".GamingRoot");

            var modifiableWindowsAppsDirectoryExists = fileSystem.DirectoryExists(modifiableWindowsAppsPath);
            var gamingRootFileExists = fileSystem.FileExists(gamingRootFilePath);

            if (modifiableWindowsAppsDirectoryExists) paths.Add(modifiableWindowsAppsPath);

            if (!modifiableWindowsAppsDirectoryExists && !gamingRootFileExists)
            {
                errors.Add(new Error($"Neither {modifiableWindowsAppsPath} nor {gamingRootFilePath} exist on the current drive."));
                continue;
            }

            if (!gamingRootFileExists) continue;

            var parseGamingRootFileResult = ParseGamingRootFile(fileSystem, gamingRootFilePath);
            if (parseGamingRootFileResult.IsFailed)
                errors.AddRange(parseGamingRootFileResult.Errors);
            paths.AddRange(parseGamingRootFileResult.Value);
        }

        return (paths, errors);
    }

    internal static Result<XboxGame> ParseAppManifest(IFileSystem fileSystem,
        AbsolutePath manifestFilePath)
    {
        try
        {
            using var stream = fileSystem.ReadFile(manifestFilePath);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                ValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes,
            });

            var obj = new XmlSerializer(typeof(Package)).Deserialize(reader);
            if (obj is null)
            {
                return Result.Fail($"Unable to deserialize file {manifestFilePath}");
            }

            if (obj is not Package appManifest)
            {
                return Result.Fail($"Deserialization of {manifestFilePath} failed: resulting object is not of type {typeof(Package)} but {obj.GetType()}");
            }

            var displayName = appManifest.Properties.DisplayName;
            var id = appManifest.Identity.Name;
            var game = new XboxGame(XboxGameId.From(id), displayName, manifestFilePath.Parent);
            return Result.Ok(game);
        }
        catch (Exception e)
        {
            return Result.Fail(new Error($"Unable to parse manifest file {manifestFilePath}").CausedBy(e));
        }
    }

    internal static Result<List<AbsolutePath>> ParseGamingRootFile(
        IFileSystem fileSystem, AbsolutePath gamingRootFilePath)
    {
        try
        {
            using var stream = fileSystem.ReadFile(gamingRootFilePath);
            using var reader = new BinaryReader(stream, Encoding.Unicode);

            const uint expectedMagic = 0x58424752;
            var magic = reader.ReadUInt32();
            if (magic != expectedMagic)
            {
                return Result.Fail($"Unable to parse {gamingRootFilePath}, file magic does not match: expected {expectedMagic.ToString("x8", NumberFormatInfo.InvariantInfo)} got {magic.ToString("x8", NumberFormatInfo.InvariantInfo)}");
            }

            var folderCount = reader.ReadUInt32();
            if (folderCount >= byte.MaxValue)
            {
                return Result.Fail($"Folder count exceeds the limit: {folderCount}");
            }

            var parentFolder = gamingRootFilePath.Parent;
            var folders = new List<AbsolutePath>((int)folderCount);
            for (var i = 0; i < folderCount; i++)
            {
                var sb = new StringBuilder();
                var c = reader.ReadChar();
                while (c != '\0')
                {
                    sb.Append(c);
                    c = reader.ReadChar();
                }

                var part = RelativePath.FromUnsanitizedInput(sb.ToString());
                folders.Add(parentFolder.Combine(part));
            }

            return Result.Ok(folders);
        }
        catch (Exception e)
        {
            return Result.Fail(new Error($"Unable to parse gaming root file {gamingRootFilePath}").CausedBy(e));
        }

    }
}
