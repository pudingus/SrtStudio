using System;
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
}
