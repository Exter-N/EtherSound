using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
