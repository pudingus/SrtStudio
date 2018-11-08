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

        private void grid_MouseMove(object sender, MouseEventArgs e) {
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

        private void grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Grid grid = sender as Grid;
            Point point = e.GetPosition(grid);

            if (point.Y >= 0 && point.Y <= grid.Height) {
                if ((point.X >= 0 && point.X <= dragSize)) {
                    draggingPoint = DraggingPoint.Start;
                    draggedGrid = grid;
                    Mouse.Capture(grid);
                }
                else if (point.X >= grid.Width - dragSize && point.X <= grid.Width) {
                    draggingPoint = DraggingPoint.End;
                    draggedGrid = grid;
                    Mouse.Capture(grid);

                }
                else {
                    draggingPoint = DraggingPoint.Middle;
                    draggedGrid = grid;
                    Mouse.Capture(grid);

                }
            }
        }

        private void grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            draggedGrid = null;
            Mouse.Capture(null);

        }

        private void grid_MouseLeave(object sender, MouseEventArgs e) {
            Cursor = Cursors.Arrow;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            ScrollViewer scrollviewer = sender as ScrollViewer;
            if (e.Delta > 0)
                scrollviewer.LineLeft();
            else
                scrollviewer.LineRight();
            e.Handled = true;
        }
    }
}
