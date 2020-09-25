using System;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;

namespace SrtStudio
{
    [Serializable]
    public class Settings
    {
        const string FILENAME = "settings.xml";

        public bool Maximized { get; set; }
        public bool SafelyExited { get; set; }
        public string LastProject { get; set; }

        public static Settings Read() {
            Settings settings = null;
            try {
                using (var sr = new StreamReader(FILENAME)) {
                    var xmls = new XmlSerializer(typeof(Settings));
                    settings = xmls.Deserialize(sr) as Settings;
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
                settings = new Settings();
            }
            return settings;
        }

        public void Write() {
            using (var sw = new StreamWriter(FILENAME)) {
                var xmls = new XmlSerializer(typeof(Settings));
                xmls.Serialize(sw, this);
            }
        }
    }
}
