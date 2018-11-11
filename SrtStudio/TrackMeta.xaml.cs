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
    /// Interaction logic for TrackMeta.xaml
    /// </summary>
    public partial class TrackMeta : UserControl
    {
        public Grid Track { get; set; }
        public string Text {
            get { return trackName.Text; }
            set { trackName.Text = value; }
        }
        public TrackMeta()
        {
            InitializeComponent();
        }
    }
}
