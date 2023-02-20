using System.IO.Abstractions.TestingHelpers;
using System.Web;
using GameCollector.Common;

namespace GameCollector.StoreHandlers.Origin.Tests;

public partial class OriginTests
{
    private static (OriginHandler handler, string manifestDir) SetupHandler(MockFileSystem fs)
    {
        var manifestDir = OriginHandler.GetManifestDir(fs);
        fs.AddDirectory(manifestDir.FullName);

        var handler = new OriginHandler(fs);
        return (handler, manifestDir.FullName);
    }

    private static IEnumerable<Game> SetupGames(MockFileSystem fs, string manifestDir)
    {
        var fixture = new Fixture();

        fixture.Customize<Game>(composer => composer
            .FromFactory<string>(id =>
            {
                var installPath = fs.Path.Combine(manifestDir, id);

                var manifest = fs.Path.Combine(manifestDir, $"{id}.mfst");
                fs.AddFile(manifest, $"?id={HttpUtility.UrlEncode(id)}&dipInstallPath={HttpUtility.UrlEncode(installPath)}");

                return new Game(id, "", installPath);
            })
            .OmitAutoProperties());

        return fixture.CreateMany<Game>();
    }
}
