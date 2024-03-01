using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentResults;
using JetBrains.Annotations;

namespace GameFinder.Common;

/// <summary>
/// Utility extensions for GameFinder.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Fully enumerates <paramref name="results"/> and splits the results into
    /// two separate arrays.
    /// </summary>
    /// <param name="results"></param>
    /// <typeparam name="TGame"></typeparam>
    /// <returns></returns>
    public static (TGame[] games, IList<IError>[]) SplitResults<TGame>(
        [InstantHandle] this IEnumerable<Result<TGame>> results)
        where TGame : class, IGame
    {
        var allResults = results.ToArray();

        var games = allResults
            .Where(x => x.IsSuccess)
            .Select(x => x.Value)
            .ToArray();

        var errors = allResults
            .Where(x => x.IsFailed)
            .Select(x => x.Errors)
            .ToArray();

        return (games, errors);
    }

    /// <summary>
    /// Custom <see cref="Enumerable.ToDictionary{TSource,TKey}(System.Collections.Generic.IEnumerable{TSource},System.Func{TSource,TKey})"/>
    /// function that skips duplicate keys.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="keySelector"></param>
    /// <param name="elementSelector"></param>
    /// <param name="comparer"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    /// <returns></returns>
    public static IReadOnlyDictionary<TKey, TElement> CustomToDictionary<TSource, TKey, TElement>(
        [InstantHandle] this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TElement>(comparer);

        foreach (var element in source)
        {
            var key = keySelector(element);
            if (dictionary.ContainsKey(key)) continue;

            dictionary.Add(key, elementSelector(element));
        }

        return dictionary;
    }

    /// <summary>
    /// Returns <c>true</c> if the result is of type <typeparamref name="TGame"/>.
    /// </summary>
    /// <param name="result"></param>
    /// <typeparam name="TGame"></typeparam>
    /// <returns></returns>
    public static bool IsGame<TGame>(this Result<TGame> result)
        where TGame : class, IGame
    {
        return result.IsSuccess;
    }

    /// <summary>
    /// Returns <c>true</c> if the result IsFailed.
    /// </summary>
    /// <param name="result"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool IsError<T>(this Result<T> result)
    {
        return result.IsFailed;
    }

    /// <summary>
    /// Returns the <typeparamref name="TGame"/> part of the result. This can throw if
    /// the result is not of type <typeparamref name="TGame"/>. Use <see cref="TryGetGame{TGame}"/>
    /// instead.
    /// </summary>
    /// <param name="result"></param>
    /// <typeparam name="TGame"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is not of type <typeparamref name="TGame"/>.
    /// TODO: Is this true for FluentResults?
    /// </exception>
    public static TGame AsGame<TGame>(this Result<TGame> result)
        where TGame : class, IGame
    {
        return result.Value;
    }

    /// <summary>
    /// Returns the <see cref="IError"/> parts of the result. This can
    /// throw if the result is not of type <see cref="IError"/>. Use
    /// <see cref="TryGetError{T}"/> instead.
    /// </summary>
    /// <param name="result"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the result is not of type <see cref="IError"/>.
    /// TODO: Is this true for FluentResults?
    /// </exception>
    public static IList<IError> AsErrors<T>(this Result<T> result)
    {
        return result.Errors;
    }

    /// <summary>
    /// Returns the <typeparamref name="TGame"/> part of the result using the try-get
    /// pattern.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="game"></param>
    /// <typeparam name="TGame"></typeparam>
    /// <returns></returns>
    public static bool TryGetGame<TGame>(this Result<TGame> result,
        [MaybeNullWhen(false)] out TGame game)
        where TGame : class, IGame
    {
        game = null;
        if (!result.IsGame()) return false;

        game = result.AsGame();
        return true;
    }

    /// <summary>
    /// Returns the <see cref="IError"/> part of the result using the
    /// try-get pattern.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="errors"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryGetErrors<T>(this Result<T> result, out IList<IError> errors)
    {
        errors = new List<IError>();
        if (!result.IsError()) return false;

        errors = result.AsErrors();
        return true;
    }
}
