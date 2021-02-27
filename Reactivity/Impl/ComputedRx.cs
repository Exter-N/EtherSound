using System;
using System.Collections.Generic;

namespace Reactivity.Impl
{
    internal class ComputedRx<T> : IRx<T>
    {
        readonly IEqualityComparer<T> equalityComparer;
        readonly Func<T> compute;
        T cachedData;

        public T Value => cachedData;

        public event RxValueChanged<T> ValueChanged;

        internal ComputedRx(Func<T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            this.compute = compute;
            cachedData = compute();
        }

        public bool Update()
        {
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
