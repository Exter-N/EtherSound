using EtherSound.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherSound
{
    static class EnumerableExtensions
    {
        public static (ISet<T> removed, ISet<T> kept, ISet<T> added) Diff<T>(this IEnumerable<T> before, IEnumerable<T> after)
        {
            ISet<T> beforeSet = new HashSet<T>(before);
            ISet<T> afterSet = new HashSet<T>(after);

            bool isFromLarger = beforeSet.Count > afterSet.Count;
            ISet<T> inter = isFromLarger ? new HashSet<T>(afterSet) : new HashSet<T>(beforeSet);
            inter.IntersectWith(isFromLarger ? beforeSet : afterSet);

            beforeSet.ExceptWith(inter);
            afterSet.ExceptWith(inter);

            return (beforeSet, inter, afterSet);
        }

        public static (ISet<T> removed, ISet<T> kept, ISet<T> added) Diff<T>(this PropertyChangedEventArgs e)
        {
            return ((CollectionPropertyChangedEventArgs<T>)e).Diff;
        }

        public static bool ExistsZip<T, TSecond>(this IEnumerable<T> first, IEnumerable<TSecond> second, Func<T, TSecond, bool> predicate)
        {
            return first.Zip(second, predicate).FirstOrDefault(x => x);
        }
    }
}
