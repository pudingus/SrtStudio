using System;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;

namespace SrtStudio
{
    [Serializable]
    public class SettingsStorage
    {
        public bool Maximized { get; set; }
        public bool SafelyExited { get; set; }
        public string LastProject { get; set; }
    }

    public static class Settings
    {
        const string FILENAME = "settings.xml";

        public static SettingsStorage Load()
        {
            SettingsStorage settings = null;
            try {
                using (var sr = new StreamReader(FILENAME)) {
                    var xmls = new XmlSerializer(typeof(SettingsStorage));
                    settings = xmls.Deserialize(sr) as SettingsStorage;
                }
            }
            catch (FileNotFoundException ex) {
                Console.WriteLine(ex.Message);
            }
            catch (UnauthorizedAccessException ex) {
                Console.WriteLine(ex.Message);
            }
            catch (InvalidOperationException ex) {
                Console.WriteLine(ex.Message);
            }
            catch (IOException ex) {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            if (settings == null) {
                settings = new SettingsStorage();
            }
            return settings;
        }

        public static void Save(SettingsStorage settings)
        {
            using (var sw = new StreamWriter(FILENAME)) {
                var xmls = new XmlSerializer(typeof(SettingsStorage));
                xmls.Serialize(sw, settings);
            }
        }
    }
}
