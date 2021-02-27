using System;
using System.Collections.Generic;

namespace Reactivity.Impl
{
    internal class TwoWayBoundRx<T> : IWritableRx<T>
    {
        readonly IEqualityComparer<T> equalityComparer;
        readonly Func<T> compute;
        readonly Action<T> assign;
        bool shouldUpdate;
        T cachedData;

        public T Value
        {
            get => cachedData;
            set
            {
                shouldUpdate = true;
                assign(value);
                if (shouldUpdate)
                {
                    Update();
                }
            }
        }

        public event RxValueChanged<T> ValueChanged;

        internal TwoWayBoundRx(Func<T> compute, Action<T> assign, IEqualityComparer<T> equalityComparer = null)
        {
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            this.compute = compute;
            this.assign = assign;
            shouldUpdate = false;
            cachedData = compute();
        }

        public bool Update()
        {
            shouldUpdate = false;
            T value = compute();
            if (equalityComparer.Equals(value, cachedData))
            {
                return false;
            }
            T previousValue = cachedData;
            cachedData = value;
            ValueChanged?.Invoke(this, value, previousValue);

            return true;
        }
    }
}
