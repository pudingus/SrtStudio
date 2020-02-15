using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        public delegate void NeedleMovedEventHandler(object sender);
        public event NeedleMovedEventHandler NeedleMoved;


        public delegate void ChunkUpdatedEventHandler(object sender, Chunk chunk);
        public event ChunkUpdatedEventHandler ChunkUpdated;

        public ObservableCollection<Item> SelectedItems { get; } = new ObservableCollection<Item>();

        public enum DraggingPoint {
            Start,
            Middle,
            End
        }

        public int Timescale => 10;   //one page is 'scale' (30) seconds
        public int Pixelscale => 1000;        

        public bool Ripple { get; set; }


        public ObservableCollection<Track> Tracks { get; } = new ObservableCollection<Track>();

        public TimeSpan Position {
            get {
                double start = needle.Margin.Left / Pixelscale * Timescale;
                return TimeSpan.FromSeconds(start);
            }
            set {
                double margin = value.TotalSeconds / Timescale * Pixelscale;
                needle.Margin = new Thickness(margin, 0, 0, 0);

                var position = value;
                foreach (Track track in Tracks) {                    
                    foreach (Item item in track.Items) {
                        if (position >= item.Start && position <= item.End) {
                            track.ItemUnderNeedle = item;
                            break;
                        }
                    }
                }                
            }
        }

        public ContextMenu ChunkContextMenu { get; set; }
        public event ContextMenuEventHandler ChunkContextMenuOpening;

        TrackHeader draggedHeader;
        const int DRAG_SIZE = 10;
        Point point;
        readonly DispatcherTimer clickTimer = new DispatcherTimer();
        bool beforetime = true;
        double startdeltax;
        bool afterPoint = false;
        Chunk draggedChunk;
        DraggingPoint draggingPoint;
        TimeSpan scrollHorizonLeft, scrollHorizonRight;
        bool seekbarDown = false;
        bool afterDblClick;
        Point startPoint;
        //bool chunkCtxMenu = false;

        public Timeline() {
            InitializeComponent();

            SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;

            Tracks.CollectionChanged += Tracks_CollectionChanged;

            clickTimer.Interval = new TimeSpan(0, 0, 0, 0, 150);
            clickTimer.Tick += ClickTimer_Tick;

            ChunkUpdated += Timeline_ChunkUpdated;
        }

        private void Tracks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {

            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                    contentStack.Children.Clear();
                    headerStack.Children.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                    foreach (Track track in e.NewItems) {
                        track.TrackHeader.MouseMove += TrackHeader_MouseMove;
                        track.TrackHeader.MouseLeftButtonDown += TrackHeader_MouseLeftButtonDown;
                        track.TrackHeader.MouseLeftButtonUp += TrackHeader_MouseLeftButtonUp;
                        track.TrackHeader.MouseLeave += TrackHeader_MouseLeave;

                        contentStack.Children.Insert(e.NewStartingIndex, track.TrackContent);
                        headerStack.Children.Insert(e.NewStartingIndex, track.TrackHeader);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (Track track in e.OldItems) {
                        contentStack.Children.Remove(track.TrackContent);
                        headerStack.Children.Remove(track.TrackHeader);
                    }
                    break;
            }            
        }

        public void RevealNeedle() {
            needle.BringIntoView(new Rect(new Size(50, 50)));
        }
        
        public void AddChunkToTrack(Chunk chunk, Track track) {
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

            track.TrackContent.Children.Add(chunk);
        }

        public void RemoveChunkFromTrack(Chunk chunk, Track track) {
            track.TrackContent.Children.Remove(chunk);
        }

        public void DeselectAll() {
            var copy = new ObservableCollection<Item>(SelectedItems);
            foreach (Item item in copy) {
                SelectedItems.Remove(item);
            }
        }

        Color Darken(Color color, float perc) {
            var fcolor = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            fcolor = System.Windows.Forms.ControlPaint.Dark(fcolor, perc);
            return Color.FromRgb(fcolor.R, fcolor.G, fcolor.B);
        }

        void SelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
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

        

        void TrackHeader_MouseMove(object sender, MouseEventArgs e) {
            TrackHeader trackHeader = sender as TrackHeader;
            Point pointe = e.GetPosition(trackHeader);

            Cursor cursor = Cursors.Arrow;
            if (IsCursorAtHeaderResizeBorder(pointe, trackHeader)) {
                cursor = Cursors.SizeNS;
            }
           
            Cursor = cursor;
        }

        void TrackHeader_MouseLeave(object sender, MouseEventArgs e) {
            Cursor = Cursors.Arrow;
        }

        void TrackHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            TrackHeader trackHeader = sender as TrackHeader;
            Point pointe = e.GetPosition(trackHeader);

            if (IsCursorAtHeaderResizeBorder(pointe, trackHeader)) {
                draggedHeader = trackHeader;
                Mouse.Capture(trackHeader);
            }            
        }

        void TrackHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            draggedHeader = null;
            Mouse.Capture(null);
        }

        void UserControl_PreviewMouseMove(object sender, MouseEventArgs e) {
            double deltax = point.X;
            double deltay = point.Y;
            point = e.GetPosition(headerStack);
            deltax -= point.X;
            deltay -= point.Y;
            //Console.WriteLine(point);


            TrackHeader trackHeader = draggedHeader;

            if (trackHeader != null) {
                trackHeader.Height -= deltay;
                draggedHeader.ParentTrack.TrackContent.Height = draggedHeader.ActualHeight;

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
                        ChunkUpdated?.Invoke(this, chunk);

                    }
                    else if (draggingPoint == DraggingPoint.Start) {
                        double mleft = chunk.Margin.Left;
                        mleft -= deltax;
                        chunk.Width += deltax;

                        chunk.Margin = new Thickness(mleft, 0, 0, 0);
                        ChunkUpdated?.Invoke(this, chunk);

                    }
                    //user is moving his mouse after he clicked in the middle of a chunk
                    else if (draggingPoint == DraggingPoint.Middle) {
                        if (!Ripple) {
                            foreach (Item item in SelectedItems) {

                                TimeSpan timeDelta = TimeSpan.FromSeconds(deltax / Pixelscale * Timescale);
                                item.Start -= timeDelta;
                                item.End -= timeDelta;

                                ////old way
                                //double mleft = item.Chunk.Margin.Left;
                                //mleft -= deltax;
                                //item.Chunk.Margin = new Thickness(mleft, 0, 0, 0);
                                //OnChunkUpdated?.Invoke(item.Chunk);
                            }
                            UpdateStreamedChunks(draggedChunk.ParentTrack);
                            RecalculateStreamedSet(draggedChunk.ParentTrack);

                        }
                        else {
                            //loop from selected item forward
                            for (int i = draggedChunk.Item.Index-1; i < draggedChunk.ParentTrack.Items.Count; i++) {
                                Item item = draggedChunk.ParentTrack.Items[i];
                                TimeSpan timeDelta = TimeSpan.FromSeconds(deltax / Pixelscale * Timescale);
                                item.Start -= timeDelta;
                                item.End -= timeDelta;

                                //Item item = draggedChunk.ParentTrack.Super[i];
                                //double mleft = item.Chunk.Margin.Left;
                                //mleft -= deltax;
                                //item.Chunk.Margin = new Thickness(mleft, 0, 0, 0);
                                //OnChunkUpdated?.Invoke(item.Chunk);
                            }
                            UpdateStreamedChunks(draggedChunk.ParentTrack);
                            RecalculateStreamedSet(draggedChunk.ParentTrack);
                        }

                    }
                }
            }
        }

        void UserControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (afterDblClick) {
                afterDblClick = false;
                return;
            }
            if (afterPoint && draggedChunk != null) return;

            //disable seeking while multiselecting chunks with ctrl
            if (Keyboard.Modifiers == ModifierKeys.Control) return;

            if (!afterPoint && draggedChunk != null) {
                if (draggingPoint == DraggingPoint.Start) {
                    needle.Margin = new Thickness(draggedChunk.Margin.Left, 0, 0, 0);
                    NeedleMoved?.Invoke(this);
                }
                else if (draggingPoint == DraggingPoint.End) {
                    needle.Margin = new Thickness(draggedChunk.Margin.Left + draggedChunk.Width, 0, 0, 0);
                    NeedleMoved?.Invoke(this);
                }
                else {
                    Point pointe = e.GetPosition(contentStack);
                    needle.Margin = new Thickness(pointe.X, 0, 0, 0);
                    NeedleMoved?.Invoke(this);
                }


            }
            if (!afterPoint && draggedChunk == null) {
                Point pointe = e.GetPosition(contentStack);

                Point pointscr = e.GetPosition(scrollbar);

                Debug.WriteLine($"{pointe.X} {pointe.Y}");
                Debug.WriteLine($"{pointscr.X} {pointscr.Y}");



                //not on scrollbar
                if (pointscr.Y < 0) {


                    if (point.X > headerStack.ActualWidth) {
                        needle.Margin = new Thickness(pointe.X, 0, 0, 0);
                        NeedleMoved?.Invoke(this);
                        Debug.WriteLine("DIS!");
                    }
                }


            }
        }

        void UserControl_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            //dirty hack
            //Task.Delay(2).ContinueWith(t => {
            //    Dispatcher.Invoke(() => {
            //        if (!chunkCtxMenu) {
            //            if (!afterPoint &&draggedChunk == null) {
            //                Point pointe = Mouse.GetPosition(stack);
            //                needle.Margin = new Thickness(pointe.X, 0, 0, 0);
            //                OnNeedleMoved?.Invoke();
            //            }
            //        }
            //        chunkCtxMenu = false;
            //    });
            //});
        }



        void Timeline_ChunkUpdated(object sender, Chunk chunk) {
            Item item = (Item)chunk.DataContext;

            //correct values in 'item'
            double start = chunk.Margin.Left / Pixelscale * Timescale;
            item.Start = TimeSpan.FromSeconds(start);

            double dur = chunk.Width / Pixelscale * Timescale;
            double end = start + dur;
            item.End = TimeSpan.FromSeconds(end);
        }

        void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
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

        void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
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

            double val = svHor.HorizontalOffset / Pixelscale * Timescale;
            scrollHorizonLeft = TimeSpan.FromSeconds(val);
            val = (svHor.ViewportWidth + svHor.HorizontalOffset) / Pixelscale * Timescale;
            scrollHorizonRight = TimeSpan.FromSeconds(val);

            foreach (Track track in Tracks) {
                RecalculateStreamedSet(track);
            }
        }        

        void ScrollBar_Scroll(object sender, ScrollEventArgs e) {
            svHor.ScrollToHorizontalOffset(e.NewValue);
        }

        
        void Seekbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            Point pointe = e.GetPosition(contentStack);
            needle.Margin = new Thickness(pointe.X, 0, 0, 0);
            NeedleMoved?.Invoke(this);
            seekbarDown = true;
            Mouse.Capture(seekbar);
        }

        void Seekbar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (seekbarDown) {
                seekbarDown = false;
                Mouse.Capture(null);
            }
        }

        void Seekbar_MouseMove(object sender, MouseEventArgs e) {
            if (seekbarDown) {
                Point pointe = e.GetPosition(contentStack);
                needle.Margin = new Thickness(pointe.X, 0, 0, 0);
                NeedleMoved?.Invoke(this);
            }
        }
        


        void ClickTimer_Tick(object sender, EventArgs e) {
            beforetime = false;
            clickTimer.Stop();
        }

        void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            ChunkContextMenuOpening?.Invoke(sender, e);
            //chunkCtxMenu = true;
        }    
        
        void Chunk_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            afterDblClick = true;
            var chunk = (Chunk)sender;

            needle.Margin = new Thickness(chunk.Margin.Left, 0, 0, 0);
            NeedleMoved?.Invoke(this);
            e.Handled = true;
        }


        void Chunk_MouseMove(object sender, MouseEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {
                Point point = e.GetPosition(chunk);

                Cursor cursor = Cursors.Arrow;
                if (IsCursorVerticallyInChunkBounds(point, chunk)) {
                    if (IsCursorHorizontallyAtStartBorder(point) || IsCursorHorizontallyAtEndBorder(point, chunk)) {
                        cursor = Cursors.SizeWE;
                    }
                }
                Cursor = cursor;
            }
        }

        void Chunk_MouseEnter(object sender, MouseEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {
                chunk.hilitBorder.Visibility = Visibility.Visible;
            }
        }

        void Chunk_MouseLeave(object sender, MouseEventArgs e) {
            Chunk chunk = (Chunk)sender;
            if (!chunk.Locked) {
                Cursor = Cursors.Arrow;

                chunk.hilitBorder.Visibility = Visibility.Hidden;
            }
        }       

        

        void Chunk_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var chunk = (Chunk)sender;
            if (!chunk.Locked) {
                Point pointc = e.GetPosition(chunk);
                startPoint = e.GetPosition(headerStack);
                                
                if (IsCursorVerticallyInChunkBounds(pointc, chunk)) {
                    if (IsCursorHorizontallyAtStartBorder(pointc)) {
                        draggingPoint = DraggingPoint.Start;
                        draggedChunk = chunk;
                        Mouse.Capture(chunk);
                    }
                    else if (IsCursorHorizontallyAtEndBorder(pointc, chunk)) {
                        draggingPoint = DraggingPoint.End;
                        draggedChunk = chunk;
                        Mouse.Capture(chunk);
                        startPoint = e.GetPosition(headerStack);

                    }
                    //Middle
                    else {
                        draggingPoint = DraggingPoint.Middle;
                        draggedChunk = chunk;
                        Mouse.Capture(chunk);

                        if (Ripple) {

                        }
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

                        DeselectAll();
                        //SelectedChunks.Clear();
                        SelectedItems.Add(chunk.Item);
                    }
                }

                beforetime = true;
                clickTimer.Start();
            }
        }

        void Chunk_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            var chunk = (Chunk)sender;
            if (!chunk.Locked) {
                if (!chunk.Item.Selected) {

                    DeselectAll();
                    //SelectedChunks.Clear();
                    SelectedItems.Add(chunk.Item);
                }
            }
        }

        void Chunk_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var chunk = (Chunk)sender;
            if (!chunk.Locked) {

                draggedChunk = null;
                Mouse.Capture(null);
                startdeltax = 0.0;
                afterPoint = false;

                if (beforetime) {
                    if (Keyboard.Modifiers != ModifierKeys.Control) {
                        DeselectAll();
                        //SelectedChunks.Clear();
                        SelectedItems.Add(chunk.Item);
                    }
                }
            }
        }

        #region Helper Methods
        void UpdateStreamedChunks(Track track) {
            foreach (Item item in track.StreamedItems) {
                item.Chunk.Update();
            }
        }
        void RecalculateStreamedSet(Track track) {

            foreach (Item item in track.Items) {
                if (item.Start <= scrollHorizonRight && item.End >= scrollHorizonLeft) {
                    if (!track.StreamedItems.Contains(item)) {
                        track.StreamedItems.Add(item);
                        Chunk chunk = new Chunk(track, item) {
                            ContextMenu = ChunkContextMenu
                        };
                        chunk.ContextMenuOpening += Chunk_ContextMenuOpening;
                        item.Chunk = chunk;
                        AddChunkToTrack(chunk, track);
                    }
                }
                else {
                    track.StreamedItems.Remove(item);
                    RemoveChunkFromTrack(item.Chunk, track);
                }
            }
        }


        bool IsCursorHorizontallyInHeaderBounds(Point cursorPos, TrackHeader trackHeader) {
            return cursorPos.X >= 0 && cursorPos.X <= trackHeader.ActualWidth;
        }

        bool IsCursorVerticallyAtHeaderResizeBorder(Point cursorPos, TrackHeader trackHeader) {
            return cursorPos.Y >= trackHeader.ActualHeight - DRAG_SIZE && cursorPos.Y <= trackHeader.ActualHeight;
        }

        bool IsCursorAtHeaderResizeBorder(Point cursorPos, TrackHeader trackHeader) {
            return IsCursorHorizontallyInHeaderBounds(cursorPos, trackHeader) &&
                IsCursorVerticallyAtHeaderResizeBorder(cursorPos, trackHeader);
        }


        bool IsCursorVerticallyInChunkBounds(Point cursorPos, Chunk chunk) {
            return cursorPos.Y >= 0 && cursorPos.Y <= chunk.ActualHeight;
        }

        bool IsCursorHorizontallyAtStartBorder(Point cursorPos) {
            return cursorPos.X >= 0 && cursorPos.X <= DRAG_SIZE;
        }

        bool IsCursorHorizontallyAtEndBorder(Point cursorPos, Chunk chunk) {
            return cursorPos.X >= chunk.ActualWidth - DRAG_SIZE && cursorPos.X <= chunk.ActualWidth;
        }

        #endregion
    }
}
