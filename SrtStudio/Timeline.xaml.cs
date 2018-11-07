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
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        public Timeline()
        {
            InitializeComponent();
        }

        public string trackName {
            get { return TrackName.Text; }
            set { TrackName.Text = value; }
        }

        public string subText {
            get { return SubText.Text; }
            set { SubText.Text = value; }
        }

        public double width {
            get { return grid1.Width; }
            set { grid1.Width = value; }
        }

        public double mLeft {
            get { return grid1.Margin.Left; }
            set { grid1.Margin = new Thickness(value, 0, 0, 0); }
        }

        //public delegate void SizeChanged();
        //public event SizeChanged OnSizeChanged;



        bool draggingEnd = false;
        bool draggingStart = false;
        bool draggingMid = false;


        Point point;
        private void UserControl_MouseMove(object sender, MouseEventArgs e) {

        }

        private void UserControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Point pointd;
            pointd = e.GetPosition(grid1);
            //point = e.GetPosition(wrap1);
            Console.WriteLine("pre mouse lb down");
            if (pointd.X >= grid1.Width - 5 && pointd.X <= grid1.Width + 5) {
                draggingEnd = true;
            }
            else if (pointd.X >= -5 && pointd.X <= 5) {
                draggingStart = true;
            }
            else draggingMid = true;
        }

        private void UserControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            Console.WriteLine("pre mouse lb up");
            draggingEnd = false;
            draggingStart = false;
            draggingMid = false;
        }

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e) {
            double deltax = point.X;
            point = e.GetPosition(wrap1);
            deltax -= point.X;
            Console.WriteLine(point);

            Point pointd;
            pointd = e.GetPosition(grid1);

            if (draggingEnd) {
                grid1.Width -= deltax;
            }
            else if (draggingStart) {
                double mleft = grid1.Margin.Left;
                mleft -= deltax;
                grid1.Width += deltax;

                grid1.Margin = new Thickness(mleft, 0, 0, 0);
            }
            else if (draggingMid) {
                double mleft = grid1.Margin.Left;
                mleft -= deltax;
                grid1.Margin = new Thickness(mleft, 0, 0, 0);
            }

            else {
                if (pointd.X >= grid1.Width - 5 && pointd.X <= grid1.Width + 5) {
                    Cursor = Cursors.SizeWE;
                }
                else if (pointd.X >= -5 && pointd.X <= 5) {
                    Cursor = Cursors.SizeWE;
                }
                else Cursor = Cursors.Arrow;
            }
        }

        private void grid1_MouseMove(object sender, MouseEventArgs e) {

        }
    }
}
