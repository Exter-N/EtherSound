namespace Reactivity
{
    public interface IRx
    {
        bool Update();
    }

    public interface IRx<out T> : IRx
    {
        T Value { get; }

        event RxValueChanged<T> ValueChanged;
    }
}
