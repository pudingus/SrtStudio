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
    /// Interaction logic for Event.xaml
    /// </summary>
    public partial class Chunk : UserControl
    {
        public bool Locked { get; set; }
        //private bool _selected;
        //public bool Selected {
        //    get { return _selected; }
        //    set {
        //        _selected = value;
        //        if (_selected) selBorder.Visibility = Visibility.Visible;
        //        else selBorder.Visibility = Visibility.Hidden;
        //    }
        //}


        private Timeline _parent;
        public Item Item { get; }

        public Track ParentTrack { get; set; }

        public Chunk(Timeline parent, Item item)
        {
            _parent = parent;
            Item = item;
            InitializeComponent();
            selBorder.Visibility = item.Selected ? Visibility.Visible : Visibility.Hidden;
            hilitBorder.Visibility = Visibility.Hidden;

            Item.PropertyChanged += Item_PropertyChanged;
            DataContext = Item;
            Update();
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            Item item = (Item)sender;
            if (e.PropertyName == nameof(item.Selected)) {
                selBorder.Visibility = item.Selected ? Visibility.Visible : Visibility.Hidden;
            }
            //else if (e.PropertyName == nameof(item.Start)) {
            //    double margin = item.Start.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //    Margin = new Thickness(margin, 0, 0, 0);
            //    double width = item.Dur.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //    Width = width;
            //}
            //else if (e.PropertyName == nameof(item.Dur)) {
            //    double margin = item.Start.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //    Margin = new Thickness(margin, 0, 0, 0);
            //    double width = item.Dur.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //    Width = width;
            //}
        }

        public void Update() {
            double margin = Item.Start.TotalSeconds / _parent.Timescale * _parent.Pixelscale;
            Margin = new Thickness(margin, 0, 0, 0);
            double width = Item.Dur.TotalSeconds / _parent.Timescale * _parent.Pixelscale;
            Width = width;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
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
