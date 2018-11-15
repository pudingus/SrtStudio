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
        private bool _selected;
        public bool Selected {
            get { return _selected; }
            set {
                _selected = value;
                if (_selected) selBorder.Visibility = Visibility.Visible;
                else selBorder.Visibility = Visibility.Hidden;
            }
        }

        public Chunk()
        {
            InitializeComponent();
            selBorder.Visibility = Visibility.Hidden;
            hilitBorder.Visibility = Visibility.Hidden;
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
