using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WASCap;

namespace EtherSound.Settings
{
    static class SettingsHelper
    {
        static readonly Regex DeviceNamePattern = new Regex(@"^(.*) \((?:(\d+)-)?\s*(.*)\)$", RegexOptions.Compiled);

        public static Device Resolve(Device[] devices, string id, string friendlyName, DataFlow flow)
        {
            devices = Array.FindAll(devices, dev => (dev.Flow & flow) != 0);

            Device device = Array.Find(devices, dev => dev.Id == id);
            if (null != device)
            {
                return device;
            }

            if (null == friendlyName)
            {
                return null;
            }

            device = Array.Find(devices, dev => dev.FriendlyName == friendlyName);
            if (null != device)
            {
                return device;
            }

            string desc;
            int ifNumber;
            string ifName;
            Dictionary<string, (string, int, string)> cache = new Dictionary<string, (string, int, string)>();
            if (!TryParseDeviceName(friendlyName, cache, out desc, out ifNumber, out ifName))
            {
                return null;
            }

            device = Array.Find(devices, dev => TryParseDeviceName(dev.FriendlyName, cache, out string devDesc, out _, out string devIfName) && devDesc == desc && devIfName == ifName);
            if (null != device)
            {
                return device;
            }

            device = Array.Find(devices, dev => TryParseDeviceName(dev.FriendlyName, cache, out _, out int devIfNumber, out string devIfName) && devIfNumber == ifNumber && devIfName == ifName);
            if (null != device)
            {
                return device;
            }

            device = Array.Find(devices, dev => TryParseDeviceName(dev.FriendlyName, cache, out _, out _, out string devIfName) && devIfName == ifName);
            if (null != device)
            {
                return device;
            }

            return null;
        }

        static bool TryParseDeviceName(string name, Dictionary<string, (string, int, string)> cache, out string desc, out int ifNumber, out string ifName)
        {
            if (cache.TryGetValue(name, out (string, int, string) cached))
            {
                (desc, ifNumber, ifName) = cached;

                return true;
            }

            Match match = DeviceNamePattern.Match(name);
            if (null == match)
            {
                desc = null;
                ifNumber = 0;
                ifName = null;

                return false;
            }

            desc = match.Groups[1].Value;
            ifNumber = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            ifName = match.Groups[3].Value;

            return true;
        }
    }
}
