using System.Collections.Generic;
using GameFinder.Common;
using JetBrains.Annotations;
using OneOf;

namespace GameCollector.Wine;

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
    IEnumerable<OneOf<TPrefix, ErrorMessage>> FindPrefixes();
}
