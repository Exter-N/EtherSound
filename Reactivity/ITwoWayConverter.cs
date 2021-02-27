namespace Reactivity
{
    public interface ITwoWayConverter<T, T0>
    {
        T Convert(T0 value);

        T0 ConvertBack(T value, T0 oldValue);
    }
}
