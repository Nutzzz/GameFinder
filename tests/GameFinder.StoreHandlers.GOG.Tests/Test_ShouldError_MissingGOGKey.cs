using GameCollector.StoreHandlers.GOG;
using GameFinder.RegistryUtils;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
using TestUtils;

namespace GameFinder.StoreHandlers.GOG.Tests;

public partial class GOGTests
{
    [Theory(Skip = "Fix me"), AutoFileSystem]
    public void Test_ShouldError_MissingGOGKey(InMemoryFileSystem fileSystem, InMemoryRegistry registry)
    {
        var handler = new GOGHandler(registry, fileSystem);

        foreach (var error in handler.ShouldOnlyBeErrors())
        {
            error.Should().BeOneOf($"Unable to open HKEY_LOCAL_MACHINE\\{GOGHandler.GOGRegKey}", "GOG database not found.");
        }
    }
}
