using System;

namespace EtherSound.ViewModel
{
    public class TapDataEventArgs : EventArgs
    {
        readonly byte[] data;

        public byte[] Data => data;

        public TapDataEventArgs(byte[] data)
        {
            this.data = data;
        }
    }
}
