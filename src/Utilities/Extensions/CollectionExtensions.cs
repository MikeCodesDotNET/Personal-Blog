using System;
using System.Collections.Generic;


namespace MikeCodesDotNET.Utilities.Extensions;

internal static class CollectionExtensions
{
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> data)
    {
        foreach (var item in data)
        {
            collection.Add(item);
        }
    }

    public static void TransformAndAddRange<TTarget, TInput>(
        this ICollection<TTarget> collection,
        IEnumerable<TInput> inputData,
        Func<TInput, TTarget> transformer)
    {
        if (inputData == null)
            return;

        foreach (var item in inputData)
        {
            collection.Add(transformer.Invoke(item));
        }
    }

}
