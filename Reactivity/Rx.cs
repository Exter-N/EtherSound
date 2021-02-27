using System;
using System.Collections.Generic;
using Reactivity.Impl;

namespace Reactivity
{
    public static class Rx
    {
        public static IWritableRx<T> Data<T>(T initialValue, IEqualityComparer<T> equalityComparer = null)
        {
            return new DataRx<T>(initialValue, equalityComparer);
        }

        public static IWritableRx<T> TwoWayBound<T>(Func<T> compute, Action<T> assign, IEqualityComparer<T> equalityComparer = null)
        {
            return new TwoWayBoundRx<T>(compute, assign, equalityComparer);
        }

        public static IWritableRx<T> TwoWayBound<T, T0>(IWritableRx<T0> source, Converter<T0, T> convert, Converter<T, T0> convertBack, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableRx<T> rx = new TwoWayBoundRx<T>(() => convert(source.Value), value => source.Value = convertBack(value), equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IWritableRx<T> TwoWayBound<T, T0>(IWritableRx<T0> source, Converter<T0, T> convert, Func<T, T0, T0> convertBack, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableRx<T> rx = new TwoWayBoundRx<T>(() => convert(source.Value), value => source.Value = convertBack(value, source.Value), equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IWritableRx<T> TwoWayBound<T, T0>(IWritableRx<T0> source, ITwoWayConverter<T, T0> converter, IEqualityComparer<T> equalityComparer = null)
        {
            IWritableRx<T> rx = new TwoWayBoundRx<T>(() => converter.Convert(source.Value), value => source.Value = converter.ConvertBack(value, source.Value), equalityComparer);
            source.Watch(rx.Update);

            return rx;
        }

        public static IRx<T> Computed<T>(Func<T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            return new ComputedRx<T>(compute, equalityComparer);
        }

        public static IRx<T> Computed<T, T1>(IRx<T1> source1, Func<T1, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            IRx<T> rx = new ComputedRx<T>(() => compute(source1.Value), equalityComparer);
            source1.Watch(rx.Update);

            return rx;
        }

        public static IRx<T> Computed<T, T1, T2>(IRx<T1> source1, IRx<T2> source2, Func<T1, T2, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            IRx<T> rx = new ComputedRx<T>(() => compute(source1.Value, source2.Value), equalityComparer);
            source1.Watch(rx.Update);
            source2.Watch(rx.Update);

            return rx;
        }

        public static IRx<T> Computed<T, T1, T2, T3>(IRx<T1> source1, IRx<T2> source2, IRx<T3> source3, Func<T1, T2, T3, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            IRx<T> rx = new ComputedRx<T>(() => compute(source1.Value, source2.Value, source3.Value), equalityComparer);
            source1.Watch(rx.Update);
            source2.Watch(rx.Update);
            source3.Watch(rx.Update);

            return rx;
        }

        public static IRx<T> Computed<T, T1, T2, T3, T4>(IRx<T1> source1, IRx<T2> source2, IRx<T3> source3, IRx<T4> source4, Func<T1, T2, T3, T4, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            IRx<T> rx = new ComputedRx<T>(() => compute(source1.Value, source2.Value, source3.Value, source4.Value), equalityComparer);
            source1.Watch(rx.Update);
            source2.Watch(rx.Update);
            source3.Watch(rx.Update);
            source4.Watch(rx.Update);

            return rx;
        }

        public static IRx<T> Computed<T, T1, T2, T3, T4, T5>(IRx<T1> source1, IRx<T2> source2, IRx<T3> source3, IRx<T4> source4, IRx<T5> source5, Func<T1, T2, T3, T4, T5, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            IRx<T> rx = new ComputedRx<T>(() => compute(source1.Value, source2.Value, source3.Value, source4.Value, source5.Value), equalityComparer);
            source1.Watch(rx.Update);
            source2.Watch(rx.Update);
            source3.Watch(rx.Update);
            source4.Watch(rx.Update);
            source5.Watch(rx.Update);

            return rx;
        }

        public static IRx<T> Computed<T, T1, T2, T3, T4, T5, T6>(IRx<T1> source1, IRx<T2> source2, IRx<T3> source3, IRx<T4> source4, IRx<T5> source5, IRx<T6> source6, Func<T1, T2, T3, T4, T5, T6, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            IRx<T> rx = new ComputedRx<T>(() => compute(source1.Value, source2.Value, source3.Value, source4.Value, source5.Value, source6.Value), equalityComparer);
            source1.Watch(rx.Update);
            source2.Watch(rx.Update);
            source3.Watch(rx.Update);
            source4.Watch(rx.Update);
            source5.Watch(rx.Update);
            source6.Watch(rx.Update);

            return rx;
        }

        public static IRx<T> Computed<T, T1, T2, T3, T4, T5, T6, T7>(IRx<T1> source1, IRx<T2> source2, IRx<T3> source3, IRx<T4> source4, IRx<T5> source5, IRx<T6> source6, IRx<T7> source7, Func<T1, T2, T3, T4, T5, T6, T7, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            IRx<T> rx = new ComputedRx<T>(() => compute(source1.Value, source2.Value, source3.Value, source4.Value, source5.Value, source6.Value, source7.Value), equalityComparer);
            source1.Watch(rx.Update);
            source2.Watch(rx.Update);
            source3.Watch(rx.Update);
            source4.Watch(rx.Update);
            source5.Watch(rx.Update);
            source6.Watch(rx.Update);
            source7.Watch(rx.Update);

            return rx;
        }

        public static IRx<T> Computed<T, T1, T2, T3, T4, T5, T6, T7, T8>(IRx<T1> source1, IRx<T2> source2, IRx<T3> source3, IRx<T4> source4, IRx<T5> source5, IRx<T6> source6, IRx<T7> source7, IRx<T8> source8, Func<T1, T2, T3, T4, T5, T6, T7, T8, T> compute, IEqualityComparer<T> equalityComparer = null)
        {
            IRx<T> rx = new ComputedRx<T>(() => compute(source1.Value, source2.Value, source3.Value, source4.Value, source5.Value, source6.Value, source7.Value, source8.Value), equalityComparer);
            source1.Watch(rx.Update);
            source2.Watch(rx.Update);
            source3.Watch(rx.Update);
            source4.Watch(rx.Update);
            source5.Watch(rx.Update);
            source6.Watch(rx.Update);
            source7.Watch(rx.Update);
            source8.Watch(rx.Update);

            return rx;
        }
    }
}
