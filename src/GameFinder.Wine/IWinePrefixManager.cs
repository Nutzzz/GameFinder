using System.Collections.Generic;
using FluentResults;
using GameFinder.Common;
using JetBrains.Annotations;

namespace GameFinder.Wine;

/// <summary>
/// Implementation for wine prefix managers.
/// </summary>
/// <typeparam name="TPrefix"></typeparam>
[PublicAPI]
public interface IWinePrefixManager<TPrefix> where TPrefix : AWinePrefix
{
    /// <summary>
    /// Finds all prefixes associated with this manager.
    /// </summary>
    /// <returns></returns>
    IEnumerable<Result<TPrefix>> FindPrefixes();
}
