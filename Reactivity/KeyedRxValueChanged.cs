namespace Reactivity
{
    public delegate void KeyedRxValueChanged<TKey, in T>(IKeyedRx<TKey, T> source, TKey key, T newValue, T oldValue);
}
