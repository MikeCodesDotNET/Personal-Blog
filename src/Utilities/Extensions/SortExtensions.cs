using MikeCodesDotNET.Utilities.Comparers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MikeCodesDotNET.Utilities.Extensions;

public static class SortExtensions
{
    //  Sorts an IList<T> in place.
    public static void Sort<T>(this IList<T> list, Comparison<T> comparison)
    {
        ArrayList.Adapter((IList)list).Sort(new ComparisonComparer<T>(comparison));
    }

    // Sorts in IList<T> in place, when T is IComparable<T>
    public static void Sort<T>(this IList<T> list) where T : IComparable<T>
    {
        Comparison<T> comparison = (l, r) => l.CompareTo(r);
        Sort(list, comparison);
    }

    // Convenience method on IEnumerable<T> to allow passing of a
    // Comparison<T> delegate to the OrderBy method.
    public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> list, Comparison<T> comparison)
    {
        return list.OrderBy(t => t, new ComparisonComparer<T>(comparison));
    }

}
