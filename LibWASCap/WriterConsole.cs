using System;
using System.IO;

namespace WASCap
{
    public class WriterConsole : IConsole, IDisposable
    {
        readonly TextWriter writer;

        public WriterConsole(TextWriter writer)
        {
            if (null == writer)
            {
                throw new ArgumentNullException("writer");
            }

            this.writer = writer;
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
