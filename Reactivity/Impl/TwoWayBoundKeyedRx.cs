using System;
using System.Collections.Generic;

namespace Reactivity.Impl
{
    internal class TwoWayBoundKeyedRx<TKey, T> : IWritableKeyedRx<TKey, T>
    {
        readonly IEqualityComparer<T> equalityComparer;
        readonly Func<TKey, T> compute;
        readonly Action<TKey, T> assign;
        readonly Func<TKey, T> fetch;
        readonly Action<TKey, T> store;
        bool shouldUpdate;

        public T this[TKey key]
        {
            get => fetch(key);
            set
            {
                shouldUpdate = true;
                assign(key, value);
                if (shouldUpdate)
                {
                    Update(key);
                }
            }
        }

        public event KeyedRxValueChanged<TKey, T> ValueChanged;

        internal TwoWayBoundKeyedRx(Func<TKey, T> compute, Action<TKey, T> assign, (Func<TKey, T>, Action<TKey, T>) cacheStorage, IEqualityComparer<T> equalityComparer = null)
        {
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            this.compute = compute;
            this.assign = assign;
            (fetch, store) = cacheStorage;
            shouldUpdate = false;
        }

        public void Initialize(TKey key)
        {
            store(key, compute(key));
        }

        public bool Update(TKey key)
        {
            shouldUpdate = false;
            T value = compute(key);
            T previousValue = fetch(key);
            if (equalityComparer.Equals(value, previousValue))
            {
                return false;
            }
            store(key, value);
            ValueChanged?.Invoke(this, key, value, previousValue);

            return true;
        }
    }
}
