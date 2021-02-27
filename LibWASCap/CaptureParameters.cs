using System;

namespace WASCap
{
    public class CaptureParameters
    {
        public ControlStructure ControlStructure { get; set; }

        public int? SampleRate { get; set; }
        public Channel Channels { get; set; }

        public SourceInfo Source { get; set; } = new DefaultDeviceWASSourceInfo(DataFlow.Render, Role.Console);

        public WASSinkInfo WASSink { get; set; }
        public NetworkSinkInfo NetworkSink { get; set; }
        public bool WithSharedMemoryTapSink { get; set; } = true;
        public bool WithSharedMemoryAveragingSink { get; set; } = true;

        public TimeSpan? Duration { get; set; }

        public abstract class SourceInfo
        {
            internal SourceInfo() { }
        }

        public class DeviceWASSourceInfo : SourceInfo
        {
            public string DeviceId { get; set; }

            public DeviceWASSourceInfo(string deviceId)
            {
                DeviceId = deviceId;
            }
        }

        public class DefaultDeviceWASSourceInfo : SourceInfo
        {
            public DataFlow Flow { get; set; }
            public Role Role { get; set; }

            public DefaultDeviceWASSourceInfo(DataFlow flow = DataFlow.Render, Role role = Role.Console)
            {
                Flow = flow;
                Role = role;
            }
        }

        public class NetworkSinkInfo
        {
            public string BindAddress { get; set; }
            public string PeerAddress { get; set; }
            public string PeerService { get; set; }
        }

        public abstract class WASSinkInfo
        {
            internal WASSinkInfo() { }
        }

        public class DeviceWASSinkInfo : WASSinkInfo
        {
            public string DeviceId { get; set; }

            public DeviceWASSinkInfo(string deviceId)
            {
                DeviceId = deviceId;
            }
        }

        public class DefaultDeviceWASSinkInfo : WASSinkInfo
        {
            public Role Role { get; set; }

            public DefaultDeviceWASSinkInfo(Role role = Role.Console)
            {
                Role = role;
            }
        }
    }
}
