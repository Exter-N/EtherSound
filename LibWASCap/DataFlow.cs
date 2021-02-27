using System;

namespace WASCap
{
    [Flags]
    public enum DataFlow
    {
        Render = 1,
        Capture = 2,

        All = Render | Capture,
    }
}
