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



        public Chunk(Timeline parent, Item item)
        {
            _parent = parent;
            Item = item;
            InitializeComponent();
            selBorder.Visibility = Visibility.Hidden;
            hilitBorder.Visibility = Visibility.Hidden;

            Item.PropertyChanged += Item_PropertyChanged;
            DataContext = Item;

            //double margin = Item.Start.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //Margin = new Thickness(margin, 0, 0, 0);
            //double width = Item.Dur.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //Width = width;
            Update();
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "Selected") {
                if (Item.Selected) selBorder.Visibility = Visibility.Visible;
                else selBorder.Visibility = Visibility.Hidden;
            }
            //else if (e.PropertyName == "Start") {
            //    double margin = Item.Start.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //    Margin = new Thickness(margin, 0, 0, 0);
            //    double width = Item.Dur.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //    Width = width;
            //}
            //else if (e.PropertyName == "Dur") {
            //    double margin = Item.Start.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //    Margin = new Thickness(margin, 0, 0, 0);
            //    double width = Item.Dur.TotalSeconds / _parent.timescale * _parent.pixelscale;
            //    Width = width;
            //}
        }

        public void Update() {
            double margin = Item.Start.TotalSeconds / _parent.timescale * _parent.pixelscale;
            Margin = new Thickness(margin, 0, 0, 0);
            double width = Item.Dur.TotalSeconds / _parent.timescale * _parent.pixelscale;
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
