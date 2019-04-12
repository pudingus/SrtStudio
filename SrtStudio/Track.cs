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

        public List<Item> Streamed;
        /// <summary>
        /// All items in the track?
        /// </summary>
        public ObservableCollection<Item> Super;


        public string Name {
            get => TrackMeta.trackName.Text;
            set => TrackMeta.trackName.Text = value;
        }

        private double _height = 100;
        public double Height {
            get => _height;
            set {
                _height = value;
                TrackMeta.Height = _height;
                TrackLine.Height = _height;
            }
        }

        public TrackMeta TrackMeta { get; set; }
        public Grid TrackLine { get; set; }

        public Track()
        {
            TrackMeta = new TrackMeta(this) {
                Height = _height
            };
            TrackLine = new Grid {
                Height = _height,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            //TrackLine.itemsControl.ItemsSource = TrackLine.ChunksSuper;
        }
    }
}
