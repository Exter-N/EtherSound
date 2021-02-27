using System;
using System.Reflection;

namespace EtherSound.View.Effects
{
    static class Helpers
    {
        public static Uri MakePackUri(string relativeFile)
        {
            Assembly a = typeof(Helpers).Assembly;

            // Extract the short name.
            string assemblyShortName = a.ToString().Split(',')[0];

            string uriString = "pack://application:,,,/" + assemblyShortName + ";component/" + relativeFile;

            return new Uri(uriString);
        }
    }
}
