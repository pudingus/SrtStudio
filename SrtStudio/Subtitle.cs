using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace SrtStudio
{
    public class Subtitle : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int index;
        public int Index {
            get => index;
            set {
                if (index != value) {
                    index = value;
                    RaisePropertyChanged(nameof(Index));
                }
            }
        }

        TimeSpan start;
        [XmlIgnore]
        public TimeSpan Start {
            get => start;
            set {
                if (value != start) {
                    start = value;
                    RaisePropertyChanged(nameof(Start));
                    Duration = End - Start;
                }
            }
        }

        [XmlAttribute]
        public string SStart {
            get => Start.ToString();
            set => Start = TimeSpan.Parse(value);
        }

        TimeSpan end;
        [XmlIgnore]
        public TimeSpan End {
            get => end;
            set {
                if (value != end) {
                    end = value;
                    RaisePropertyChanged(nameof(End));
                    Duration = End - Start;
                }
            }
        }

        [XmlAttribute]
        public string SEnd {
            get => End.ToString();
            set => End = TimeSpan.Parse(value);
        }

        TimeSpan duration;
        [XmlIgnore]
        public TimeSpan Duration {
            get => duration;
            private set {
                if (value != duration) {
                    duration = value;
                    RaisePropertyChanged(nameof(Duration));
                    CPS = Text.Length / Duration.TotalSeconds;
                }
            }
        }

        double cps;
        [XmlIgnore]
        public double CPS {
            get => cps;
            private set {
                if (value != cps) {
                    cps = value;
                    RaisePropertyChanged(nameof(CPS));
                }
            }
        }

        string text = string.Empty;
        public string Text {
            get => text;
            set {
                if (value != text) {
                    text = value;
                    RaisePropertyChanged(nameof(Text));
                    CPS = Text.Length / Duration.TotalSeconds;
                }
            }
        }
        
        bool enabled;
        [XmlIgnore]
        public bool Enabled {
            get => enabled;
            set {
                if (value != enabled) {
                    enabled = value;
                    RaisePropertyChanged(nameof(Enabled));
                }
            }
        }

        [XmlIgnore]
        public Chunk Chunk { get; set; }
    }
}
