using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EtherSound.ViewModel
{
    public class CollectionPropertyChangedEventArgs<T> : PropertyChangedEventArgs
    {
        readonly ISet<T> removed;
        readonly ISet<T> kept;
        readonly ISet<T> added;

        public ISet<T> Removed => removed;
        public ISet<T> Kept => kept;
        public ISet<T> Added => added;

        public (ISet<T> removed, ISet<T> kept, ISet<T> added) Diff => (removed, kept, added);

        public CollectionPropertyChangedEventArgs(string propertyName, (ISet<T>, ISet<T>, ISet<T>) diff) : base(propertyName)
        {
            (removed, kept, added) = diff;
        }
    }
}
