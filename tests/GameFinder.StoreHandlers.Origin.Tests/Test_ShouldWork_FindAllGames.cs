using GameFinder.RegistryUtils;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
using TestUtils;

namespace GameFinder.StoreHandlers.Origin.Tests;

public partial class OriginTests
{
    [Theory, AutoFileSystem]
    public void Test_ShouldWork_FindAllGames(InMemoryFileSystem fs, InMemoryRegistry registry)
    {
        var (handler, manifestDir) = SetupHandler(fs, registry);
        var expectedGames = SetupGames(fs, manifestDir);
        handler.ShouldFindAllGames(expectedGames);
    }

    [Theory, AutoFileSystem]
    public void Test_ShouldWork_FindAllGamesById(InMemoryFileSystem fs, InMemoryRegistry registry)
    {
        var (handler, manifestDir) = SetupHandler(fs, registry);
        var expectedGames = SetupGames(fs, manifestDir).ToArray();
        handler.ShouldFindAllGamesById(expectedGames, game => game.Id);
    }

    [Theory, AutoFileSystem]
    public void Test_ShouldWork_FindAllInterfaceGames(InMemoryFileSystem fs, InMemoryRegistry registry)
    {
        var (handler, manifestDir) = SetupHandler(fs, registry);
        var expectedGames = SetupGames(fs, manifestDir);
        handler.ShouldFindAllInterfacesGames(expectedGames);
    }
}
