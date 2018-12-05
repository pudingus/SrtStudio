using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;

namespace SrtStudio
{

    public class ProjectStorage {
        public string VideoPath { get; set; }
        public string TrackName { get; set; }
        public List<Subtitle> Subtitles { get; set; }
        public string RefTrackName { get; set; }
        public List<Subtitle> RefSubtitles { get; set; }
        public double VideoPos { get; set; }
        public double ScrollPos { get; set; }
        public int SelIndex { get; set; }
    }

    public static class Project
    {
        public static string FileName { get; private set; }
        public static ProjectStorage Data { get; set; } = new ProjectStorage();

        public static void Load(string filename, bool asBackup = false) {
            XmlSerializer ser = new XmlSerializer(typeof(ProjectStorage));

            using (FileStream stream = File.OpenRead(asBackup ? filename+".bak" : filename)) {
                using (GZipStream zipStream = new GZipStream(stream, CompressionMode.Decompress)) {
                    Data = ser.Deserialize(zipStream) as ProjectStorage;
                    FileName = filename;
                }
            }
        }

        public static void Save(string filename, bool asBackup = false) {
            XmlSerializer ser = new XmlSerializer(typeof(ProjectStorage));

            using (FileStream stream = File.Create(asBackup ? filename+".bak" : filename)) {
                using (GZipStream zipStream = new GZipStream(stream, CompressionMode.Compress)) {
                    ser.Serialize(zipStream, Data);
                    FileName = filename;
                }
            }
        }
    }
}
