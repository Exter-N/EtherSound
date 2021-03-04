using System;
using System.Diagnostics;
using System.Threading;

namespace WASCap
{
    public class CaptureSession : IDisposable
    {
        readonly string args;
        readonly ControlStructure ctlS;
        readonly IConsole console;
        Process worker;
        bool disposed;
        int restartDelay;

        public IConsole Console => console;

        public event EventHandler Starting;

        internal CaptureSession(string args, ControlStructure ctlS, IConsole console, EventHandler starting)
        {
            this.args = args;
            this.ctlS = ctlS;
            this.console = console;
            worker = null;
            disposed = false;
            restartDelay = 1000;
            if (null != starting)
            {
                Starting += starting;
            }
            Start();
        }
        ~CaptureSession() => Dispose(false);

        void Start()
        {
            OnStarting(EventArgs.Empty);
            lock (this)
            {
                if (disposed || worker != null)
                {
                    return;
                }
                worker = new Process();
            }
            worker.StartInfo = new ProcessStartInfo
            {
                FileName = Driver.ExecutablePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = null != console,
            };
            worker.Exited += ChildExited;
            if (null != console)
            {
                worker.ErrorDataReceived += ChildErrorDataReceived;
            }
            worker.EnableRaisingEvents = true;
            if (null != ctlS)
            {
                ctlS.AbortRequested = false;
            }
            restartDelay = 1000;
            worker.Start();
            if (null != console)
            {
                worker.BeginErrorReadLine();
            }
        }

        void Log(string entry)
        {
            if (null == console)
            {
                return;
            }
            lock (console)
            {
                console.Log(entry);
            }
        }

        void ChildExited(object sender, EventArgs eventArgs)
        {
            if (worker != null)
            {
                string logEntry;
                try
                {
                    logEntry = string.Format("[{0:HH:mm:ss}] Exited with exit code {1}", worker.ExitTime, worker.ExitCode);
                }
                catch
                {
                    logEntry = string.Format("[{0:HH:mm:ss}] Exited with unknown exit code", DateTime.Now);
                }
                Log(logEntry);
                worker.Dispose();
                worker = null;
            }

            if (!disposed)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    Thread.Sleep(restartDelay);
                    Start();
                });
            }
        }

        void ChildErrorDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                return;
            }
            Log(string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, eventArgs.Data.Trim()));
        }

        static void Stop(Process child, bool wait)
        {
            if (wait)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        child.WaitForExit(5000);
                        child.Kill();
                        child.WaitForExit();
                    }
                    catch
                    {
                        // This block intentionally left blank.
                    }
                });
            }
            else
            {
                child.Kill();
                child.WaitForExit();
            }
        }

        void Stop()
        {
            Process currentChild = worker;
            if (currentChild == null)
            {
                return;
            }

            if (ctlS != null)
            {
                ctlS.AbortRequested = true;
                Stop(currentChild, !disposed);
            }
            else
            {
                Stop(currentChild, false);
            }
        }

        public void Restart()
        {
            restartDelay = 0;
            Stop();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Stop();
            if (worker != null)
            {
                worker.EnableRaisingEvents = false;
                worker.Exited -= ChildExited;
                worker.Dispose();
            }
        }

        protected virtual void OnStarting(EventArgs e)
        {
            Starting?.Invoke(this, e);
        }
    }
}
