namespace Reactivity
{
    public interface IWritableKeyedRx<in TKey> : IKeyedRx<TKey>
    {
    }

    public interface IWritableKeyedRx<TKey, T> : IKeyedRx<TKey, T>, IWritableKeyedRx<TKey>
    {
        new T this[TKey key] { get; set; }
    }
}
