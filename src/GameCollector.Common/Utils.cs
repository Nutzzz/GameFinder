using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using NexusMods.Paths;

namespace GameCollector.Common;

/// <summary>
/// Utilities.
/// </summary>
public static class Utils
{
    /*
    /// <summary>
    /// Sanitizes the given path.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public static string SanitizeInputPath(string input)
    {
        var sb = new StringBuilder(input.Trim());

        char currentDirectorySeparator;
        char otherDirectorySeparator;
        if (OperatingSystem.IsLinux())
        {
            currentDirectorySeparator = '/';
            otherDirectorySeparator = '\\';
        }
        else if (OperatingSystem.IsWindows())
        {
            currentDirectorySeparator = '\\';
            otherDirectorySeparator = '/';
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        sb.Replace(otherDirectorySeparator, currentDirectorySeparator);
        sb.Replace(
            $"{currentDirectorySeparator}{currentDirectorySeparator}",
            $"{currentDirectorySeparator}"
        );

        int i;
        for (i = sb.Length - 1; i > 0; i--)
        {
            var c = sb[i];
            if (c != currentDirectorySeparator) break;
        }

        var rootLength = GetRootLength(sb, currentDirectorySeparator);
        if (rootLength == 0 || rootLength != i + 2) return sb.ToString(0, i + 1);
        return sb.ToString(0, rootLength);
    }

    private static int GetRootLength(StringBuilder sb, char directorySeparator)
    {
        if (OperatingSystem.IsLinux())
            return sb.Length >= 1 && sb[0] == directorySeparator ? 1 : 0;

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        if (sb.Length < 3 || sb[1] != ':' || !IsValidDriveChar(sb[0])) return 0;
        return sb[2] == directorySeparator ? 3 : 0;
    }

    /// <summary>
    /// Returns true if the given character is a valid drive letter.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidDriveChar(char value)
    {
        // Licensed to the .NET Foundation under one or more agreements.
        // The .NET Foundation licenses this file to you under the MIT license.
        // https://github.com/dotnet/runtime/blob/main/LICENSE.TXT
        // source: https://github.com/dotnet/runtime/blob/d9f453924f7c3cca9f02d920a57e1477293f216e/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L69-L75
        return (uint)((value | 0x20) - 'a') <= 'z' - 'a';
    }
    */

    /// <summary>
    /// Returns the path to the best-guess of a game executable found recursively in a given path.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="name"></param>
    /// <param name="fileSystem"></param>
    /// <returns></returns>
    public static AbsolutePath FindExe(AbsolutePath path, IFileSystem fileSystem, string name = "")
    {
        try
        {
            AbsolutePath exe = new();
            var exes = fileSystem.EnumerateFiles(path, "*.exe", recursive: true).ToList();

            //TODO: Explore ways of making FindExe() better
            if (exes.Count == 1)
                exe = exes[0];
            else
            {
                var j = 0;
                foreach (var file in exes)
                {
                    j++;
                    var tmpFile = file.FileName;
                    List<string> badNames = new()
                {
                    "unins", "install", "patch", "redist", "prereq", "dotnet", "setup", "config", "w9xpopen", "edit", "help",
                    "python", "server", "service", "cleanup", "anticheat", "touchup", "error", "crash", "report", "helper", "handler",
                };
                    List<string> goodNames = new()
                    {
                        //"launch", "scummvm",
                    };
                    foreach (var badName in badNames)
                    {
                        if (tmpFile.Contains(badName, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        var nameSanitized = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
                        var nameAlphanum = name.Where(c => c == 32 || (char.IsLetterOrDigit(c) && c < 128)).ToString();
                        if (tmpFile.Contains(nameSanitized, StringComparison.OrdinalIgnoreCase) ||
                            tmpFile.Contains(nameSanitized.Replace(' ', '-'), StringComparison.OrdinalIgnoreCase) ||
                            tmpFile.Contains(nameSanitized.Replace(' ', '_'), StringComparison.OrdinalIgnoreCase) ||
                            tmpFile.Contains(nameSanitized.Remove(' '), StringComparison.OrdinalIgnoreCase) ||
                            (nameAlphanum is not null &&
                            (tmpFile.Contains(nameAlphanum, StringComparison.OrdinalIgnoreCase) ||
                            tmpFile.Contains(nameAlphanum.Replace(' ', '-'), StringComparison.OrdinalIgnoreCase) ||
                            tmpFile.Contains(nameAlphanum.Replace(' ', '_'), StringComparison.OrdinalIgnoreCase) ||
                            tmpFile.Contains(nameAlphanum.Remove(' '), StringComparison.OrdinalIgnoreCase))))
                        {
                            exe = file;
                            break;
                        }
                    }
                    foreach (var goodName in goodNames)
                    {
                        if (tmpFile.Contains(goodName, StringComparison.OrdinalIgnoreCase))
                        {
                            exe = file;
                            break;
                        }
                    }
                    exe = file;
                }
            }
            return exe;
        }
        catch (Exception) { }
        
        return default;
    }
}
