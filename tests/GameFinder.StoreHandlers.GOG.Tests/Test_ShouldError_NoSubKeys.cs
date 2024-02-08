using GameFinder.RegistryUtils;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
using TestUtils;

namespace GameFinder.StoreHandlers.GOG.Tests;

public partial class GOGTests
{
    [Theory(Skip = "Fix me"), AutoFileSystem]
    public void Test_ShouldError_NoSubKeys(InMemoryFileSystem fileSystem, InMemoryRegistry registry)
    {
        var (handler, gogKey) = SetupHandler(fileSystem, registry);

        foreach (var error in handler.ShouldOnlyBeErrors())
        {
            error.Should().BeOneOf($"Registry key {gogKey.GetName()} has no sub-keys", "GOG database not found.");
        }
    }
}
