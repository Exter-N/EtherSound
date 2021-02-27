namespace Reactivity.Impl
{
    internal sealed class Box<T>
    {
        public T Value { get; set; }

        public Box()
        {
            Value = default;
        }

        public Box(T value)
        {
            Value = value;
        }
    }
}
