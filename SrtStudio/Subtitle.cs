using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace SrtStudio
{
    public class Subtitle : INotifyPropertyChanged, IBackwardCompatibilitySerializer
    {
        int index;
        TimeSpan start;
        TimeSpan duration;
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
                    RaisePropertyChanged(nameof(End));
                }
            }
        }
                
        [XmlAttribute("Start")]
        public string StartString {
            get => Start.ToString();
            set => Start = TimeSpan.Parse(value);
        }

        [XmlIgnore]
        public TimeSpan End {
            get => Start + Duration;            
        } 

        [XmlIgnore]
        public TimeSpan Duration {
            get => duration;
            set {
                if (value != duration) {
                    duration = value;
                    RaisePropertyChanged(nameof(Duration)); 
                    RaisePropertyChanged(nameof(End));
                    RaisePropertyChanged(nameof(CPS));
                }
            }
        }

        [XmlAttribute("Duration")]
        public string DurationString {
            get => Duration.ToString();
            set => Duration = TimeSpan.Parse(value);
        }

        [XmlIgnore]
        public double CPS {
            get => Text.Length / Duration.TotalSeconds;
        }

        [XmlAttribute]
        public string Text {
            get => text;
            set {
                if (value != text) {
                    text = value;
                    RaisePropertyChanged(nameof(Text)); 
                    RaisePropertyChanged(nameof(CPS));
                }
            }
        }

        [XmlIgnore]
        public bool TbxEnabled {
            get => enabled;
            set {
                if (value != enabled) {
                    enabled = value;
                    RaisePropertyChanged(nameof(TbxEnabled));
                }
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnUnknownElementFound(string unknownName, string value) {
            //Debug.WriteLine($"name: {unknownName}, value: {value}");
            switch (unknownName) {
                case "SEnd":
                    var end = TimeSpan.Parse(value);
                    Duration = end - Start;
                    break;
                case "SStart":
                    StartString = value;
                    break;
            }
        }
    }
}
