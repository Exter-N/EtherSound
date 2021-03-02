using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WASCap
{
    public static class Driver
    {
        public const string ExecutableName = "WASCap.exe";

        static readonly IntPtr waitableCurrentProcess;

        public static string ExecutablePath => Path.Combine(Path.GetDirectoryName(typeof(Driver).Assembly.Location), ExecutableName);

        static Driver()
        {
            IntPtr currentProcess = GetCurrentProcess();
            if (!DuplicateHandle(currentProcess, currentProcess, currentProcess, out waitableCurrentProcess, 0x00100000, true, 0))
            {
                waitableCurrentProcess = IntPtr.Zero;
            }
        }

        public static CaptureSession StartCapture(CaptureParameters parameters, IConsole console = null, EventHandler starting = null)
        {
            return new CaptureSession(BuildCaptureArguments(parameters), parameters.ControlStructure, console, starting);
        }

        public static async Task<Device[]> ListDevicesAsync(DataFlow flow = DataFlow.All, DeviceState state = DeviceState.All, IConsole console = null)
        {
            string args = BuildListArguments(flow, state);
            List<string> lines = new List<string>();
            int attempt = 0;
            do
            {
                using (Process worker = new Process())
                {
                    worker.StartInfo = new ProcessStartInfo
                    {
                        FileName = ExecutablePath,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = null != console,
                    };
                    worker.OutputDataReceived += (sender, e) =>
                    {
                        lock (lines)
                        {
                            lines.Add(e.Data);
                        }
                    };
                    if (null != console)
                    {
                        worker.ErrorDataReceived += (sender, e) =>
                        {
                            if (string.IsNullOrWhiteSpace(e.Data))
                            {
                                return;
                            }
                            console.Log(string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, e.Data.Trim()));
                        };
                    }
                    worker.Start();
                    worker.BeginOutputReadLine();
                    if (null != console)
                    {
                        worker.BeginErrorReadLine();
                    }
                    await worker.WaitForExitAsync();
                }
            } while (lines.Count == 0 && ++attempt < 3);
            List<Device> devices = new List<Device>();
            lock (lines)
            {
                if (lines.Count == 0)
                {
                    throw new Exception("WASCap list failed");
                }
                int linesPerDevice = int.Parse(lines[0].Trim());
                string[] deviceLines = new string[linesPerDevice];
                for (int i = 1; i + linesPerDevice <= lines.Count; i += linesPerDevice)
                {
                    lines.CopyTo(i, deviceLines, 0, linesPerDevice);
                    devices.Add(Device.Parse(deviceLines));
                }
            }

            return devices.ToArray();
        }

        static string BuildListArguments(DataFlow flow, DeviceState state)
        {
            StringBuilder args = new StringBuilder();
            args.AppendFormat("list {0}", ToString(flow));
            if ((state & DeviceState.Active) != 0)
            {
                args.Append(" active");
            }
            if ((state & DeviceState.Disabled) != 0)
            {
                args.Append(" disabled");
            }
            if ((state & DeviceState.NotPresent) != 0)
            {
                args.Append(" not-present");
            }
            if ((state & DeviceState.Unplugged) != 0)
            {
                args.Append(" unplugged");
            }

            return args.ToString();
        }

        static string ToString(DataFlow flow, bool allowAll = true)
        {
            switch (flow)
            {
                case DataFlow.Render:
                    return "render";
                case DataFlow.Capture:
                    return "capture";
                case DataFlow.All:
                    if (!allowAll)
                    {
                        throw new ArgumentException();
                    }
                    return "all";
                default:
                    throw new ArgumentException();
            }
        }

        static string BuildCaptureArguments(CaptureParameters parameters)
        {
            StringBuilder args = new StringBuilder();
            args.Append("capture");
            if (IntPtr.Zero != waitableCurrentProcess)
            {
                args.AppendFormat(" lifetime {0}", waitableCurrentProcess);
            }
            if (parameters.Source is CaptureParameters.DeviceWASSourceInfo devSource)
            {
                args.AppendFormat(" from-was-dev {0}",
                    devSource.DeviceId.EncodeParameterArgument());
            }
            else if (parameters.Source is CaptureParameters.DefaultDeviceWASSourceInfo defSource && (defSource.Flow != DataFlow.Render || defSource.Role != Role.Console))
            {
                args.AppendFormat(" from-was {0} {1}",
                    ToString(defSource.Flow, false).EncodeParameterArgument(),
                    defSource.Role.ToString().ToLowerInvariant().EncodeParameterArgument());
            }
            if (null != parameters.ControlStructure)
            {
                args.AppendFormat(" shm {0}",
                    parameters.ControlStructure.Name.EncodeParameterArgument());
                if (!parameters.WithSharedMemoryTapSink)
                {
                    args.Append(" no-shm-tap");
                }
                if (!parameters.WithSharedMemoryAveragingSink)
                {
                    args.Append(" no-shm-averaging");
                }
            }
            if (parameters.SampleRate.HasValue)
            {
                args.AppendFormat(" samplerate {0}",
                    parameters.SampleRate.GetValueOrDefault());
            }
            if (0 != parameters.Channels)
            {
                args.AppendFormat(" channel-mask {0}",
                    (int)parameters.Channels);
            }
            if (parameters.NetworkSink is CaptureParameters.NetworkSinkInfo netSink)
            {
                if (!string.IsNullOrEmpty(netSink.BindAddress))
                {
                    args.AppendFormat(" bind {0}",
                        netSink.BindAddress.EncodeParameterArgument());
                }
                if (!string.IsNullOrEmpty(netSink.PeerAddress) || !string.IsNullOrEmpty(netSink.PeerService))
                {
                    args.AppendFormat(" to-network-peer {0} {1}",
                        netSink.PeerAddress.EncodeParameterArgument(),
                        netSink.PeerService.EncodeParameterArgument());
                }
                else
                {
                    args.Append(" to-network");
                }
            }
            if (parameters.WASSink is CaptureParameters.DeviceWASSinkInfo devSink)
            {
                args.AppendFormat(" to-was-dev {0}",
                    devSink.DeviceId.EncodeParameterArgument());
            }
            else if (parameters.WASSink is CaptureParameters.DefaultDeviceWASSinkInfo defSink)
            {
                args.AppendFormat(" to-was {0}",
                    defSink.Role.ToString().ToLowerInvariant().EncodeParameterArgument());
            }
            if (parameters.Duration.HasValue)
            {
                args.AppendFormat(" duration {0}",
                    parameters.Duration.GetValueOrDefault().TotalSeconds.ToString().EncodeParameterArgument());
            }

            return args.ToString();
        }

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess", ExactSpelling = true)]
        static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwOptions);
    }
}
