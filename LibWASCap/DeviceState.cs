using System;

namespace WASCap
{
    [Flags]
    public enum DeviceState
    {
        Active = 1,
        Disabled = 2,
        NotPresent = 4,
        Unplugged = 8,

        All = Active | Disabled | NotPresent | Unplugged,
    }
}
