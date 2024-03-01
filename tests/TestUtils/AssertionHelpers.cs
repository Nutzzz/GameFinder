using FluentResults;
using GameFinder.Common;

namespace TestUtils;

public static class AssertionHelpers
{
    public static IEnumerable<TGame> ShouldOnlyBeGames<TGame>(this ICollection<Result<TGame>> results)
        where TGame : class, IGame
    {
        results.Should().AllSatisfy(result =>
        {
            result.IsGame().Should().BeTrue(result.IsError() ? result.AsErrors()[0].Message : string.Empty);
            result.IsError().Should().BeFalse();
        });

        return results.Select(result => result.AsGame());
    }

    private static IList<IError> ShouldOnlyBeOneError<TGame>(
        this ICollection<Result<TGame>> results)
        where TGame : class, IGame
    {
        results.Should().ContainSingle();

        var result = results.First();
        result.IsError().Should().BeTrue();
        result.IsGame().Should().BeFalse();

        return result.AsErrors();
    }

    public static IList<IError> ShouldOnlyBeOneError<TGame, TId>(
        this AHandler<TGame, TId> handler)
        where TGame : class, IGame
        where TId : notnull
    {
        var results = handler.FindAllGames().ToArray();
        return results.ShouldOnlyBeOneError();
    }

    public static void ShouldFindAllGames<TGame, TId>(
        this AHandler<TGame, TId> handler,
        IEnumerable<TGame> expectedGames)
        where TGame : class, IGame
        where TId : notnull
    {
        var results = handler.FindAllGames().ToArray();
        var games = results.ShouldOnlyBeGames();

        games.Should().Equal(expectedGames);
    }

    public static void ShouldFindAllGamesById<TGame, TId>(
        this AHandler<TGame, TId> handler,
        ICollection<TGame> expectedGames,
        Func<TGame, TId> keySelector)
        where TGame : class, IGame
        where TId : notnull
    {
        var results = handler.FindAllGamesById(out var errors);
        errors.Should().BeEmpty();

        results.Should().ContainKeys(expectedGames.Select(keySelector));
        results.Should().ContainValues(expectedGames);
    }

    public static void ShouldFindAllInterfacesGames<TGame, TId>(
        this AHandler<TGame, TId> handler,
        IEnumerable<TGame> expectedGames)
        where TGame : class, IGame
        where TId : notnull
    {
        var results = handler.FindAllInterfaceGames().ToArray();
        var games = results.ShouldOnlyBeGames();

        games.Should().AllBeOfType<TGame>().Which.Should().Equal(expectedGames);
    }
}
