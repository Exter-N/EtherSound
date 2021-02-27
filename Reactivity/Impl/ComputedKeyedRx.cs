using System;
using System.Collections.Generic;

namespace Reactivity.Impl
{
    internal class ComputedKeyedRx<TKey, T> : IKeyedRx<TKey, T>
    {
        readonly IEqualityComparer<T> equalityComparer;
        readonly Func<TKey, T> compute;
        readonly Func<TKey, T> fetch;
        readonly Action<TKey, T> store;

        public T this[TKey key] => fetch(key);

        public event KeyedRxValueChanged<TKey, T> ValueChanged;

        internal ComputedKeyedRx(Func<TKey, T> compute, (Func<TKey, T>, Action<TKey, T>) cacheStorage, IEqualityComparer<T> equalityComparer = null)
        {
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            this.compute = compute;
            (fetch, store) = cacheStorage;
        }

        public void Initialize(TKey key)
        {
            store(key, compute(key));
        }

        public bool Update(TKey key)
        {
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
