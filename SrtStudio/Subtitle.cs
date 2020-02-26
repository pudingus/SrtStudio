using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SrtStudio
{
    public class Subtitle : INotifyPropertyChanged
    {
        int index;
        TimeSpan start;
        TimeSpan end;
        TimeSpan duration;
        double cps;
        string text = string.Empty;
        bool enabled;

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlIgnore]
        public Chunk Chunk { get; set; }

        [XmlIgnore]
        public int Index {
            get => index;
            set {
                if (index != value) {
                    index = value;
                    RaisePropertyChanged(nameof(Index));
                }
            }
        }

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

        [XmlAttribute]
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

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
