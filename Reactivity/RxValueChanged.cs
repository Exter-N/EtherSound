namespace Reactivity
{
    public delegate void RxValueChanged<in T>(IRx<T> source, T newValue, T oldValue);
}
