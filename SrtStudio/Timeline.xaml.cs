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

        public delegate void ChunkUpdated(Chunk chunk);
        public event ChunkUpdated OnChunkUpdated;

        public string trackName {
            get { return TrackName.Text; }
            set { TrackName.Text = value; }
        }


        public Timeline() {
            InitializeComponent();
        }


        //public double width {
        //    get { return grid1.Width; }
        //    set { grid1.Width = value; }
        //}

        //public double mLeft {
        //    get { return grid1.Margin.Left; }
        //    set { grid1.Margin = new Thickness(value, 0, 0, 0); }
        //}

        public void RegisterHandlers(Chunk chunk) {
            chunk.MouseMove += grid_MouseMove;
            chunk.MouseLeftButtonDown += grid_MouseLeftButtonDown;
            chunk.MouseLeftButtonUp += grid_MouseLeftButtonUp;
            chunk.MouseLeave += grid_MouseLeave;
        }

        public List<Chunk> List { get; set; } = new List<Chunk>();

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e) {
            double deltax = point.X;
            point = e.GetPosition(wrap1);
            deltax -= point.X;
            Console.WriteLine(point);

            Point pointd;
            pointd = e.GetPosition(wrap1);

            if (draggedChunk != null) {
                Chunk chunk = draggedChunk;

                if (draggingPoint == DraggingPoint.End) {
                    chunk.Width -= deltax;
                }
                else if (draggingPoint == DraggingPoint.Start) {
                    double mleft = chunk.Margin.Left;
                    mleft -= deltax;
                    chunk.Width += deltax;

                    chunk.Margin = new Thickness(mleft, 0, 0, 0);
                }
                else if (draggingPoint == DraggingPoint.Middle) {
                    double mleft = chunk.Margin.Left;
                    mleft -= deltax;
                    chunk.Margin = new Thickness(mleft, 0, 0, 0);
                }


                OnChunkUpdated?.Invoke(chunk);
            }
        }

        int dragSize = 5;

        Point point;

        Chunk draggedChunk;
        DraggingPoint draggingPoint;

        enum DraggingPoint {
            Start,
            Middle,
            End
        }

        private void grid_MouseMove(object sender, MouseEventArgs e) {
            Chunk chunk = (Chunk)sender;
            Point point = e.GetPosition(chunk);

            Cursor cursor = Cursors.Arrow;
            if (point.Y >= 0 && point.Y <= chunk.Height) {
                if ((point.X >= 0 && point.X <= dragSize) ||
                    (point.X >= chunk.Width - dragSize && point.X <= chunk.Width)) {
                    cursor = Cursors.SizeWE;
                }
            }
            Cursor = cursor;
        }

        private void grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Chunk chunk = (Chunk)sender;
            Point point = e.GetPosition(chunk);

            if (point.Y >= 0 && point.Y <= chunk.Height) {
                if ((point.X >= 0 && point.X <= dragSize)) {
                    draggingPoint = DraggingPoint.Start;
                    draggedChunk = chunk;
                    Mouse.Capture(chunk);
                }
                else if (point.X >= chunk.Width - dragSize && point.X <= chunk.Width) {
                    draggingPoint = DraggingPoint.End;
                    draggedChunk = chunk;
                    Mouse.Capture(chunk);
                }
                else {
                    draggingPoint = DraggingPoint.Middle;
                    draggedChunk = chunk;
                    Mouse.Capture(chunk);
                }
            }
        }

        private void grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            draggedChunk = null;
            Mouse.Capture(null);

        }

        private void grid_MouseLeave(object sender, MouseEventArgs e) {
            Cursor = Cursors.Arrow;
        }

        int lines = System.Windows.Forms.SystemInformation.MouseWheelScrollLines;



        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            ScrollViewer scrollviewer = sender as ScrollViewer;
            if (e.Delta > 0) {

                scrollviewer.LinesRight(lines);
            }
            else {
                scrollviewer.LinesLeft(lines);
            }

            e.Handled = true;
        }
    }
    public static class Extensions {

        public static void LinesLeft(this ScrollViewer scroll, int lines) {
            for (int i = 0; i < lines; i++)
                scroll.LineLeft();
        }

        public static void LinesRight(this ScrollViewer scroll, int lines) {
            for (int i = 0; i < lines; i++)
                scroll.LineRight();
        }
    }
}
