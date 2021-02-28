using Newtonsoft.Json;
using System;
using System.IO;

namespace EtherSound.Settings
{
    class SettingsContainer
    {
        readonly string path;
        readonly RootSettings settings;

        public RootSettings Settings => settings;

        public SettingsContainer(string path)
        {
            this.path = path;
            try
            {
                string text = File.ReadAllText(path);
                settings = JsonConvert.DeserializeObject<RootSettings>(text);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                settings = new RootSettings
                {
                    Dirty = true
                };
            }
        }

        public void Save(bool force = false)
        {
            if (!settings.Dirty && !force)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
            settings.Dirty = false;
        }
    }
}
