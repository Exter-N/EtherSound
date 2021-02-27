namespace Reactivity
{
    public interface IKeyedRx<in TKey>
    {
        void Initialize(TKey key);

        bool Update(TKey key);
    }

    public interface IKeyedRx<TKey, out T> : IKeyedRx<TKey>
    {
        T this[TKey key] { get; }

        event KeyedRxValueChanged<TKey, T> ValueChanged;
    }
}
