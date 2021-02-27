using Reactivity.Impl;
using System;
using System.Runtime.CompilerServices;

namespace Reactivity
{
    public static class AttachedStorage<TKey> where TKey : class
    {
        public static (Func<TKey, T>, Action<TKey, T>) Create<T>(Func<TKey, T> defaultValue = null)
        {
            ConditionalWeakTable<TKey, Box<T>> wTable = new ConditionalWeakTable<TKey, Box<T>>();
            if (null == defaultValue)
            {
                return (key => wTable.GetOrCreateValue(key).Value, (key, value) => wTable.GetOrCreateValue(key).Value = value);
            }
            else
            {
                ConditionalWeakTable<TKey, Box<T>>.CreateValueCallback createWithDefault = key => new Box<T>(defaultValue(key));

                return (key => wTable.GetValue(key, createWithDefault).Value, (key, value) => wTable.GetOrCreateValue(key).Value = value);
            }
        }
    }
}
