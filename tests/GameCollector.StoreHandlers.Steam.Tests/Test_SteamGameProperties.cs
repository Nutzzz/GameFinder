using System.Globalization;
using System.IO.Abstractions.TestingHelpers;

namespace GameCollector.StoreHandlers.Steam.Tests;

public partial class SteamTests
{
    [Theory, AutoData]
    public void Test_ShouldWork_GetManifestPath(MockFileSystem fs, int id, string name)
    {
        var path = fs.Path.Combine(fs.Path.GetTempPath(), "common", name);
        var expectedManifestPath = fs.Path.Combine(
            fs.Path.GetTempPath(),
            $"{id.ToString(CultureInfo.InvariantCulture)}.acf");

        SteamGame game = new(id, name, path);

        game.GetManifestPath(fs).Should().Be(expectedManifestPath);
        game.GetManifestPath().Should().Be(expectedManifestPath);
    }
}
