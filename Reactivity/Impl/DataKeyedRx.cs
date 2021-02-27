using System;
using System.Collections.Generic;

namespace Reactivity.Impl
{
    internal class DataKeyedRx<TKey, T> : IWritableKeyedRx<TKey, T>
    {
        readonly IEqualityComparer<T> equalityComparer;
        readonly Func<TKey, T> fetch;
        readonly Action<TKey, T> store;
        readonly Func<TKey, T> initialValue;

        public T this[TKey key]
        {
            get => fetch(key);
            set
            {
                KeyedRxValueChanged<TKey, T> valueChanged = ValueChanged;
                if (null == valueChanged)
                {
                    store(key, value);

                    return;
                }
                T previousValue = fetch(key);
                if (equalityComparer.Equals(value, previousValue))
                {
                    return;
                }
                store(key, value);
                valueChanged(this, key, value, previousValue);
            }
        }

        public event KeyedRxValueChanged<TKey, T> ValueChanged;

        internal DataKeyedRx(Func<TKey, T> initialValue, (Func<TKey, T>, Action<TKey, T>) storage, IEqualityComparer<T> equalityComparer = null)
        {
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            (fetch, store) = storage;
            this.initialValue = initialValue ?? fetch;
        }

        public void Initialize(TKey key)
        {
            store(key, initialValue(key));
        }

        bool IKeyedRx<TKey>.Update(TKey key)
        {
            return false;
        }
    }
}
