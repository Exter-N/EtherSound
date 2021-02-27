using System.Collections.Generic;

namespace Reactivity.Impl
{
    internal class DataRx<T> : IWritableRx<T>
    {
        readonly IEqualityComparer<T> equalityComparer;
        T data;

        public T Value
        {
            get => data;
            set
            {
                RxValueChanged<T> valueChanged = ValueChanged;
                if (null == valueChanged)
                {
                    data = value;

                    return;
                }
                if (equalityComparer.Equals(value, data))
                {
                    return;
                }
                T previousValue = data;
                data = value;
                valueChanged(this, value, previousValue);
            }
        }

        public event RxValueChanged<T> ValueChanged;

        internal DataRx(T initialValue, IEqualityComparer<T> equalityComparer = null)
        {
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
            data = initialValue;
        }

        bool IRx.Update()
        {
            return false;
        }
    }
}
