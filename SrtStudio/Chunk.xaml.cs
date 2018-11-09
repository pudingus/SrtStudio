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
        public Subtitle sub;
        public string Text {
            get { return textBlock.Text; }
            set { textBlock.Text = value; }
        }

        public string Dur {
            get { return textBlock_dur.Text; }
            set { textBlock_dur.Text = value; }
        }

        public Chunk()
        {
            InitializeComponent();
        }
    }
}
