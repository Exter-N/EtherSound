using System;
using System.Collections.Generic;
using System.Linq;

namespace WASCap
{
    static class EnumerableExtensions
    {
        public static bool ExistsZip<T, TSecond>(this IEnumerable<T> first, IEnumerable<TSecond> second, Func<T, TSecond, bool> predicate)
        {
            return first.Zip(second, predicate).FirstOrDefault(x => x);
        }
    }
}
