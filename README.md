# GameCollector
A fork of [err120/GameFinder](https://github.com/erri120/GameFinder), collecting more data while supporting many more launchers

[![CI](https://github.com/Nutzzz/GameCollector/actions/workflows/ci.yml/badge.svg)](https://github.com/Nutzzz/GameCollector/actions/workflows/ci.yml) [![codecov](https://codecov.io/gh/Nutzzz/GameCollector/branch/master/graph/badge.svg?token=10PVRFWH39)](https://codecov.io/gh/Nutzzz/GameCollector)

.NET library for finding games. The following launchers are supported (so far):

- Amazon Games
- Arc
- Blizzard Battle.net
- [EA Desktop](#ea-desktop) [![GameFinder Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.EADesktop)](https://www.nuget.org/packages/GameFinder.StoreHandlers.EADesktop)
- [Epic Games Store](#epic-games-store) [![GameFinder Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.EGS)](https://www.nuget.org/packages/GameFinder.StoreHandlers.EGS)
- [GOG Galaxy](#gog-galaxy) [![GameFinder Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.GOG)](https://www.nuget.org/packages/GameFinder.StoreHandlers.GOG)
- [Steam](#steam) [![GameFinder Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.Steam)](https://www.nuget.org/packages/GameFinder.StoreHandlers.Steam)
- Ubisoft Connect

The following launchers are not yet supported or support has been dropped:

- [Bethesda.net Launcher](#bethesdanet-launcher) [![GameFinder Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.BethNet?color=red&label=deprecated)](https://www.nuget.org/packages/GameFinder.StoreHandlers.BethNet)
- Big Fish Games (WIP)
- Game Jolt Client (WIP)
- Humble App (WIP)
- Indiegala IGClient (WIP)
- itch (WIP)
- Legacy Games Launcher (WIP)
- Oculus (WIP)
- [Origin](#origin) [![GameFinder Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.Origin)](https://www.nuget.org/packages/GameFinder.StoreHandlers.Origin)
- Paradox Launcher (WIP)
- Plarium Play (WIP)
- Riot Client (WIP)
- Rockstar Games Launcher (WIP)
- Wargaming.net Game Center (WIP)
- [Xbox Game Pass](#xbox-game-pass) [![GameFinder Nuget](https://img.shields.io/nuget/v/GameFinder.StoreHandlers.Xbox?color=red&label=deprecated)](https://www.nuget.org/packages/GameFinder.StoreHandlers.Xbox) (UWP apps, see [Finding Xbox Games](#how-to-find-xbox-game-pass-uwp-games) for more information)

If you are interested in understanding _how_ GameCollector finds some of these games, check the upstream [GameFinder wiki](https://github.com/erri120/GameFinder/wiki) for more information.

## Supported Launchers

### Steam

Steam is supported on Windows and Linux. Use [SteamDB](https://steamdb.info/) to find the ID of a game.

**Usage:**

```csharp
// use the Windows registry on Windows
// Linux doesn't have a registry
var handler = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? new SteamHandler(new WindowsRegistry())
    : new SteamHandler(registry: null);

// method 1: iterate over the game-error result
foreach (var (game, error) in handler.FindAllGames())
{
    if (game is not null)
    {
        Console.WriteLine($"Found {game}");
    }
    else
    {
        Console.WriteLine($"Error: {error}");
    }
}

// method 2: use the dictionary if you need to find games by id
Dictionary<Game, string> games = handler.FindAllGamesById(out string[] errors);

// method 3: find a single game by id
Game? game = handler.FindOneGameById(570940, out string[] errors);
```

### GOG Galaxy

GOG Galaxy is only supported on Windows. Use the [GOG Database](https://www.gogdb.org/) to find the ID of a game.

**Usage:**

```csharp
var handler = new GOGHandler();

// method 1: iterate over the game-error result
foreach (var (game, error) in handler.FindAllGames())
{
    if (game is not null)
    {
        Console.WriteLine($"Found {game}");
    }
    else
    {
        Console.WriteLine($"Error: {error}");
    }
}

// method 2: use the dictionary if you need to find games by id
Dictionary<Game, string> games = handler.FindAllGamesById(out string[] errors);

// method 3: find a single game by id
Game? game = handler.FindOneGameById(1971477531, out string[] errors);
```

### Epic Games Store

Epic Games Store is only supported on Windows.

**Usage:**

```csharp
var handler = new EGSHandler();

// method 1: iterate over the game-error result
foreach (var (game, error) in handler.FindAllGames())
{
    if (game is not null)
    {
        Console.WriteLine($"Found {game}");
    }
    else
    {
        Console.WriteLine($"Error: {error}");
    }
}

// method 2: use the dictionary if you need to find games by id
Dictionary<Game, string> games = handler.FindAllGamesById(out string[] errors);

// method 3: find a single game by id
Game? game = handler.FindOneGameById("3257e06c28764231acd93049f3774ed6", out string[] errors);
```

### EA Desktop

EA Desktop is the replacement for [Origin](#origin): See [EA is deprecating Origin](https://www.ea.com/en-gb/news/ea-app). This is by far, the most complicated Store Handler. **You should read the [wiki entry](https://github.com/erri120/GameFinder/wiki/EA-Desktop).** @erri120's implementation decrypts the encrypted file, created by EA Desktop. You should be aware that the key used to encrypt the file is derived from hardware information. If the user changes their hardware, the decryption process might fail because their key has changed.

**Usage:**

```csharp
var handler = new EADesktopHandler();

// method 1: iterate over the game-error result
foreach (var (game, error) in handler.FindAllGames())
{
    if (game is not null)
    {
        Console.WriteLine($"Found {game}");
    }
    else
    {
        Console.WriteLine($"Error: {error}");
    }
}

// method 2: use the dictionary if you need to find games by id
Dictionary<Game, string> games = handler.FindAllGamesById(out string[] errors);

// method 3: find a single game by id
Game? game = handler.FindOneGameById("Origin.SFT.50.0000532", out string[] errors);
```

### Origin

Origin is only supported on Windows. **Note:** [EA is deprecating Origin](https://www.ea.com/en-gb/news/ea-app) and will replace it with [EA Desktop](#ea-desktop).

**Usage:**

```csharp
var handler = new OriginHandler();

// method 1: iterate over the game-error result
foreach (var (game, error) in handler.FindAllGames())
{
    if (game is not null)
    {
        Console.WriteLine($"Found {game}");
    }
    else
    {
        Console.WriteLine($"Error: {error}");
    }
}

// method 2: use the dictionary if you need to find games by id
Dictionary<Game, string> games = handler.FindAllGamesById(out string[] errors);

// method 3: find a single game by id
Game? game = handler.FindOneGameById("Origin.OFR.50.0001456", out string[] errors);
```


### Xbox Game Pass (UWP)

The GameFinder package [GameFinder.StoreHandlers.Xbox](https://www.nuget.org/packages/GameFinder.StoreHandlers.Xbox/) has been deprecated and marked as _legacy_. @erri120 no longer maintains this package because it never got used. He initially made GameFinder for [Wabbajack](https://github.com/wabbajack-tools/wabbajack) and other modding tools however, you can't mod games installed with the Xbox App on Windows. These games are installed as UWP apps, which makes them protected and hard to modify. Another issue is the fact that you can't distinguish between normal UWP apps and Xbox games, meaning your calculator will show up as an Xbox game.

The final issue is related to actual code: in order to find all UWP apps it used the Windows SDK, which was a pain to integrate. The CI had to be on Windows, the .NET target framework had to be a Windows specific version (`net7.0-windows-XXXXXXXXXX`), and it was overall not nice to use.

The package is still available with [GameFinder NuGet](https://www.nuget.org/packages/GameFinder.StoreHandlers.Xbox/) and should still work, but it's marked as deprecated and won't receive any updates.

#### How to find Xbox Game Pass (UWP) Games

Implementation can be found in `GameFinder.StoreHandlers.Xbox`: [XboxHandler](GameFinder.StoreHandlers.Xbox/XboxHandler.cs).

These games are installed through the Xbox Game Pass app or the Windows Store. These games are UWP packages and in a UWP container. I had to use the Windows 10 SDK to get all packages:

```c#
internal static IEnumerable<Package> GetUWPPackages()
{
   var manager = new PackageManager();
   var user = WindowsIdentity.GetCurrent().User;
   var packages = manager.FindPackagesForUser(user.Value)
       .Where(x => !x.IsFramework && !x.IsResourcePackage && x.SignatureKind == PackageSignatureKind.Store)
       .Where(x => x.InstalledLocation != null);
   return packages;
}
```

Since the packages uses the Windows 10 SDK the project settings had to change to reflect that:

```xml
<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup>
     <TargetFrameworks>net6.0-windows10.0.20348.0;net7.0-windows10.0.20348.0</TargetFrameworks>
 </PropertyGroup>
</Project>
```

Since the query from above will get us all UWP packages, some of those might not be Xbox games. The solution to this is getting a list of all games the current user owns on Xbox Game Pass which we can get using the Xbox REST API:

```
https://titlehub.xboxlive.com/users/xuid({_xuid})/titles/titlehistory/decoration/details
```

The main problem is getting the `xuid` parameter. This library can accept the `xuid` parameter in the `XboxHandler` constructor and get the title history. Using the title history the library will filter out all installed UWP packages that are not present in the title history.

The following information is on how to get the `xuid` parameter:

Follow [this](https://docs.microsoft.com/en-us/advertising/guides/authentication-oauth-live-connect) guide on how to get started with Live Connect Authentication. After the user logged in with OAuth at `https://login.live.com/oauth20_authorize.srf` you can get the following parameters from the redirection url: `#access_token`, `refresh_token`, `expires_in`, `token_type` and `user_id`.

The access token is needed to authenticate at `https://user.auth.xboxlive.com/user/authenticate` by doing a POST with the following data (remember to use content-type `application/json`):

```json
{
   "RelyingPart": "http://auth.xboxlive.com",
   "TokenType": "JWT",
   "Properties": {
      "AuthMethod": "RPS",
      "SiteName": "user.auth.xboxlive.com",
      "RpsTicket": "<access_token>"
   }
}
```

(DTO: [AuthenticationRequest](GameFinder.StoreHandlers.Xbox/DTO/AuthenticationRequest.cs))

The response is also JSON:

```json
{
   "Token": "the-only-important-field"
}
```

(DTO: [AuthorizationData](GameFinder.StoreHandlers.Xbox/DTO/AuthorizationData.cs))

Now you need to authorize and get the final token. This is another POST request with JSON data to `https://xsts.auth.xboxlive.com/xsts/authorize`:

```json
{
   "RelyingParty": "http://xboxlive.com",
   "TokenType": "JWT",
   "Properties": {
      "SandboxID": "RETAIL",
      "UserTokens": ["<token you got from the previous response>"]
   }
}
```

(DTO: [AuthorizationRequest](GameFinder.StoreHandlers.Xbox/DTO/AuthorizationRequest.cs))

The response is of course also in JSON:

```json
{
   "DisplayClaims": {
      "xui": [
         {
            "uhs": "",
            "usr": "",
            "utr": "",
            "prv": "",
            "xid": "THIS IS THE XUID TOKEN",
            "gtg": ""
         }
      ]
   }
}
```

(DTO: [AuthorizationData](GameFinder.StoreHandlers.Xbox/DTO/AuthorizationData.cs))

After all this requesting you finally have the xuid you can use in the constructor.

## Contributing

See [CONTRIBUTING](CONTRIBUTING.md) for more information.

## License

See [LICENSE](LICENSE) for more information.
