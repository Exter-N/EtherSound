namespace Reactivity
{
    public interface IKeyedTwoWayConverter<TKey, T, T0>
    {
        T Convert(TKey key, T0 value);

        T0 ConvertBack(TKey key, T value, T0 oldValue);
    }
}
