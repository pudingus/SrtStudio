using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SrtStudio {
    [Serializable]
    public class SettingsStorage {
        public bool Maximized { get; set; }
    }

    public static class Settings {

        public static SettingsStorage Data { get; private set; } = new SettingsStorage();
        static string filename = "settings.xml";
        public static void Read() {
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

        public static void Write() {
            using (StreamWriter sw = new StreamWriter(filename)) {
                XmlSerializer xmls = new XmlSerializer(typeof(SettingsStorage));
                xmls.Serialize(sw, Data);
            }
        }
    }
}
