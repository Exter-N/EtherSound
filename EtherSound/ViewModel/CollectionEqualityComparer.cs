using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherSound.ViewModel
{
    class CollectionEqualityComparer<T> : IEqualityComparer<ICollection<T>>
    {
        readonly IEqualityComparer<T> elementComparer;

        public CollectionEqualityComparer(IEqualityComparer<T> elementComparer = null)
        {
            this.elementComparer = elementComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(ICollection<T> collX, ICollection<T> collY)
        {
            return ReferenceEquals(collX, collY) || collX.Count == collY.Count && !collX.ExistsZip(collY, (x, y) => !elementComparer.Equals(x, y));
        }

        public int GetHashCode(ICollection<T> coll)
        {
            throw new NotSupportedException();
        }
    }
}
