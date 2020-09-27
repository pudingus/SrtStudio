using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace SrtStudio
{
    public interface IBackwardCompatibilitySerializer
    {
        void OnUnknownElementFound(string unknownName, string value);
    }

    [Serializable]
    [XmlRoot("ProjectStorage")]
    public class Project
    {
        public string VideoPath { get; set; } = "";
        public string TrackName { get; set; } = "";
        public ObservableCollection<Subtitle> Subtitles { get; set; } = new ObservableCollection<Subtitle>();
        public string RefTrackName { get; set; } = "";
        public ObservableCollection<Subtitle> RefSubtitles { get; set; } = new ObservableCollection<Subtitle>();
        public double VideoPos { get; set; }
        public double ScrollPos { get; set; }
        public int SelIndex { get; set; }

        [XmlIgnore]
        public string FileName { get; set; }
        [XmlIgnore]
        public bool UnsavedChanges { get; set; }
        [XmlIgnore]
        public bool UnwrittenChanges { get; set; }


        public void SignalChange() {
            UnsavedChanges = true;
            UnwrittenChanges = true;
        }

        public static Project Read(string filename, bool asBackup = false) {
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            Project project = null;
            var ser = new XmlSerializer(typeof(Project));
            ser.UnknownAttribute += Ser_UnknownAttribute;

            using (FileStream stream = File.OpenRead(asBackup ? filename+".temp" : filename)) {
                using (var zipStream = new GZipStream(stream, CompressionMode.Decompress)) {
                    project = ser.Deserialize(zipStream) as Project;
                }
            }

            if (project == null) {
                project = new Project();
            }
            else {
                project.FileName = filename;
                if (asBackup) project.UnsavedChanges = true;
            }
            return project;
        }

        private static void Ser_UnknownAttribute(object sender, XmlAttributeEventArgs e) {
            //credits to someone stackoverflow
            var deserializedObj = (e.ObjectBeingDeserialized as IBackwardCompatibilitySerializer);
            if (deserializedObj == null) return;
            deserializedObj.OnUnknownElementFound(e.Attr.Name, e.Attr.InnerText);
        }

        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void Write(bool asBackup = false) {
            string filename = FileName;
            var ser = new XmlSerializer(typeof(Project));

            using (FileStream stream = File.Create(asBackup ? filename + ".temp" : filename)) {
                using (var zipStream = new GZipStream(stream, CompressionMode.Compress)) {
                    ser.Serialize(zipStream, this);
                    if (!asBackup) UnsavedChanges = false;
                    UnwrittenChanges = false;
                }
            }
        }
    }
}
