using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace SrtStudio
{
    public class Subtitle
    {
        [XmlIgnore]
        public TimeSpan Start { get; set; }
        [XmlAttribute]
        public string SStart {
            get => Start.ToString();
            set => Start = TimeSpan.Parse(value);
        }

        [XmlIgnore]
        public TimeSpan End { get; set; }
        [XmlAttribute]
        public string SEnd {
            get => End.ToString();
            set => End = TimeSpan.Parse(value);
        }

        [XmlAttribute]
        public string Text { get; set; } = "";
    }

    public static class Srt
    {
        static Subtitle subtitle = new Subtitle();

        public static void Write(string filename, List<Subtitle> list) {
            StreamWriter writer = new StreamWriter(filename);

            int i = 0;
            foreach (Subtitle sub in list) {
                i++;
                writer.WriteLine(i);
                string format = "hh\\:mm\\:ss\\,fff";
                writer.Write(sub.Start.ToString(format));
                writer.Write(" --> ");
                writer.WriteLine(sub.End.ToString(format));
                writer.WriteLine(sub.Text);
                writer.WriteLine();
            }

            writer.Close();
        }

        //0 - looking for timecode
        //1 - looking for text
        //2 - looking for line break

        static int mode = 0;
        static int lnumber = 0;

        public static List<Subtitle> Read(string filename) {
            List<Subtitle> list = new List<Subtitle>();

            //string s = File.ReadAllText(filename);
            //StringReader reader = new StringReader(s);
            //string line;
            //while ((line = reader.ReadLine()) != null) {

            StreamReader reader = new StreamReader(filename);
            string line;
            while ((line = reader.ReadLine()) != null) {

                if (mode == 1) {
                    lnumber++;
                    if (lnumber >= 2 && string.IsNullOrWhiteSpace(line)) {
                        mode = 0;
                        //MessageBox.Show(subtitle.start + " " + subtitle.end + "\n" + subtitle.text);
                        list.Add(subtitle);
                        subtitle = new Subtitle();
                    }
                    else {
                        if (!string.IsNullOrEmpty(subtitle.Text))
                            subtitle.Text += Environment.NewLine;
                        subtitle.Text += line;
                    }
                }

                if (mode == 0) {
                    bool found = false;
                    while (line != "" && !found) {
                        if (line.Length >= 12) {
                            if ((line[0] >= '0' && line[0] <= '9') &&
                            (line[1] >= '0' && line[1] <= '9') &&
                            (line[2] == ':') &&
                            (line[3] >= '0' && line[3] <= '9') &&
                            (line[4] >= '0' && line[4] <= '9') &&
                            (line[5] == ':') &&
                            (line[6] >= '0' && line[6] <= '9') &&
                            (line[7] >= '0' && line[7] <= '9') &&
                            (line[8] == ',') &&
                            (line[9] >= '0' && line[9] <= '9') &&
                            (line[10] >= '0' && line[10] <= '9') &&
                            (line[11] >= '0' && line[11] <= '9'))
                            {
                                found = true;
                                int hour = Convert.ToInt32(line.Substring(0, 2));
                                int minute = Convert.ToInt32(line.Substring(3, 2));
                                int second = Convert.ToInt32(line.Substring(6, 2));
                                int ms = Convert.ToInt32(line.Substring(9, 3));

                                subtitle.Start = new TimeSpan(0, hour, minute, second, ms);

                                line = line.Remove(0, 12);
                            }
                            else line = line.Remove(0, 1);
                        }
                        else line = line.Remove(0, 1);
                    }

                    found = false;
                    while (line != "" && !found) {
                        if (line.Length >= 12) {
                            if ((line[0] >= '0' && line[0] <= '9') &&
                            (line[1] >= '0' && line[1] <= '9') &&
                            (line[2] == ':') &&
                            (line[3] >= '0' && line[3] <= '9') &&
                            (line[4] >= '0' && line[4] <= '9') &&
                            (line[5] == ':') &&
                            (line[6] >= '0' && line[6] <= '9') &&
                            (line[7] >= '0' && line[7] <= '9') &&
                            (line[8] == ',') &&
                            (line[9] >= '0' && line[9] <= '9') &&
                            (line[10] >= '0' && line[10] <= '9') &&
                            (line[11] >= '0' && line[11] <= '9'))
                            {
                                found = true;
                                int hour = Convert.ToInt32(line.Substring(0, 2));
                                int minute = Convert.ToInt32(line.Substring(3, 2));
                                int second = Convert.ToInt32(line.Substring(6, 2));
                                int ms = Convert.ToInt32(line.Substring(9, 3));

                                subtitle.End = new TimeSpan(0, hour, minute, second, ms);

                                line = line.Remove(0, 12);

                                mode = 1;
                            }
                            else line = line.Remove(0, 1);
                        }
                        else line = line.Remove(0, 1);
                    }
                }
            }
            reader.Close();
            return list;

            //MessageBox.Show("done");
        }
    }
}
