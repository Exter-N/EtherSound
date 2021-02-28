using System.ComponentModel;

namespace EtherSound.ViewModel
{
    class SessionPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        readonly SessionModel session;
        readonly PropertyChangedEventArgs e;

        public SessionModel Session => session;
        public PropertyChangedEventArgs EventArgs => e;

        public SessionPropertyChangedEventArgs(SessionModel session, PropertyChangedEventArgs e) : base(e.PropertyName)
        {
            this.session = session;
            this.e = e;
        }
    }
}
