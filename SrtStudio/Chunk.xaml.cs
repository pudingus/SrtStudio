using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SrtStudio
{
    /// <summary>
    /// Container for Items
    /// </summary>
    public partial class Chunk : UserControl
    {
        bool locked;
        bool hilit;
        bool selected;

        public Chunk(Subtitle subtitle)
        {
            DataContext = subtitle;
            InitializeComponent();
            hilitBorder.Visibility = Visibility.Hidden;
            selBorder.Visibility = Visibility.Hidden;
        }

        public bool Locked {
            get => locked;
            set {
                locked = value;
                var bc = new BrushConverter();
                if (locked) backRect.Fill = (Brush)bc.ConvertFrom("#FF3C3C3C");
                else backRect.Fill = (Brush)bc.ConvertFrom("#FFA83535");
            }
        }

        public bool Hilit {
            get => hilit;
            set {
                hilit = value;
                hilitBorder.Visibility = hilit ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public bool Selected {
            get => selected;
            set {
                selected = value;
                selBorder.Visibility = selected ? Visibility.Visible : Visibility.Hidden;
            }
        }

        void Chunk_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size size = e.NewSize;
            if (size.Height < 70) {
                textBlock_dur.Visibility = Visibility.Collapsed;
                textBlock_cps.Visibility = Visibility.Collapsed;
            }
            else {
                textBlock_dur.Visibility = Visibility.Visible;
                textBlock_cps.Visibility = Visibility.Visible;
            }
        }
    }
}
