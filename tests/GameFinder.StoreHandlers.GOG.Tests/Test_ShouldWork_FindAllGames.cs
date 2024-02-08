using GameFinder.RegistryUtils;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
using TestUtils;

namespace GameFinder.StoreHandlers.GOG.Tests;

public partial class GOGTests
{
    [Theory(Skip = "Fix me"), AutoFileSystem]
    public void Test_ShouldWork_FindAllGames(InMemoryFileSystem fileSystem, InMemoryRegistry registry)
    {
        var (handler, gogKey) = SetupHandler(fileSystem, registry);
        var expectedGames = SetupGames(fileSystem, gogKey);

        handler.ShouldFindAllGames(expectedGames);
    }

    [Theory(Skip = "Fix me"), AutoFileSystem]
    public void Test_ShouldWork_FindAllGamesById(InMemoryFileSystem fileSystem, InMemoryRegistry registry)
    {
        var (handler, gogKey) = SetupHandler(fileSystem, registry);
        var expectedGames = SetupGames(fileSystem, gogKey).ToArray();

        handler.ShouldFindAllGamesById(expectedGames, game => game.Id);
    }

    [Theory(Skip = "Fix me"), AutoFileSystem]
    public void Test_ShouldWork_FindAllInterfaceGames(InMemoryFileSystem fileSystem, InMemoryRegistry registry)
    {
        var (handler, gogKey) = SetupHandler(fileSystem, registry);
        var expectedGames = SetupGames(fileSystem, gogKey);

        handler.ShouldFindAllInterfacesGames(expectedGames);
    }
}
