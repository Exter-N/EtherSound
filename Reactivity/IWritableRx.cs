namespace Reactivity
{
    public interface IWritableRx : IRx
    {
    }

    public interface IWritableRx<T> : IRx<T>, IWritableRx
    {
        new T Value { get; set; }
    }
}
