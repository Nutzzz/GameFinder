# GameCollector
#### a fork of [GameFinder](https://github.com/erri120/GameFinder)

[![CI](https://github.com/Nutzzz/GameCollector/actions/workflows/ci.yml/badge.svg)](https://github.com/Nutzzz/GameCollector/actions/workflows/ci.yml) [![codecov](https://codecov.io/gh/Nutzzz/GameCollector/branch/master/graph/badge.svg?token=ARU010EHZ4)](https://codecov.io/gh/Nutzzz/GameCollector)

.NET library for finding games. GameCollector expands on upstream GameFinder (which is primarily designed to support modding tools), by adding additional supported store launchers, emulators, and data sources, and includes additional information about each game (sufficient for a multi-store game launcher such as [GLC](https://github.com/Solaire/GLC)). The following launchers and emulators are supported:

| handler | package |
| -- | -- |
| Amazon Games | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Amazon)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Amazon) |
| Arc | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Arc)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Arc) |
| [Bethesda.net](#bethesdanet) | [![Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.BethNet?color=red&label=deprecated,upstream)](https://www.nuget.org/packages/GameFinder.StoreHandlers.BethNet) |
| Big Fish Game Manager | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.BigFish)](https://www.nuget.org/packages/GameCollector.StoreHandlers.BigFish) |
| Blizzard Battle.net | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.BattleNet)](https://www.nuget.org/packages/GameCollector.StoreHandlers.BattleNet) |
| Dolphin Emulator | [![Nuget](https://img.shields.io/nuget/v/GameCollector.EmuHandlers.Dolphin)](https://www.nuget.org/packages/GameCollector.EmuHandlers.Dolphin) |
| [EA app](#ea-app) | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.EADesktop)](https://www.nuget.org/packages/GameCollector.StoreHandlers.EADesktop) [![Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.EADesktop?color=red&label=upstream)](https://www.nuget.org/packages/GameFinder.StoreHandlers.EADesktop) |
| [Epic Games Store](#epic-games-store) | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.EGS)](https://www.nuget.org/packages/GameCollector.StoreHandlers.EGS) [![Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.EGS?color=red&label=upstream)](https://www.nuget.org/packages/GameFinder.StoreHandlers.EGS) |
| Game Jolt Client | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.GameJolt)](https://www.nuget.org/packages/GameCollector.StoreHandlers.GameJolt) |
| [GOG Galaxy](#gog-galaxy) | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.GOG)](https://www.nuget.org/packages/GameCollector.StoreHandlers.GOG) [![Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.GOG?color=red&label=upstream)](https://www.nuget.org/packages/GameFinder.StoreHandlers.GOG) |
| Humble App | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Humble)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Humble) |
| Indiegala IGClient | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.IGClient)](https://www.nuget.org/packages/GameCollector.StoreHandlers.IGClient) |
| itch | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Itch)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Itch) |
| Legacy Games Launcher | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Legacy)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Legacy) |
| Multiple Arcade Machine Emulator (MAME) | [![Nuget](https://img.shields.io/nuget/v/GameCollector.EmuHandlers.MAME)](https://www.nuget.org/packages/GameCollector.EmuHandlers.MAME) |
| Oculus | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Oculus)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Oculus) |
| [Origin](#origin) | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Origin)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Origin) [![Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.Origin?color=red&label=upstream)](https://www.nuget.org/packages/GameFinder.StoreHandlers.Origin) |
| Paradox Launcher | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Paradox)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Paradox) |
| Plarium Play | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Plarium)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Plarium) |
| Riot Client | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Riot)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Riot) |
| Robot Cache Client | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.RobotCache)](https://www.nuget.org/packages/GameCollector.StoreHandlers.RobotCache) |
| Rockstar Games Launcher | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Rockstar)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Rockstar) |
| [Steam](#steam) | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Steam)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Steam) [![Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.Steam?color=red&label=upstream)](https://www.nuget.org/packages/GameFinder.StoreHandlers.Steam) |
| Ubisoft Connect | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Ubisoft)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Ubisoft) |
| Wargaming.net Game Center | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.WargamingNet)](https://www.nuget.org/packages/GameCollector.StoreHandlers.WargamingNet) |
| [Xbox Game Pass](#xbox-game-pass) | [![Nuget](https://img.shields.io/nuget/v/GameCollector.StoreHandlers.Xbox)](https://www.nuget.org/packages/GameCollector.StoreHandlers.Xbox) [![Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.Xbox?color=red&label=upstream)](https://www.nuget.org/packages/GameFinder.StoreHandlers.Xbox) |

If you are interested in understanding _how_ GameCollector/GameFinder finds these games, check [the upstream wiki](https://github.com/erri120/GameFinder/wiki) for more information.

Additionally, the following Linux tools are supported:

| handler | package |
| -- | :--: |
| [Wine](#wine) | [![Nuget](https://img.shields.io/nuget/v/GameCollector.Wine)](https://www.nuget.org/packages/GameCollector.Wine) [![Nuget](https://img.shields.io/nuget/v/GameFinder.Wine?color=red&label=upstream)](https://www.nuget.org/packages/GameFinder.Wine) |

## Example

The [example project](./other/GameFinder.Example) uses every available store handler and can be used as a reference. You can go to the [GitHub Actions Page](https://github.com/Nutzzz/GameCollector/actions/workflows/ci.yml) and click on one of the latest CI workflow runs to download a build of this project.

## Usage

All store handlers inherit from `AHandler<TGame, TId>` and implement `FindAllGames()` which returns `IEnumerable<OneOf<TGame, ErrorMessage>>`. The [`OneOf`](https://github.com/mcintyre321/OneOf) struct is a F# style union and is guaranteed to only contain _one of_ the following: a `TGame` or an `ErrorMessage`. I recommended checking out the [OneOf library](https://github.com/mcintyre321/OneOf), if you want to learn more.

Some **important** things to remember:

- All store handler methods are _pure_, meaning they do not change the internal state of the store handler because they don't have any. This also means that the **results are not cached** and you **shouldn't call the same method multiple times**. It's up to the library consumer to cache the results somewhere.
- Ids are **store dependent**. Each store handler has their own type of id and figuring out the right id for your game might require some testing. You can find useful resources in this README for some store handlers.

### Basic Usage

```csharp
var results = handler.FindAllGames();

foreach (var result in results)
{
    // using the switch method
    result.Switch(game =>
    {
        Console.WriteLine($"Found {game}");
    }, error =>
    {
        Console.WriteLine(error);
    });

    // using the provided extension functions
    if (result.TryGetGame(out var game))
    {
        Console.WriteLine($"Found {game}");
    } else
    {
        Console.WriteLine(result.AsError());
    }
}
```

### Finding a single game

If you're working on an application that only needs to find **1** game, then you can use the `FindOneGameById` method instead. **IMPORTANT NOTE: the results are not cached**. If you call this method multiple, the store handler will do the same thing multiple times, which is search for every game installed. Do not call this method if you need to [find multiple games](#finding-multiple-games).

```csharp
var game = handler.FindOneGameById(someId, out var errors);

// I highly recommend logging errors regardless of whether or not the game was found.
foreach (var error in errors)
{
    Console.WriteLine(error);
}

if (game is null)
{
    Console.WriteLine("Unable to find game");
} else
{
    Console.WriteLine($"Found {game}");
}
```

### Finding multiple games

If you need to find multiple games at once, you can use the `FindAllGamesById` method instead. This returns an `IReadOnlyDictionary<TId, TGame>` which you can use to lookup games by id. **IMPORTANT NOTE: the results are not cached**. You have to do that yourself.

```csharp
var games = handler.FindAllGamesById(out var errors);

// I highly recommend always logging errors.
foreach (var error in errors)
{
    Console.WriteLine(error);
}

if (games.TryGetValue(someId, out var game))
{
    Console.WriteLine($"Found {game}");
} else
{
    Console.WriteLine($"Unable to find game with the id {someId}");
}
```

## Supported Emulators

This is a new category of handler for GameCollector. They are Windows-only for now. These both require you pass the path to the emulator executable.

- Dolphin
- MAME

## New Supported Launchers

The following handlers have been added for GameCollector. They are all Windows-only for now:

- Amazon Games
- Arc
- Big Fish Game Manager
- Blizzard Battle.net
- Game Jolt
- Humble App
- Indiegala IGClient
- itch
- Legacy Games Launcher
- Oculus
- Paradox Launcher
- Plarium Play
- Riot Client
- Robot Cache Client
- Rockstar Games Launcher
- Ubisoft Connect
- Wargaming.net Game Center

## Upstream Supported Launchers

The following handlers come from upstream [GameFinder](https://github.com/erri120/GameFinder):

### Steam

Steam is supported natively on Windows and Linux. Use [SteamDB](https://steamdb.info/) to find the ID of a game.

**Usage (cross-platform):**

```csharp
var handler = new SteamHandler(FileSystem.Shared, OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null);
```

GameCollector adds the ability to check a Steam profile for owned not-installed games. A specific [Steam ID](https://store.steampowered.com/account) may be specified, though it will automatically attempt to find one. However, this feature requires that [an API key be activated](https://steamcommunity.com/dev/apikey) and specified, and the [user profile set to public](https://steamcommunity.com/my/edit/settings).

### GOG Galaxy

GOG Galaxy is supported natively on Windows, and with [Wine](#wine) on Linux. Use the [GOG Database](https://www.gogdb.org/) to find the ID of a game.

**Usage (native on Windows):**

```csharp
var handler = new GOGHandler(WindowsRegistry.Shared, FileSystem.Shared);
```

**Usage (Wine on Linux):**

See [Wine](#wine) for more information.

```csharp
// requires a valid prefix
var wineFileSystem = winePrefix.CreateOverlayFileSystem(FileSystem.Shared);
var wineRegistry = winePrefix.CreateRegistry(FileSystem.Shared);

var handler = new GOGHandler(wineRegistry, wineFileSystem);
```

GameCollector adds finding owned not-installed GOG games.

### Epic Games Store

The Epic Games Store is supported natively on Windows, and with [Wine](#wine) on Linux. Use the [Epic Games Store Database](https://github.com/erri120/egs-db) to find the ID of a game (**WIP**).

**Usage (native on Windows):**

```csharp
var handler = new EGSHandler(WindowsRegistry.Shared, FileSystem.Shared);
```

**Usage (Wine on Linux):**

See [Wine](#wine) for more information.

```csharp
// requires a valid prefix
var wineFileSystem = winePrefix.CreateOverlayFileSystem(FileSystem.Shared);
var wineRegistry = winePrefix.CreateRegistry(FileSystem.Shared);

var handler = new EGSHandler(wineRegistry, wineFileSystem);
```

GameCollector adds finding owned not-installed EGS games.

### Origin

Origin is supported natively on Windows, and with [Wine](#wine) on Linux. **Note:** [EA is deprecating Origin](https://www.ea.com/en-gb/news/ea-app) and will replace it with [EA app](#ea-app).

**Usage (native on Windows):**

```csharp
var handler = new OriginHandler(FileSystem.Shared);
```

**Usage (Wine on Linux):**

See [Wine](#wine) for more information.

```csharp
// requires a valid prefix
var wineFileSystem = winePrefix.CreateOverlayFileSystem(FileSystem.Shared);

var handler = new OriginHandler(wineFileSystem);
```

### EA app

The EA app is the replacement for [Origin](#origin) on Windows: See [EA is deprecating Origin](https://www.ea.com/en-gb/news/ea-app). This is by far the most complicated Store Handler. **You should read the [upstream wiki entry](https://github.com/erri120/GameFinder/wiki/EA-Desktop).** This implementation decrypts the encrypted file created by the EA app. You should be aware that the key used to encrypt the file is derived from hardware information. If the user changes their hardware, the decryption process might fail because they key has changed.

The EA app is only supported on Windows.

**Usage:**

```csharp
var handler = new EADesktopHandler(FileSystem.Shared, new HardwareInfoProvider());
```

### Xbox Game Pass

This package used to be deprecated, but support was re-added in GameFinder [3.0.0](./CHANGELOG.md#300---2023-05-09). Xbox Game Pass used to install games inside a `SYSTEM` protected folder, making modding not feasible for the average user. You can read more about this [here](https://github.com/Nexus-Mods/NexusMods.App/issues/175).

Xbox Game Pass is only supported on Windows.

**Usage:**

```csharp
var handler = new XboxHandler(FileSystem.Shared);
```

### Bethesda.net

[As of May 11, 2022, the Bethesda.net launcher is no longer in use](https://bethesda.net/en/article/2RXxG1y000NWupPalzLblG/sunsetting-the-bethesda-net-launcher-and-migrating-to-steam). The upstream package [GameFinder.StoreHandlers.BethNet](https://www.nuget.org/packages/GameFinder.StoreHandlers.BethNet/) has been deprecated and marked as _legacy_.


## Wine

[Wine](https://www.winehq.org/) is a compatibility layer capable of running Windows applications on Linux. Wine uses [prefixes](https://wiki.winehq.org/FAQ#Wineprefixes) to create and store virtual `C:` drives. A user can install and run Windows program inside these prefixes, and applications running inside the prefixes likely won't even notice they are not actually running on Windows.

Since GameCollector/GameFinder is all about finding games, it also has to be able to find games inside Wine prefixes to provide good Linux support. The package `NexusMods.Paths` from [NexusMods.App](https://github.com/Nexus-Mods/NexusMods.App) provides a file system abstraction `IFileSystem` which enables path re-mappings:

```csharp
AWinePrefix prefix = //...

// creates a new IFileSystem, with path mappings into the wine prefix
IFileSystem wineFileSystem = prefix.CreateOverlayFileSystem(FileSystem.Shared);

// this wineFileSystem can be used instead of FileSystem.Shared:
var handler = new OriginHandler(wineFileSystem);

// you can also create a new IRegistry:
IRegistry wineRegistry = prefix.CreateRegistry(FileSystem.Shared);

// and use both:
var handler = new EGSHandler(wineRegistry, wineFileSystem);
```

### Default Prefix Manager

`GameFinder.Wine` implements a `IWinePrefixManager` for finding Wine prefixes.

**Usage**:

```csharp
var prefixManager = new DefaultWinePrefixManager(FileSystem.Shared);

foreach (var result in prefixManager.FindPrefixes())
{
    result.Switch(prefix =>
    {
        Console.WriteLine($"Found wine prefix at {prefix.ConfigurationDirectory}");
    }, error =>
    {
        Console.WriteLine(error.Value);
    });
}
```

### Bottles

`GameFinder.Wine` implements a `IWinePrefixManager` for finding Wine prefixes managed by [Bottles](https://usebottles.com/).

**Usage**:

```csharp
var prefixManager = new BottlesWinePrefixManager(FileSystem.Shared);

foreach (var result in prefixManager.FindPrefixes())
{
    result.Switch(prefix =>
    {
        Console.WriteLine($"Found wine prefix at {prefix.ConfigurationDirectory}");
    }, error =>
    {
        Console.WriteLine(error.Value);
    });
}
```

### Proton

Valve's [Proton](https://github.com/ValveSoftware/Proton) is a compatibility tool for Steam and is mostly based on Wine. The Wine prefixes managed by Proton are in the `compatdata` directory of the steam library where the game itself is installed. Since the path is relative to the game itself and requires the app id, erri120 decided to put this functionality in `GameFinder.StoreHandlers.Steam`:

```csharp
SteamGame? steamGame = steamHandler.FindOneGameById(1237970, out var errors);
if (steamGame is null) return;

ProtonWinePrefix protonPrefix = steamGame.GetProtonPrefix();
var protonPrefixDirectory = protonPrefix.ProtonDirectory;

if (protonDirectory != default && fileSystem.DirectoryExists(protonDirectory))
{
    Console.WriteLine($"Proton prefix is at {protonDirectory}");
}
```

## Trimming

Self-contained deployments and executables can be [trimmed](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained) starting with .NET 6. This feature is _only_ available to applications that are published self-contained.

**Trimmable**:

- `GameFinder.Common`
- `GameFinder.RegistryUtils`
- `GameFinder.Wine`
- `GameFinder.StoreHandlers.Steam`
- `GameFinder.StoreHandlers.GOG`
- `GameFinder.StoreHandlers.EGS`
- `GameFinder.StoreHandlers.Origin`

**NOT Trimmable**:

- `GameFinder.StoreHandlers.EADesktop`: This package references `System.Management`, which is **not trimmable** due to COM interop issues. See [dotnet/runtime#78038](https://github.com/dotnet/runtime/issues/78038), [dotnet/runtime#75176](https://github.com/dotnet/runtime/pull/75176) and [dotnet/runtime#61960](https://github.com/dotnet/runtime/issues/61960) for more details.

I recommend looking at the [project file](./other/GameFinder.Example/GameFinder.Example.csproj) of the example project, if you run into warnings or errors with trimming.

## Contributing

See [CONTRIBUTING](CONTRIBUTING.md) for more information.

## License

See [LICENSE](LICENSE) for more information.
