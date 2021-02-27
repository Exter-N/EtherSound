using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherSound.ViewModel
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    sealed class WebSocketExposedAttribute : Attribute
    {
        public bool Writable { get; set; } = true;

        public string Name { get; set; }
    }
}
