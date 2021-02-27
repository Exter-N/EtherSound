using System;

namespace Reactivity
{
    public static class RxExtensions
    {
        public static IRx<T> Watch<T>(this IRx<T> rx, RxValueChanged<T> valueChanged)
        {
            rx.ValueChanged += valueChanged;

            return rx;
        }

        public static IRx<T> Watch<T>(this IRx<T> rx, Action valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged();

            return rx;
        }

        public static IRx<T> Watch<T>(this IRx<T> rx, Func<bool> valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged();

            return rx;
        }

        public static IRx<T> Watch<T>(this IRx<T> rx, Action<T> valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged(newValue);

            return rx;
        }

        public static IRx<T> Watch<T>(this IRx<T> rx, Action<T, T> valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged(newValue, oldValue);

            return rx;
        }

        public static IRx<T> Watch<T>(this IRx<T> rx, Action<IRx<T>, T> valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged(source, newValue);

            return rx;
        }

        public static IWritableRx<T> Watch<T>(this IWritableRx<T> rx, RxValueChanged<T> valueChanged)
        {
            rx.ValueChanged += valueChanged;

            return rx;
        }

        public static IWritableRx<T> Watch<T>(this IWritableRx<T> rx, Action valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged();

            return rx;
        }

        public static IWritableRx<T> Watch<T>(this IWritableRx<T> rx, Action<T> valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged(newValue);

            return rx;
        }

        public static IWritableRx<T> Watch<T>(this IWritableRx<T> rx, Action<T, T> valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged(newValue, oldValue);

            return rx;
        }

        public static IWritableRx<T> Watch<T>(this IWritableRx<T> rx, Action<IRx<T>, T> valueChanged)
        {
            rx.ValueChanged += (source, newValue, oldValue) => valueChanged(source, newValue);

            return rx;
        }

        public static IKeyedRx<TKey, T> Watch<TKey, T>(this IKeyedRx<TKey, T> rx, KeyedRxValueChanged<TKey, T> valueChanged)
        {
            rx.ValueChanged += valueChanged;

            return rx;
        }

        public static IKeyedRx<TKey, T> Watch<TKey, T>(this IKeyedRx<TKey, T> rx, Action<TKey> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(key);

            return rx;
        }

        public static IKeyedRx<TKey, T> Watch<TKey, T>(this IKeyedRx<TKey, T> rx, Func<TKey, bool> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(key);

            return rx;
        }

        public static IKeyedRx<TKey, T> Watch<TKey, T>(this IKeyedRx<TKey, T> rx, Action<TKey, T> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(key, newValue);

            return rx;
        }

        public static IKeyedRx<TKey, T> Watch<TKey, T>(this IKeyedRx<TKey, T> rx, Action<TKey, T, T> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(key, newValue, oldValue);

            return rx;
        }

        public static IKeyedRx<TKey, T> Watch<TKey, T>(this IKeyedRx<TKey, T> rx, Action<IKeyedRx<TKey, T>, TKey, T> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(source, key, newValue);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> Watch<TKey, T>(this IWritableKeyedRx<TKey, T> rx, KeyedRxValueChanged<TKey, T> valueChanged)
        {
            rx.ValueChanged += valueChanged;

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> Watch<TKey, T>(this IWritableKeyedRx<TKey, T> rx, Action<TKey> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(key);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> Watch<TKey, T>(this IWritableKeyedRx<TKey, T> rx, Action<TKey, T> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(key, newValue);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> Watch<TKey, T>(this IWritableKeyedRx<TKey, T> rx, Action<TKey, T, T> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(key, newValue, oldValue);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> Watch<TKey, T>(this IWritableKeyedRx<TKey, T> rx, Action<IKeyedRx<TKey, T>, TKey, T> valueChanged)
        {
            rx.ValueChanged += (source, key, newValue, oldValue) => valueChanged(source, key, newValue);

            return rx;
        }
    }
}
