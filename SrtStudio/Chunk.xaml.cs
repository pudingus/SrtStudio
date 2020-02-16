using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SrtStudio
{
    /// <summary>
    /// Container for Items
    /// </summary>
    public partial class Chunk : UserControl
    {

        bool locked;
        public bool Locked { 
            get {
                return locked;
            } 
            set {
                locked = value;
                var bc = new BrushConverter();
                if (locked) backRect.Fill = (Brush)bc.ConvertFrom("#FF3C3C3C");
                else backRect.Fill = (Brush)bc.ConvertFrom("#FFA83535");
            }
        }

        bool hilit;
        public bool Hilit {
            get => hilit;
            set {
                hilit = value;
                hilitBorder.Visibility = hilit ? Visibility.Visible : Visibility.Hidden;
            }
        }

        bool selected;
        public bool Selected {
            get => selected; 
            set {
                selected = value;
                selBorder.Visibility = selected ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public Item Item { get; }

        public Chunk(Item item) {
            Item = item;
            InitializeComponent();
            hilitBorder.Visibility = Visibility.Hidden;

            DataContext = Item;
        }

        void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) {
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
