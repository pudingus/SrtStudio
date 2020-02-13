using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SrtStudio
{
    public class Track
    {
        public bool Locked { get; set; }

        public List<Item> StreamedItems { get; } = new List<Item>();
        /// <summary>
        /// All items in the track?
        /// </summary>
        public ObservableCollection<Item> Super { get; } = new ObservableCollection<Item>();
        

        public string Name {
            get => TrackHeader.trackName.Text;
            set => TrackHeader.trackName.Text = value;
        }

        private double _height = 100;
        public double Height {
            get => _height;
            set {
                _height = value;
                TrackHeader.Height = _height;
                TrackLine.Height = _height;
            }
        }

        public TrackHeader TrackHeader { get; set; }
        public Grid TrackLine { get; set; }

        public Track()
        {
            TrackHeader = new TrackHeader(this) {
                Height = _height
            };
            TrackLine = new Grid {
                Height = _height,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }
    }
}
