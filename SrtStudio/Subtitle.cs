using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace SrtStudio
{
    public class Subtitle
    {
        public TimeSpan start;
        public TimeSpan end;
        public string text;
    }
    public class Srt
    {
        public List<Subtitle> list = new List<Subtitle>();
        Subtitle subtitle = new Subtitle();

        //0 - looking for timecode
        //1 - looking for text
        //2 - looking for line break

        int mode = 0;
        int lnumber = 0;

        public void Read(string filename) {
            StreamReader reader = new StreamReader(filename);
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();

                if (mode == 1) {
                    lnumber++;
                    if (lnumber >= 2 && string.IsNullOrWhiteSpace(line)) {
                        mode = 0;
                        //MessageBox.Show(subtitle.start + " " + subtitle.end + "\n" + subtitle.text);
                        list.Add(subtitle);
                        subtitle = new Subtitle();
                    }
                    else {
                        if (!string.IsNullOrEmpty(subtitle.text))
                            subtitle.text += "\n";
                        subtitle.text += line;
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
                            (line[8] >= ',') &&
                            (line[9] >= '0' && line[9] <= '9') &&
                            (line[10] >= '0' && line[10] <= '9') &&
                            (line[11] >= '0' && line[11] <= '9'))
                            {
                                found = true;
                                int hour = Convert.ToInt32(line.Substring(0, 2));
                                int minute = Convert.ToInt32(line.Substring(3, 2));
                                int second = Convert.ToInt32(line.Substring(6, 2));
                                int ms = Convert.ToInt32(line.Substring(9, 3));

                                subtitle.start = new TimeSpan(0, hour, minute, second, ms);

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
                            (line[8] >= ',') &&
                            (line[9] >= '0' && line[9] <= '9') &&
                            (line[10] >= '0' && line[10] <= '9') &&
                            (line[11] >= '0' && line[11] <= '9'))
                            {
                                found = true;
                                int hour = Convert.ToInt32(line.Substring(0, 2));
                                int minute = Convert.ToInt32(line.Substring(3, 2));
                                int second = Convert.ToInt32(line.Substring(6, 2));
                                int ms = Convert.ToInt32(line.Substring(9, 3));

                                subtitle.end = new TimeSpan(0, hour, minute, second, ms);

                                line = line.Remove(0, 12);

                                mode = 1;
                            }
                            else line = line.Remove(0, 1);
                        }
                        else line = line.Remove(0, 1);
                    }
                }
            }
        }
    }
}
