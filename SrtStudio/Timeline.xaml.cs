using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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



        public Timeline() {
            InitializeComponent();

            SelectedChunks.CollectionChanged += SelectedChunks_CollectionChanged;
        }

        private void SelectedChunks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) {
                foreach (Chunk chunk in e.OldItems) {
                    chunk.Selected = false;
                }
            }

            if (e.NewItems != null) {
                foreach (Chunk chunk in e.NewItems) {
                    chunk.Selected = true;
                }
            }


        }

        public void RegisterHandlers(Chunk chunk) {
            chunk.MouseMove += grid_MouseMove;
            chunk.MouseLeftButtonDown += grid_MouseLeftButtonDown;
            chunk.MouseLeftButtonUp += grid_MouseLeftButtonUp;
            chunk.MouseLeave += grid_MouseLeave;
        }

        public void RegisterTrackMeta(TrackMeta trackMeta)
        {
            trackMeta.MouseMove += TrackMeta_MouseMove;
            trackMeta.MouseLeftButtonDown += TrackMeta_MouseLeftButtonDown;
            trackMeta.MouseLeftButtonUp += TrackMeta_MouseLeftButtonUp;
            trackMeta.MouseLeave += TrackMeta_MouseLeave;
        }

        public List<Chunk> List { get; set; } = new List<Chunk>();

        public ObservableCollection<Chunk> SelectedChunks { get; set; } = new ObservableCollection<Chunk>();



        private void TrackMeta_MouseMove(object sender, MouseEventArgs e)
        {
            TrackMeta trackMeta = sender as TrackMeta;
            Point pointe = e.GetPosition(trackMeta);

            Cursor cursor = Cursors.Arrow;
            if (pointe.X >= 0 && pointe.X <= trackMeta.Width) {
                if (pointe.Y >= trackMeta.Height - dragSize && pointe.Y <= trackMeta.Height) {
                    cursor = Cursors.SizeNS;
                }
            }
            Cursor = cursor;
        }


        private void TrackMeta_MouseLeave(object sender, MouseEventArgs e)
        {
            //Cursor = Cursors.Arrow;
        }

        TrackMeta draggedMeta;

        private void TrackMeta_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TrackMeta trackMeta = sender as TrackMeta;
            Point pointe = e.GetPosition(trackMeta);

            if (pointe.X >= 0 && pointe.X <= trackMeta.Width) {
                if (pointe.Y >= trackMeta.Height - dragSize && pointe.Y <= trackMeta.Height) {
                    draggedMeta = trackMeta;
                    Mouse.Capture(trackMeta);
                }
            }
        }

        private void TrackMeta_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedMeta != null && draggedMeta.Track != null) {
                draggedMeta.Track.Height = draggedMeta.Height;
            }

            draggedMeta = null;
            Mouse.Capture(null);
        }


        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e) {
            double deltax = point.X;
            double deltay = point.Y;
            point = e.GetPosition(stackMeta);
            deltax -= point.X;
            deltay -= point.Y;

            //Console.WriteLine(point);

            if (draggedChunk != null) {
                Chunk chunk = draggedChunk;

                if (draggingPoint == DraggingPoint.End) {
                    chunk.Width -= deltax;
                    OnChunkUpdated?.Invoke(chunk);

                }
                else if (draggingPoint == DraggingPoint.Start) {
                    double mleft = chunk.Margin.Left;
                    mleft -= deltax;
                    chunk.Width += deltax;

                    chunk.Margin = new Thickness(mleft, 0, 0, 0);
                    OnChunkUpdated?.Invoke(chunk);

                }
                else if (draggingPoint == DraggingPoint.Middle) {
                    foreach (Chunk chunkk in SelectedChunks) {
                        double mleft = chunkk.Margin.Left;
                        mleft -= deltax;
                        chunkk.Margin = new Thickness(mleft, 0, 0, 0);
                        OnChunkUpdated?.Invoke(chunkk);
                    }
                }
            }

            TrackMeta trackMeta = draggedMeta;

            if (trackMeta != null) {
                trackMeta.Height -= deltay;

                //if (trackMeta.Track != null)
                //    trackMeta.Track.Height -= deltay;
            }
        }

        int dragSize = 8;

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
            if (point.Y >= 0 && point.Y <= chunk.ActualHeight) {
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

            if (point.Y >= 0 && point.Y <= chunk.ActualHeight) {
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

            if (Keyboard.Modifiers == ModifierKeys.Control) {
                if (!chunk.Selected) {
                    chunk.Selected = true;
                    SelectedChunks.Add(chunk);
                }
                else {
                    chunk.Selected = false;
                    SelectedChunks.Remove(chunk);
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

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer sv = sender as ScrollViewer;
            Console.WriteLine("scroll changed");
            Console.WriteLine(e.ViewportWidth);
            Console.WriteLine(e.HorizontalOffset);
            Console.WriteLine(e.ExtentWidth);
            Console.WriteLine(sv.ScrollableWidth);



            scrollbar.Maximum = sv.ScrollableWidth;
            scrollbar.ViewportSize = e.ViewportWidth;
            scrollbar.Value = e.HorizontalOffset;
        }

        private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            Console.WriteLine("scrollbar scroll");
            svHor.ScrollToHorizontalOffset(e.NewValue);
        }
    }

}
