using System;
using System.ComponentModel;

namespace WASCap
{
    public class BufferConsole : IConsole, INotifyPropertyChanged
    {
        string[] buffer;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Capacity { get; }

        public string[] Buffer
        {
            get => buffer;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (buffer == value)
                {
                    return;
                }
                if (value.Length > Capacity)
                {
                    throw new ArgumentException();
                }
                if (Array.IndexOf(value, null) > -1)
                {
                    throw new ArgumentException();
                }
                if (buffer.Length == value.Length && !buffer.ExistsZip(value, (l, r) => l != r))
                {
                    return;
                }
                buffer = value;
                OnPropertyChanged(nameof(Buffer));
                OnPropertyChanged(nameof(ConcatenatedBuffer));
            }
        }

        public string ConcatenatedBuffer => string.Join(Environment.NewLine, buffer);

        public BufferConsole(int capacity)
        {
            Capacity = capacity;
            buffer = new string[0];
        }

        public void Log(string entry)
        {
            string[] oldBuffer = Buffer;
            string[] newBuffer = new string[Math.Min(oldBuffer.Length + 1, Capacity)];
            Array.Copy(oldBuffer, oldBuffer.Length - newBuffer.Length + 1, newBuffer, 0, newBuffer.Length - 1);
            newBuffer[newBuffer.Length - 1] = entry;
            Buffer = newBuffer;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return ConcatenatedBuffer;
        }
    }
}
