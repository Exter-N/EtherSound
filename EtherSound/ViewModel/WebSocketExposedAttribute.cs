using System;

namespace EtherSound.ViewModel
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    sealed class WebSocketExposedAttribute : Attribute
    {
        public bool Writable { get; set; } = true;

        public string Name { get; set; }
    }
}
