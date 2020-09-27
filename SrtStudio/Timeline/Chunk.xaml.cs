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
        readonly BrushConverter bc = new BrushConverter();

        public Chunk()
        {
            InitializeComponent();
            hilitBorder.Visibility = Visibility.Hidden;
            selBorder.Visibility = Visibility.Hidden;
        }

        public bool Locked {
            get => locked;
            set {
                locked = value;
                if (locked) {
                    backRect.Fill = (Brush)bc.ConvertFrom("#FF3C3C3C");
                    startBorder.IsEnabled = false;
                    endBorder.IsEnabled = false;
                }
                else {
                    backRect.Fill = (Brush)bc.ConvertFrom("#FFA83535");
                    startBorder.IsEnabled = true;
                    endBorder.IsEnabled = true;
                }
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

        void Chunk_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Locked)
                Hilit = true;
        }

        void Chunk_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Locked)
                Hilit = false;
        }
    }
}
