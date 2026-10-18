namespace Core.Reports;

public static class LinqExtensions
{
    /// <summary>
    /// Метод-розширення для детермінованого вибору топ-N елементів.
    /// Застосовує первинне сортування за спаданням та вторинне для стабільності порядку.
    /// </summary>
    public static IEnumerable<T> TopBy<T, TKey, TSecondaryKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, TSecondaryKey> secondaryKeySelector,
        int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(secondaryKeySelector);

        if (count <= 0)
            return [];

        return source
            .OrderByDescending(keySelector)
            .ThenBy(secondaryKeySelector)
            .Take(count);
    }
}
