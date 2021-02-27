using System;

namespace Reactivity
{
    public static class Storage<TKey>
    {
        public static (Func<TKey, T>, Action<TKey, T>) Create<T>(Func<TKey, T> fetch, Action<TKey, T> store)
        {
            return (fetch, store);
        }
    }
}
