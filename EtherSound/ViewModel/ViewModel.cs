using Reactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace EtherSound.ViewModel
{
    abstract class ViewModel : INotifyPropertyChanged
    {
#if DEBUG
        static readonly Func<object, ISet<string>> GetPropertySet = AttachedStorage<object>.Create(_ => new HashSet<string>()).Item1;
#endif

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler SettingsUpdated;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged<T>(string propertyName, (ISet<T>, ISet<T>, ISet<T>) diff)
        {
            OnPropertyChanged(new CollectionPropertyChangedEventArgs<T>(propertyName, diff));
        }

        protected static IKeyedRx<TKey, T> Register<TKey, T>(IList<IKeyedRx<TKey>> list, string propertyName, IKeyedRx<TKey, T> rx) where TKey : ViewModel
        {
#if DEBUG
            if (null != propertyName)
            {
                ISet<string> propertySet = GetPropertySet(list);
                if (propertySet.Contains(propertyName))
                {
                    throw new InvalidOperationException("Duplicate property " + propertyName);
                }
                propertySet.Add(propertyName);
            }
#endif

            list.Add(rx);

            if (null != propertyName)
            {
                rx.Watch(key => key.OnPropertyChanged(propertyName));
            }

            return rx;
        }

        protected static IWritableKeyedRx<TKey, T> Register<TKey, T>(IList<IKeyedRx<TKey>> list, string propertyName, IWritableKeyedRx<TKey, T> rx) where TKey : ViewModel
        {
            list.Add(rx);

            if (null != propertyName)
            {
                rx.Watch(key => key.OnPropertyChanged(propertyName));
            }

            return rx;
        }

        protected static void Initialize<T>(T key, IList<IKeyedRx<T>> list)
        {
            foreach (IKeyedRx<T> rx in list)
            {
                rx.Initialize(key);
            }
        }

        protected virtual void OnSettingsUpdated(EventArgs e)
        {
            SettingsUpdated?.Invoke(this, e);
        }

        public bool UpdateSettings()
        {
            bool anyChanged = DoUpdateSettings();
            if (anyChanged)
            {
                OnSettingsUpdated(EventArgs.Empty);
            }

            return anyChanged;
        }

        protected abstract bool DoUpdateSettings();
    }
}
