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
        public static bool UnsavedChanges { get; private set; }
        public static bool UnwrittenChanges { get; private set; }

        public static void Load(string filename, bool asBackup = false) {
            XmlSerializer ser = new XmlSerializer(typeof(ProjectStorage));

            Settings.Data.LastProject = filename;

            using (FileStream stream = File.OpenRead(asBackup ? filename+".temp" : filename)) {
                using (GZipStream zipStream = new GZipStream(stream, CompressionMode.Decompress)) {
                    Data = ser.Deserialize(zipStream) as ProjectStorage;
                    FileName = filename;
                    if (asBackup) UnsavedChanges = true;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="asBackup"></param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Save(string filename, bool asBackup = false) {
            XmlSerializer ser = new XmlSerializer(typeof(ProjectStorage));

            Settings.Data.LastProject = filename;

            using (FileStream stream = File.Create(asBackup ? filename+".temp" : filename)) {
                using (GZipStream zipStream = new GZipStream(stream, CompressionMode.Compress)) {
                    ser.Serialize(zipStream, Data);
                    FileName = filename;
                    if (!asBackup) UnsavedChanges = false;
                    UnwrittenChanges = false;
                }
            }
        }

        public static void FlagChange() {
            UnsavedChanges = true;
            UnwrittenChanges = true;
        }

        //static string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        //static string backupPath = Path.Combine(appdata, Local.ProgramName, "backup");

        //public static void Backup() {
        //    string name = Path.GetFileName(FileName);
        //    string filepath = Path.Combine(backupPath, name);

        //    XmlSerializer ser = new XmlSerializer(typeof(ProjectStorage));


        //    using (FileStream stream = File.Create(filepath)) {
        //        using (GZipStream zipStream = new GZipStream(stream, CompressionMode.Compress)) {
        //            ser.Serialize(zipStream, Data);
        //            UnwrittenChanges = false;
        //        }
        //    }
        //}
    }
}
