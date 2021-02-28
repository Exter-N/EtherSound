using System;
using System.IO;

namespace WASCap
{
    public class WriterConsole : IConsole, IDisposable
    {
        readonly TextWriter writer;

        public WriterConsole(TextWriter writer)
        {
            this.writer = writer ?? throw new ArgumentNullException("writer");
        }

        public void Log(string entry)
        {
            writer.WriteLine(entry);
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }
}
