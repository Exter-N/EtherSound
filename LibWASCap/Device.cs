using System;
using System.Linq;
using System.Text;

namespace WASCap
{
    public class Device
    {
        public string Id { get; private set; }
        public string FriendlyName { get; private set; }
        public DataFlow Flow { get; private set; }
        public DeviceState State { get; private set; }
        public int SampleRate { get; private set; }
        public Channel Channels { get; private set; }
        public Role DefaultFor { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[Device Id={0} FriendlyName={1} Flow={2} State={3}", Id, FriendlyName, Flow, State);
            if (SampleRate != 0)
            {
                sb.AppendFormat(" SampleRate={0}", SampleRate);
            }
            if (Channels != 0)
            {
                sb.AppendFormat(" Channels={0}", Channels);
            }
            if (DefaultFor != 0)
            {
                sb.AppendFormat(" DefaultFor={0}", DefaultFor);
            }
            sb.Append(']');

            return sb.ToString();
        }

        internal static Device Parse(string[] lines)
        {
            return new Device
            {
                Id = lines[0],
                FriendlyName = lines[1],
                Flow = ParseWords(lines[2], ParseFlow, (DataFlow)0, (x, y) => x | y),
                State = ParseWords(lines[3], ParseState, (DeviceState)0, (x, y) => x | y),
                SampleRate = int.Parse(lines[4]),
                Channels = (Channel)int.Parse(lines[5]),
                DefaultFor = ParseWords(lines[6], ParseRole, (Role)0, (x, y) => x | y),
            };
        }

        static R ParseWords<R, W>(string words, Func<string, W> parseWord, R seed, Func<R, W, R> aggregate)
        {
            return words.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(parseWord).Aggregate(seed, aggregate);
        }

        static DataFlow ParseFlow(string flow)
        {
            switch (flow)
            {
                case "capture":
                    return DataFlow.Capture;
                case "render":
                    return DataFlow.Render;
                case "all":
                    return DataFlow.All;
                default:
                    throw new ArgumentException();
            }
        }

        static DeviceState ParseState(string state)
        {
            switch (state)
            {
                case "active":
                    return DeviceState.Active;
                case "disabled":
                    return DeviceState.Disabled;
                case "not-present":
                    return DeviceState.NotPresent;
                case "unplugged":
                    return DeviceState.Unplugged;
                default:
                    throw new ArgumentException();
            }
        }

        static Role ParseRole(string role)
        {
            switch (role)
            {
                case "console":
                    return Role.Console;
                case "multimedia":
                    return Role.Multimedia;
                case "communications":
                    return Role.Communications;
                default:
                    throw new ArgumentException();
            }
        }
    }
}
