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
    public partial class Timeline : UserControl {
        public Timeline() {
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



        //bool draggingEnd = false;
        //bool draggingStart = false;
        //bool draggingMid = false;



        private void UserControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            //Point pointd;
            //pointd = e.GetPosition(wrap1);
            ////point = e.GetPosition(wrap1);
            //Console.WriteLine("pre mouse lb down");

            //foreach (Grid grid in wrap1.Children) {
            //    if (pointd.Y >= 0 && pointd.Y <= grid.Height) {
            //        if (pointd.X >= grid.Margin.Left + grid.Width - 5 && pointd.X <= grid.Margin.Left + grid.Width + 5) {
            //            draggingEnd = true;
            //        }
            //        else if (pointd.X >= grid.Margin.Left - 5 && pointd.X <= grid.Margin.Left + 5) {
            //            draggingStart = true;
            //        }
            //        else if (pointd.X >= grid.Margin.Left && point.X <= grid.Margin.Left + grid.Width) {
            //            draggingMid = true;
            //        }
            //    }
            //}



        }

        private void UserControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            //Console.WriteLine("pre mouse lb up");
            //draggingEnd = false;
            //draggingStart = false;
            //draggingMid = false;
        }

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e) {
            double deltax = point.X;
            point = e.GetPosition(wrap1);
            deltax -= point.X;
            Console.WriteLine(point);

            Point pointd;
            pointd = e.GetPosition(wrap1);

            if (draggedGrid != null) {
                Grid grid = draggedGrid;

                if (draggingPoint == DraggingPoint.End) {
                    grid.Width -= deltax;
                }
                else if (draggingPoint == DraggingPoint.Start) {
                    double mleft = grid.Margin.Left;
                    mleft -= deltax;
                    grid.Width += deltax;

                    grid.Margin = new Thickness(mleft, 0, 0, 0);
                }
                else if (draggingPoint == DraggingPoint.Middle) {
                    double mleft = grid.Margin.Left;
                    mleft -= deltax;
                    grid.Margin = new Thickness(mleft, 0, 0, 0);
                }
            }

        }

        int dragSize = 5;

        Point point;

        Grid draggedGrid;
        DraggingPoint draggingPoint;

        enum DraggingPoint {
            Start,
            Middle,
            End
        }

        private void grid1_MouseMove(object sender, MouseEventArgs e) {
            Grid grid = sender as Grid;
            Point point = e.GetPosition(grid);

            Cursor cursor = Cursors.Arrow;
            if (point.Y >= 0 && point.Y <= grid.Height) {
                if ((point.X >= 0 && point.X <= dragSize) ||
                    (point.X >= grid.Width - dragSize && point.X <= grid.Width)) {
                    cursor = Cursors.SizeWE;
                }
            }
            Cursor = cursor;
        }

        private void grid1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Grid grid = sender as Grid;
            Point point = e.GetPosition(grid);

            if (point.Y >= 0 && point.Y <= grid.Height) {
                if ((point.X >= 0 && point.X <= dragSize)) {
                    draggingPoint = DraggingPoint.Start;
                    draggedGrid = grid;
                }
                else if (point.X >= grid.Width - dragSize && point.X <= grid.Width) {
                    draggingPoint = DraggingPoint.End;
                    draggedGrid = grid;
                }
                else {
                    draggingPoint = DraggingPoint.Middle;
                    draggedGrid = grid;
                }
            }
        }

        private void grid1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            draggedGrid = null;

        }

        private void grid1_MouseLeave(object sender, MouseEventArgs e) {
            Cursor = Cursors.Arrow;
        }
    }
}
