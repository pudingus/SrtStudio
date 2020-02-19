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
    public class Track {

        public Track(Timeline parentTimeline, bool locked) {
            ParentTimeline = parentTimeline;
            TrackHeader = new TrackHeader(this) {
                Height = height
            };
            TrackContent = new Grid {
                Height = height,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Locked = locked;
        }


        public bool             Locked          { get; }
        public List<Subtitle>   StreamedItems   { get; } = new List<Subtitle>();
        public Subtitle         ItemUnderNeedle { get; set; }
        public TrackHeader      TrackHeader     { get; }
        public Grid             TrackContent    { get; }
        public Timeline         ParentTimeline  { get; }


        public ObservableCollection<Subtitle> Items {
            get => items;
            set {
                items = value;
                ParentTimeline.RecalculateStreamedSet(this);
            }
        }

        public string Name {
            get => TrackHeader.trackName.Text;
            set => TrackHeader.trackName.Text = value;
        }

        public double Height {
            get => height;
            set {
                height = value;
                TrackHeader.Height = height;
                TrackContent.Height = height;
            }
        }

        ObservableCollection<Subtitle> items = new ObservableCollection<Subtitle>();
        double height = 100;
    }
}
