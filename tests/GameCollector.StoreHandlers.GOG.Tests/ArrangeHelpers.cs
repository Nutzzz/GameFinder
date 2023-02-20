using System.Globalization;
using GameCollector.Common;
using GameCollector.RegistryUtils;

namespace GameCollector.StoreHandlers.GOG.Tests;

public partial class GOGTests
{
    public static (GOGHandler handler, InMemoryRegistryKey gogKey) SetupHandler(InMemoryRegistry registry)
    {
        var gogKey = registry.AddKey(RegistryHive.LocalMachine, GOGHandler.GOGRegKey);
        var handler =  new GOGHandler(registry);
        return (handler, gogKey);
    }

    public static IEnumerable<Game> SetupGames(InMemoryRegistryKey gogKey)
    {
        var fixture = new Fixture();

        fixture.Customize<Game>(composer => composer
            .FromFactory<string, string>((id, name) =>
            {
                var path = Path.Combine(Path.GetTempPath(), name);

                var gameKey = gogKey.AddSubKey(id.ToString(CultureInfo.InvariantCulture));
                gameKey.AddValue("gameID", id.ToString(CultureInfo.InvariantCulture));
                gameKey.AddValue("gameName", name);
                gameKey.AddValue("path", path);

                return new Game(id, name, path);
            })
            .OmitAutoProperties());

        return fixture.CreateMany<Game>();
    }
}
