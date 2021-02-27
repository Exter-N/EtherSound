using System;
using System.Collections.Generic;
using Reactivity.Impl;

namespace Reactivity
{
    public static class KeyedRx
    {
        public static IWritableKeyedRx<TKey, T> Data<TKey, T>((Func<TKey, T>, Action<TKey, T>) storage, Func<TKey, T> initialValue, IEqualityComparer<T> equalityComparer = null)
        {
            return new DataKeyedRx<TKey, T>(initialValue, storage, equalityComparer);
        }

        public static IWritableKeyedRx<TKey, T> TwoWayBound<TKey, T>((Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T> compute, Action<TKey, T> assign, IEqualityComparer<T> equalityComparer = null)
        {
            return new TwoWayBoundKeyedRx<TKey, T>(compute, assign, cacheStorage, equalityComparer);
        }

        public static IWritableKeyedRx<TKey, T> TwoWayBound<TKey, T, T0>(IWritableKeyedRx<TKey, T0> source, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Converter<T0, T> convert, Converter<T, T0> convertBack, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableKeyedRx<TKey, T> rx = new TwoWayBoundKeyedRx<TKey, T>(key => convert(source[key]), (key, value) => source[key] = convertBack(value), cacheStorage, equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> TwoWayBound<TKey, T, T0>(IWritableKeyedRx<TKey, T0> source, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Converter<T0, T> convert, Func<T, T0, T0> convertBack, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableKeyedRx<TKey, T> rx = new TwoWayBoundKeyedRx<TKey, T>(key => convert(source[key]), (key, value) => source[key] = convertBack(value, source[key]), cacheStorage, equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> TwoWayBound<TKey, T, T0>(IWritableKeyedRx<TKey, T0> source, (Func<TKey, T>, Action<TKey, T>) cacheStorage, ITwoWayConverter<T, T0> converter, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableKeyedRx<TKey, T> rx = new TwoWayBoundKeyedRx<TKey, T>(key => converter.Convert(source[key]), (key, value) => source[key] = converter.ConvertBack(value, source[key]), cacheStorage, equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> TwoWayBound<TKey, T, T0>(IWritableKeyedRx<TKey, T0> source, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T0, T> convert, Func<TKey, T, T0> convertBack, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableKeyedRx<TKey, T> rx = new TwoWayBoundKeyedRx<TKey, T>(key => convert(key, source[key]), (key, value) => source[key] = convertBack(key, value), cacheStorage, equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> TwoWayBound<TKey, T, T0>(IWritableKeyedRx<TKey, T0> source, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T0, T> convert, Func<TKey, T, T0, T0> convertBack, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableKeyedRx<TKey, T> rx = new TwoWayBoundKeyedRx<TKey, T>(key => convert(key, source[key]), (key, value) => source[key] = convertBack(key, value, source[key]), cacheStorage, equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IWritableKeyedRx<TKey, T> TwoWayBound<TKey, T, T0>(IWritableKeyedRx<TKey, T0> source, (Func<TKey, T>, Action<TKey, T>) cacheStorage, IKeyedTwoWayConverter<TKey, T, T0> converter, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableKeyedRx<TKey, T> rx = new TwoWayBoundKeyedRx<TKey, T>(key => converter.Convert(key, source[key]), (key, value) => source[key] = converter.ConvertBack(key, value, source[key]), cacheStorage, equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T>((Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            return new ComputedKeyedRx<TKey, T>(compute, cacheStorage, equalityComparer);
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T, TKey1, T1>(IKeyedRx<TKey1, T1> source1, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T1, T> compute, IEqualityComparer<T> equalityComparer = null) where TKey : TKey1
        {
            IKeyedRx<TKey, T> rx = new ComputedKeyedRx<TKey, T>(key => compute(key, source1[key]), cacheStorage, equalityComparer);
            source1.Watch(Adapt<TKey1, TKey>(rx.Update));

            return rx;
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T, TKey1, T1, TKey2, T2>(IKeyedRx<TKey1, T1> source1, IKeyedRx<TKey2, T2> source2, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T1, T2, T> compute, IEqualityComparer<T> equalityComparer = null) where TKey : TKey1, TKey2
        {
            IKeyedRx<TKey, T> rx = new ComputedKeyedRx<TKey, T>(key => compute(key, source1[key], source2[key]), cacheStorage, equalityComparer);
            source1.Watch(Adapt<TKey1, TKey>(rx.Update));
            source2.Watch(Adapt<TKey2, TKey>(rx.Update));

            return rx;
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T, TKey1, T1, TKey2, T2, TKey3, T3>(IKeyedRx<TKey1, T1> source1, IKeyedRx<TKey2, T2> source2, IKeyedRx<TKey3, T3> source3, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T1, T2, T3, T> compute, IEqualityComparer<T> equalityComparer = null) where TKey : TKey1, TKey2, TKey3
        {
            IKeyedRx<TKey, T> rx = new ComputedKeyedRx<TKey, T>(key => compute(key, source1[key], source2[key], source3[key]), cacheStorage, equalityComparer);
            source1.Watch(Adapt<TKey1, TKey>(rx.Update));
            source2.Watch(Adapt<TKey2, TKey>(rx.Update));
            source3.Watch(Adapt<TKey3, TKey>(rx.Update));

            return rx;
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T, TKey1, T1, TKey2, T2, TKey3, T3, TKey4, T4>(IKeyedRx<TKey1, T1> source1, IKeyedRx<TKey2, T2> source2, IKeyedRx<TKey3, T3> source3, IKeyedRx<TKey4, T4> source4, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T1, T2, T3, T4, T> compute, IEqualityComparer<T> equalityComparer = null) where TKey : TKey1, TKey2, TKey3, TKey4
        {
            IKeyedRx<TKey, T> rx = new ComputedKeyedRx<TKey, T>(key => compute(key, source1[key], source2[key], source3[key], source4[key]), cacheStorage, equalityComparer);
            source1.Watch(Adapt<TKey1, TKey>(rx.Update));
            source2.Watch(Adapt<TKey2, TKey>(rx.Update));
            source3.Watch(Adapt<TKey3, TKey>(rx.Update));
            source4.Watch(Adapt<TKey4, TKey>(rx.Update));

            return rx;
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T, TKey1, T1, TKey2, T2, TKey3, T3, TKey4, T4, TKey5, T5>(IKeyedRx<TKey1, T1> source1, IKeyedRx<TKey2, T2> source2, IKeyedRx<TKey3, T3> source3, IKeyedRx<TKey4, T4> source4, IKeyedRx<TKey5, T5> source5, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T1, T2, T3, T4, T5, T> compute, IEqualityComparer<T> equalityComparer = null) where TKey : TKey1, TKey2, TKey3, TKey4, TKey5
        {
            IKeyedRx<TKey, T> rx = new ComputedKeyedRx<TKey, T>(key => compute(key, source1[key], source2[key], source3[key], source4[key], source5[key]), cacheStorage, equalityComparer);
            source1.Watch(Adapt<TKey1, TKey>(rx.Update));
            source2.Watch(Adapt<TKey2, TKey>(rx.Update));
            source3.Watch(Adapt<TKey3, TKey>(rx.Update));
            source4.Watch(Adapt<TKey4, TKey>(rx.Update));
            source5.Watch(Adapt<TKey5, TKey>(rx.Update));

            return rx;
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T, TKey1, T1, TKey2, T2, TKey3, T3, TKey4, T4, TKey5, T5, TKey6, T6>(IKeyedRx<TKey1, T1> source1, IKeyedRx<TKey2, T2> source2, IKeyedRx<TKey3, T3> source3, IKeyedRx<TKey4, T4> source4, IKeyedRx<TKey5, T5> source5, IKeyedRx<TKey6, T6> source6, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T1, T2, T3, T4, T5, T6, T> compute, IEqualityComparer<T> equalityComparer = null) where TKey : TKey1, TKey2, TKey3, TKey4, TKey5, TKey6
        {
            IKeyedRx<TKey, T> rx = new ComputedKeyedRx<TKey, T>(key => compute(key, source1[key], source2[key], source3[key], source4[key], source5[key], source6[key]), cacheStorage, equalityComparer);
            source1.Watch(Adapt<TKey1, TKey>(rx.Update));
            source2.Watch(Adapt<TKey2, TKey>(rx.Update));
            source3.Watch(Adapt<TKey3, TKey>(rx.Update));
            source4.Watch(Adapt<TKey4, TKey>(rx.Update));
            source5.Watch(Adapt<TKey5, TKey>(rx.Update));
            source6.Watch(Adapt<TKey6, TKey>(rx.Update));

            return rx;
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T, TKey1, T1, TKey2, T2, TKey3, T3, TKey4, T4, TKey5, T5, TKey6, T6, TKey7, T7>(IKeyedRx<TKey1, T1> source1, IKeyedRx<TKey2, T2> source2, IKeyedRx<TKey3, T3> source3, IKeyedRx<TKey4, T4> source4, IKeyedRx<TKey5, T5> source5, IKeyedRx<TKey6, T6> source6, IKeyedRx<TKey7, T7> source7, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T1, T2, T3, T4, T5, T6, T7, T> compute, IEqualityComparer<T> equalityComparer = null) where TKey : TKey1, TKey2, TKey3, TKey4, TKey5, TKey6, TKey7
        {
            IKeyedRx<TKey, T> rx = new ComputedKeyedRx<TKey, T>(key => compute(key, source1[key], source2[key], source3[key], source4[key], source5[key], source6[key], source7[key]), cacheStorage, equalityComparer);
            source1.Watch(Adapt<TKey1, TKey>(rx.Update));
            source2.Watch(Adapt<TKey2, TKey>(rx.Update));
            source3.Watch(Adapt<TKey3, TKey>(rx.Update));
            source4.Watch(Adapt<TKey4, TKey>(rx.Update));
            source5.Watch(Adapt<TKey5, TKey>(rx.Update));
            source6.Watch(Adapt<TKey6, TKey>(rx.Update));
            source7.Watch(Adapt<TKey7, TKey>(rx.Update));

            return rx;
        }

        public static IKeyedRx<TKey, T> Computed<TKey, T, TKey1, T1, TKey2, T2, TKey3, T3, TKey4, T4, TKey5, T5, TKey6, T6, TKey7, T7, TKey8, T8>(IKeyedRx<TKey1, T1> source1, IKeyedRx<TKey2, T2> source2, IKeyedRx<TKey3, T3> source3, IKeyedRx<TKey4, T4> source4, IKeyedRx<TKey5, T5> source5, IKeyedRx<TKey6, T6> source6, IKeyedRx<TKey7, T7> source7, IKeyedRx<TKey8, T8> source8, (Func<TKey, T>, Action<TKey, T>) cacheStorage, Func<TKey, T1, T2, T3, T4, T5, T6, T7, T8, T> compute, IEqualityComparer<T> equalityComparer = null) where TKey : TKey1, TKey2, TKey3, TKey4, TKey5, TKey6, TKey7, TKey8
        {
            IKeyedRx<TKey, T> rx = new ComputedKeyedRx<TKey, T>(key => compute(key, source1[key], source2[key], source3[key], source4[key], source5[key], source6[key], source7[key], source8[key]), cacheStorage, equalityComparer);
            source1.Watch(Adapt<TKey1, TKey>(rx.Update));
            source2.Watch(Adapt<TKey2, TKey>(rx.Update));
            source3.Watch(Adapt<TKey3, TKey>(rx.Update));
            source4.Watch(Adapt<TKey4, TKey>(rx.Update));
            source5.Watch(Adapt<TKey5, TKey>(rx.Update));
            source6.Watch(Adapt<TKey6, TKey>(rx.Update));
            source7.Watch(Adapt<TKey7, TKey>(rx.Update));
            source8.Watch(Adapt<TKey8, TKey>(rx.Update));

            return rx;
        }

        private static Func<TKey0, bool> Adapt<TKey0, TKey>(Func<TKey, bool> update) where TKey : TKey0
        {
            if (typeof(TKey0) == typeof(TKey))
            {
                return (Func<TKey0, bool>)(object)update;
            }

            return (TKey0 key) => (null == key || key is TKey) && update((TKey)key);
        }
    }
}
