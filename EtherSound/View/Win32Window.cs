using System;
using System.Windows;
using System.Windows.Interop;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace EtherSound.View
{
    class Win32Window : IWin32Window
    {
        readonly Window window;

        public IntPtr Handle => new WindowInteropHelper(window).Handle;

        public Win32Window(Window window)
        {
            this.window = window;
        }
    }
}
