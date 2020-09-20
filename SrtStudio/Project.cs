using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace SrtStudio
{
    public class ProjectStorage
    {
        public string VideoPath { get; set; }
        public string TrackName { get; set; }
        public ObservableCollection<Subtitle> Subtitles { get; set; }
        public string RefTrackName { get; set; }
        public ObservableCollection<Subtitle> RefSubtitles { get; set; }
        public double VideoPos { get; set; }
        public double ScrollPos { get; set; }
        public int SelIndex { get; set; }

        [XmlIgnore]
        public string FileName { get; set; } = "Untitled";
        [XmlIgnore]
        public bool UnsavedChanges { get; set; }
        [XmlIgnore]
        public bool UnwrittenChanges { get; set; }


        public void SignalChange() {
            UnsavedChanges = true;
            UnwrittenChanges = true;
        }
    }

    public static class Project
    {
        /// <summary>
        /// Full path
        /// </summary>


        public static ProjectStorage Read(string filename, bool asBackup = false)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            ProjectStorage project = null;
            var ser = new XmlSerializer(typeof(ProjectStorage));

            using (FileStream stream = File.OpenRead(asBackup ? filename+".temp" : filename)) {
                using (var zipStream = new GZipStream(stream, CompressionMode.Decompress)) {
                    project = ser.Deserialize(zipStream) as ProjectStorage;                    
                }
            }

            if (project == null) {
                project = new ProjectStorage();
            }
            else {
                project.FileName = filename;
                if (asBackup) project.UnsavedChanges = true;
            }
            return project;
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
        public static void Write(ProjectStorage project, string filename, bool asBackup = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            var ser = new XmlSerializer(typeof(ProjectStorage));

            using (FileStream stream = File.Create(asBackup ? filename + ".temp" : filename)) {
                using (var zipStream = new GZipStream(stream, CompressionMode.Compress)) {
                    ser.Serialize(zipStream, project);
                    project.FileName = filename;
                    if (!asBackup) project.UnsavedChanges = false;
                    project.UnwrittenChanges = false;
                }
            }
        }


    }
}
