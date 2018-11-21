using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Windows.Threading;

namespace SrtStudio
{
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl {

        public delegate void NeedleMoved();
        public event NeedleMoved OnNeedleMoved;


        public delegate void ChunkUpdated(Chunk chunk);
        public event ChunkUpdated OnChunkUpdated;



        TrackMeta draggedMeta;
        const int dragSize = 10;
        Point point;


        DispatcherTimer timer = new DispatcherTimer();
        public ObservableCollection<Item> SelectedItems { get; } = new ObservableCollection<Item>();
        bool beforetime = true;
        double startdeltax;
        bool afterPoint = false;
        Chunk draggedChunk;
        DraggingPoint draggingPoint;

        public enum DraggingPoint {
            Start,
            Middle,
            End
        }

        public Timeline() {
            InitializeComponent();

            SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;


            timer.Interval = new TimeSpan(0, 0, 0, 0, 150);
            timer.Tick += Timer_Tick;

            OnChunkUpdated += Timeline_OnChunkUpdated;
        }

        public readonly int timescale = 10;   //one page is 'scale' (30) seconds
        public readonly int pixelscale = 1000;

        private List<Track> _tracks = new List<Track>();
        public IEnumerable<Track> Tracks {
            get {
                return _tracks.AsReadOnly();
            }
        }


        public TimeSpan Position {
            get {
                double start = needle.Margin.Left / pixelscale * timescale;
                return TimeSpan.FromSeconds(start);
            }
            set {
                double margin = value.TotalSeconds / timescale * pixelscale;
                needle.Margin = new Thickness(margin, 0, 0, 0);
            }
        }

        public void FocusNeedle() {
            needle.BringIntoView(new Rect(new Size(50, 50)));
        }

        private void SelectedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (Item item in e.OldItems) {
                    item.Selected = false;
                }
            }

            if (e.NewItems != null) {
                foreach (Item item in e.NewItems) {
                    item.Selected = true;
                }
            }
        }

        public void AddTrack(Track track, bool toBottom = false) {
            track.TrackMeta.MouseMove += TrackMeta_MouseMove;
            track.TrackMeta.MouseLeftButtonDown += TrackMeta_MouseLeftButtonDown;
            track.TrackMeta.MouseLeftButtonUp += TrackMeta_MouseLeftButtonUp;
            track.TrackMeta.MouseLeave += TrackMeta_MouseLeave;


            if (toBottom) {
                stack.Children.Add(track.TrackLine);
                stackMeta.Children.Add(track.TrackMeta);
            }
            else {
                stack.Children.Insert(0, track.TrackLine);
                stackMeta.Children.Insert(0, track.TrackMeta);
            }

            _tracks.Add(track);
        }


        public void RemoveTrack(Track track) {
            stack.Children.Remove(track.TrackLine);
            stackMeta.Children.Remove(track.TrackMeta);

            _tracks.Remove(track);
        }

        public void ClearTracks() {
            stack.Children.Clear();
            stackMeta.Children.Clear();
        }


        public void AddChunk(Chunk chunk, Track track) {
            chunk.MouseMove += Chunk_MouseMove;
            chunk.MouseLeftButtonDown += Chunk_MouseLeftButtonDown;
            chunk.MouseLeftButtonUp += Chunk_MouseLeftButtonUp;
            chunk.MouseRightButtonDown += Chunk_MouseRightButtonDown;
            chunk.MouseEnter += Chunk_MouseEnter;
            chunk.MouseLeave += Chunk_MouseLeave;
            chunk.PreviewMouseDoubleClick += Chunk_PreviewMouseDoubleClick;

            if (track.Locked) {
                chunk.Locked = true;
                var bc = new BrushConverter();
                chunk.backRect.Fill = (Brush)bc.ConvertFrom("#FF3C3C3C");
            }
            //Brush brush = chunk.backRect.Fill;
            //Color color = ((SolidColorBrush)brush).Color;

            //color = Darken(color, 2.0f);


            //chunk.backRect.Stroke = new SolidColorBrush(color);

            track.TrackLine.Children.Add(chunk);

            //TrackLine.itemsControl.Items.Add(chunk);
            //TrackLine.ChunksSuper.Add(chunk);
        }


        public void RemoveChunk(Chunk chunk, Track track) {
            track.TrackLine.Children.Remove(chunk);
        }

        public void UnselectAll() {
            var copy = new ObservableCollection<Item>(SelectedItems);
            foreach (Item item in copy) {
                SelectedItems.Remove(item);
            }
        }

        public Color Darken(Color color, float perc) {
            var fcolor = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            fcolor = System.Windows.Forms.ControlPaint.Dark(fcolor, perc);
            return Color.FromRgb(fcolor.R, fcolor.G, fcolor.B);
        }



        private void TrackMeta_MouseMove(object sender, MouseEventArgs e) {
            TrackMeta trackMeta = sender as TrackMeta;
            Point pointe = e.GetPosition(trackMeta);

            Cursor cursor = Cursors.Arrow;
            if (pointe.X >= 0 && pointe.X <= trackMeta.ActualWidth) {
                if (pointe.Y >= trackMeta.ActualHeight - dragSize && pointe.Y <= trackMeta.ActualHeight) {
                    cursor = Cursors.SizeNS;
                }
            }
            Cursor = cursor;
        }

        private void TrackMeta_MouseLeave(object sender, MouseEventArgs e) {
            Cursor = Cursors.Arrow;
        }

        private void TrackMeta_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            TrackMeta trackMeta = sender as TrackMeta;
            Point pointe = e.GetPosition(trackMeta);

            if (pointe.X >= 0 && pointe.X <= trackMeta.ActualWidth) {
                if (pointe.Y >= trackMeta.ActualHeight - dragSize && pointe.Y <= trackMeta.ActualHeight) {
                    draggedMeta = trackMeta;
                    Mouse.Capture(trackMeta);
                }
            }
        }

        private void TrackMeta_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            //if (draggedMeta != null) {
            //    draggedMeta.ParentTrack.TrackLine.Height = draggedMeta.ActualHeight;
           // }

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


            TrackMeta trackMeta = draggedMeta;

            if (trackMeta != null) {
                trackMeta.Height -= deltay;
                draggedMeta.ParentTrack.TrackLine.Height = draggedMeta.ActualHeight;

            }

            if (draggedChunk != null) {
                startdeltax = point.X - startPoint.X;
                if (startdeltax >= 4.0 || startdeltax <= -4.0) {
                    Debug.WriteLine("after point " + DateTime.Now);
                    if (afterPoint == false) {
                        afterPoint = true;
                        deltax -= startdeltax;
                    }
                }

                if (afterPoint) {
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
                        foreach (Item item in SelectedItems) {
                            double mleft = item.Chunk.Margin.Left;
                            mleft -= deltax;
                            item.Chunk.Margin = new Thickness(mleft, 0, 0, 0);
                            OnChunkUpdated?.Invoke(item.Chunk);
                        }
                    }
                }
            }
        }

        private void Timeline_OnChunkUpdated(Chunk chunk) {
            Item item = (Item)chunk.DataContext;

            double start = chunk.Margin.Left / pixelscale * timescale;
            item.Start = TimeSpan.FromSeconds(start);

            double dur = chunk.Width / pixelscale * timescale;
            double end = start + dur;
            item.End = TimeSpan.FromSeconds(end);
        }


        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            ScrollViewer sv = (ScrollViewer)sender;
            int lines = System.Windows.Forms.SystemInformation.MouseWheelScrollLines;

            if (e.Delta > 0) {
                sv.LinesRight(lines);
            }
            else {
                sv.LinesLeft(lines);
            }

            e.Handled = true;
        }

        public ContextMenu ChunkContextMenu { get; set; }
        public event ContextMenuEventHandler ChunkContextMenuOpening;

        private void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            ChunkContextMenuOpening?.Invoke(sender, e);
        }


        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer sv = (ScrollViewer)sender;
            scrollbar.Maximum = sv.ScrollableWidth;
            scrollbar.ViewportSize = sv.ViewportWidth;
            scrollbar.Value = sv.HorizontalOffset;

            scrollbar.SmallChange = 0.00003 * sv.ScrollableWidth;
            scrollbar.LargeChange = 0.004 * sv.ScrollableWidth;


            //scrollbar.
            ////scrollbar.SmallChange = 1000;
            ////scrollbar.LargeChange = 1000;

            //Console.WriteLine("scroll changed");
            //Console.WriteLine(e.ViewportWidth);
            //Console.WriteLine(e.HorizontalOffset);
            //Console.WriteLine(e.ExtentWidth);
            //Console.WriteLine(sv.ScrollableWidth);

            //if (SuperList.Count < 1) return;
            if (e.HorizontalChange == 0) return;

            double val = svHor.HorizontalOffset / pixelscale * timescale;
            TimeSpan horizonLeft = TimeSpan.FromSeconds(val);
            val = (svHor.ViewportWidth + svHor.HorizontalOffset) / pixelscale * timescale;
            TimeSpan horizonRight = TimeSpan.FromSeconds(val);

            foreach (Track track in Tracks) {

                foreach (Item item in track.Super) {
                    if (item.Start <= horizonRight && item.End >= horizonLeft) {
                        if (!track.Streamed.Contains(item)) {
                            track.Streamed.Add(item);
                            Chunk chunk = new Chunk(this, item) {
                                ContextMenu = ChunkContextMenu
                            };
                            chunk.ContextMenuOpening += Chunk_ContextMenuOpening;
                            item.Chunk = chunk;
                            AddChunk(chunk, track);
                        }
                    }
                    else {
                        track.Streamed.Remove(item);
                        RemoveChunk(item.Chunk, track);
                    }
                }
            }
        }



        private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            svHor.ScrollToHorizontalOffset(e.NewValue);
        }

        bool seekbarDown = false;
        private void seekbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            Point pointe = e.GetPosition(stack);
            needle.Margin = new Thickness(pointe.X, 0, 0, 0);
            OnNeedleMoved?.Invoke();
            seekbarDown = true;
            Mouse.Capture(seekbar);
        }

        private void seekbar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (seekbarDown) {
                seekbarDown = false;
                Mouse.Capture(null);
            }
        }

        private void seekbar_MouseMove(object sender, MouseEventArgs e) {
            if (seekbarDown) {
                Point pointe = e.GetPosition(stack);
                needle.Margin = new Thickness(pointe.X, 0, 0, 0);
                OnNeedleMoved?.Invoke();
            }
        }



        private void UserControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (afterDblClick) {
                afterDblClick = false;
                return;
            }
            if (afterPoint && draggedChunk != null) return;


            if (!afterPoint && draggedChunk != null) {
                if (draggingPoint == DraggingPoint.Start) {
                    needle.Margin = new Thickness(draggedChunk.Margin.Left, 0, 0, 0);
                    OnNeedleMoved?.Invoke();
                }
                else if (draggingPoint == DraggingPoint.End) {
                    needle.Margin = new Thickness(draggedChunk.Margin.Left + draggedChunk.Width, 0, 0, 0);
                    OnNeedleMoved?.Invoke();
                }
                else {
                    Point pointe = e.GetPosition(stack);
                    needle.Margin = new Thickness(pointe.X, 0, 0, 0);
                    OnNeedleMoved?.Invoke();
                }


            }
            if (!afterPoint &&draggedChunk == null) {
                Point pointe = e.GetPosition(stack);
                needle.Margin = new Thickness(pointe.X, 0, 0, 0);
                OnNeedleMoved?.Invoke();
            }
        }

        bool afterDblClick;
        private void Chunk_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            afterDblClick = true;
            Chunk chunk = (Chunk)sender;

            needle.Margin = new Thickness(chunk.Margin.Left, 0, 0, 0);
            OnNeedleMoved?.Invoke();
            e.Handled = true;
        }

        private void Timer_Tick(object sender, EventArgs e) {
            beforetime = false;
            timer.Stop();
        }


        private void Chunk_MouseMove(object sender, MouseEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {
                Point point = e.GetPosition(chunk);

                Cursor cursor = Cursors.Arrow;
                if (point.Y >= 0 && point.Y <= chunk.ActualHeight) {
                    if ((point.X >= 0 && point.X <= dragSize) ||
                        (point.X >= chunk.Width - dragSize && point.X <= chunk.ActualWidth)) {
                        cursor = Cursors.SizeWE;
                    }
                }
                Cursor = cursor;
            }
        }


        private void Chunk_MouseEnter(object sender, MouseEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {
                chunk.hilitBorder.Visibility = Visibility.Visible;
            }
        }


        private void Chunk_MouseLeave(object sender, MouseEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {
                Cursor = Cursors.Arrow;

                chunk.hilitBorder.Visibility = Visibility.Hidden;
            }
        }

        Point startPoint;

        private void Chunk_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {
                Point pointc = e.GetPosition(chunk);
                startPoint = e.GetPosition(stackMeta);

                if (pointc.Y >= 0 && pointc.Y <= chunk.ActualHeight) {
                    if ((pointc.X >= 0 && pointc.X <= dragSize)) {
                        draggingPoint = DraggingPoint.Start;
                        draggedChunk = chunk;
                        Mouse.Capture(chunk);


                    }
                    else if (pointc.X >= chunk.ActualWidth - dragSize && pointc.X <= chunk.ActualWidth) {
                        draggingPoint = DraggingPoint.End;
                        draggedChunk = chunk;
                        Mouse.Capture(chunk);
                        startPoint = e.GetPosition(stackMeta);

                    }
                    else {
                        draggingPoint = DraggingPoint.Middle;
                        draggedChunk = chunk;
                        Mouse.Capture(chunk);
                    }
                }

                if (Keyboard.Modifiers == ModifierKeys.Control) {
                    if (!chunk.Item.Selected) {
                        SelectedItems.Add(chunk.Item);
                    }
                    else {
                        SelectedItems.Remove(chunk.Item);
                    }
                }
                else {
                    if (!chunk.Item.Selected) {

                        UnselectAll();
                        //SelectedChunks.Clear();
                        SelectedItems.Add(chunk.Item);
                    }
                }

                beforetime = true;
                timer.Start();
            }
        }


        private void Chunk_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {
                if (!chunk.Item.Selected) {

                    UnselectAll();
                    //SelectedChunks.Clear();
                    SelectedItems.Add(chunk.Item);
                }
            }
        }

        private void Chunk_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {

                draggedChunk = null;
                Mouse.Capture(null);
                startdeltax = 0.0;
                afterPoint = false;

                if (beforetime) {
                    if (Keyboard.Modifiers != ModifierKeys.Control) {
                        UnselectAll();
                        //SelectedChunks.Clear();
                        SelectedItems.Add(chunk.Item);
                    }
                }
            }
        }

    }
}
