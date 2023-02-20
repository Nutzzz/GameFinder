using System.IO.Abstractions.TestingHelpers;
using GameCollector.Common;
using GameCollector.RegistryUtils;
using TestUtils;

namespace GameCollector.StoreHandlers.EGS.Tests;

public partial class EGSTests
{
    private static (EGSHandler handler, string manifestDir) SetupHandler(MockFileSystem fs, InMemoryRegistry registry)
    {
        var fixture = new Fixture();

        var manifestDirName = fixture.Create<string>();
        var manifestDir = fs.Path.Combine(fs.Path.GetTempPath(), manifestDirName);

        fs.AddDirectory(manifestDir);

        var regKey = registry.AddKey(RegistryHive.CurrentUser, EGSHandler.RegKey);
        regKey.AddValue(EGSHandler.ModSdkMetadataDir, manifestDir);

        var handler = new EGSHandler(registry, fs);
        return (handler, manifestDir);
    }

    private static IEnumerable<Game> SetupGames(MockFileSystem fs, string manifestDir)
    {
        var fixture = new Fixture();

        fixture
            .Customize<Game>(composer => composer
                .FromFactory<string, string>((catalogItemId, displayName) =>
                {
                    var manifestItem = fs.Path.Combine(manifestDir, $"{catalogItemId}.item");
                    var installLocation = fs.Path.Combine(manifestDir, displayName);

                    var mockData = $@"{{
    ""CatalogItemId"": ""{catalogItemId}"",
    ""DisplayName"": ""{displayName}"",
    ""InstallLocation"": ""{installLocation.ToEscapedString()}""
}}";

                    fs.AddDirectory(installLocation);
                    fs.AddFile(manifestItem, mockData);

                    return new Game(catalogItemId, displayName, installLocation);
                })
                .OmitAutoProperties());

        return fixture.CreateMany<Game>();
    }
}
