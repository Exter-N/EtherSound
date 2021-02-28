using System;
using System.Runtime.InteropServices;

namespace EtherSound
{
    static class LocalSound
    {
        public static void ToggleMute()
        {
            SendMessage(GetForegroundWindow(), 0x0319, GetForegroundWindow(), new IntPtr(0x80000));
        }

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow", ExactSpelling = true)]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, int uMessage, IntPtr wParam, IntPtr lParam);
    }
}
