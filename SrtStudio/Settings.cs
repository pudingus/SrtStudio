using System;
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
        public static SettingsStorage Data { get; private set; } = new SettingsStorage();
        const string filename = "settings.xml";

        public static void Load()
        {
            try {
                using (StreamReader sr = new StreamReader(filename)) {
                    XmlSerializer xmls = new XmlSerializer(typeof(SettingsStorage));
                    Data = xmls.Deserialize(sr) as SettingsStorage;
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

        }

        public static void Save()
        {
            using (StreamWriter sw = new StreamWriter(filename)) {
                XmlSerializer xmls = new XmlSerializer(typeof(SettingsStorage));
                xmls.Serialize(sw, Data);
            }
        }
    }
}
