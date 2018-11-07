using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace SrtStudio
{
    public class ProjectStorage
    {
        public string VideoPath { get; set; }
    }

    public static class Project
    {
        public static ProjectStorage data = new ProjectStorage();
        public static void Read(string filename) {
            XmlSerializer ser = new XmlSerializer(typeof(ProjectStorage));
            StreamReader reader = new StreamReader(filename);
            data = ser.Deserialize(reader) as ProjectStorage;
        }
        public static void Write(string filename) {
            XmlSerializer ser = new XmlSerializer(typeof(ProjectStorage));
            StreamWriter writer = new StreamWriter(filename);
            ser.Serialize(writer, data);
        }
    }
}
