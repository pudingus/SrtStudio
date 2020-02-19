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


        public Timeline() {
            InitializeComponent();

            SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;

            Tracks.CollectionChanged += Tracks_CollectionChanged;
            
            ChunkUpdated += Timeline_ChunkUpdated;
        }


        public delegate void NeedleMovedEventHandler(object sender);
        public delegate void ChunkUpdatedEventHandler(object sender, Chunk chunk);


        public event NeedleMovedEventHandler    NeedleMoved;
        public event ChunkUpdatedEventHandler   ChunkUpdated;
        public event ContextMenuEventHandler    ChunkContextMenuOpening;

        



        

        public int Timescale => 10;   //one page is 'scale' (30) seconds
        public int Pixelscale => 1000;        

        public bool         Ripple              { get; set; }
        public ContextMenu  ChunkContextMenu    { get; set; }


        public ObservableCollection<Track>      Tracks          { get; } = new ObservableCollection<Track>();
        public ObservableCollection<Subtitle>   SelectedItems   { get; } = new ObservableCollection<Subtitle>();


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
                    foreach (Subtitle subtitle in track.Items) {
                        if (position >= subtitle.Start && position <= subtitle.End) {
                            track.ItemUnderNeedle = subtitle;
                            break;
                        }
                    }
                }                
            }
        }


        const int       DRAG_SIZE = 10;
              
        
        public enum DraggingPoint {
            None,
            Start,
            Middle,
            End,
        }

        DraggingPoint draggingPoint;

        Point prevPos;
        Point startPos;
        bool isEditingChunk = false;
        bool afterDblClick;
        bool draggingHeader = false;


        public void RevealNeedle() {
            needle.BringIntoView(new Rect(new Size(50, 50)));
        }

       
        public void AddChunkToTrack(Chunk chunk, Track track) { 
            var subtitle = (Subtitle)chunk.DataContext;
            chunk.Locked = track.Locked;
            if (!track.Locked) {
                chunk.MouseMove += Chunk_MouseMove;
                chunk.MouseLeftButtonDown += Chunk_MouseLeftButtonDown;
                chunk.MouseLeftButtonUp += Chunk_MouseLeftButtonUp;
                chunk.MouseRightButtonDown += Chunk_MouseRightButtonDown;
                chunk.MouseEnter += Chunk_MouseEnter;
                chunk.MouseLeave += Chunk_MouseLeave;
                chunk.ContextMenuOpening += Chunk_ContextMenuOpening;
            }
            track.TrackContent.Children.Add(chunk);
            subtitle.PropertyChanged += Item_PropertyChanged;
            UpdateSubtitleChunk(subtitle);
        }

        void UpdateSubtitleChunk(Subtitle subtitle) {
            double margin = subtitle.Start.TotalSeconds / Timescale * Pixelscale;
            subtitle.Chunk.Margin = new Thickness(margin, 0, 0, 0);
            subtitle.Chunk.Width = subtitle.Duration.TotalSeconds / Timescale * Pixelscale;
        }


        public void RemoveChunkFromTrack(Chunk chunk, Track track) {
            track.TrackContent.Children.Remove(chunk);
        }

        void Tracks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {

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

                        track.Items.CollectionChanged += (sender2, e2) => {
                            
                            switch (e2.Action) {
                                case NotifyCollectionChangedAction.Reset:
                                    track.StreamedItems.Clear();
                                    track.TrackContent.Children.Clear();
                                    break;

                                case NotifyCollectionChangedAction.Add:
                                    RecalculateStreamedSet(track);
                                    break;

                                case NotifyCollectionChangedAction.Remove:
                                    RecalculateStreamedSet(track);
                                    break;
                            }
                        };
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

        void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            var subtitle = (Subtitle)sender;            

            switch (e.PropertyName) {
                //case nameof(subtitle.Selected):
                //    subtitle.Chunk.selBorder.Visibility = subtitle.Selected ? Visibility.Visible : Visibility.Hidden;
                //    break;
                case nameof(subtitle.Start):
                case nameof(subtitle.Duration):
                    UpdateSubtitleChunk(subtitle);
                    break;
            }
        }

        
        void SelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {            
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                    foreach (Track track in Tracks) {
                        foreach (Subtitle subtitle in track.StreamedItems) {
                            subtitle.Chunk.Selected = false;
                        }
                    }
                    break;
            }            

            if (e.OldItems != null) {
                foreach (Subtitle sub in e.OldItems) {
                    sub.Chunk.Selected = false;
                }
            }

            if (e.NewItems != null) {
                foreach (Subtitle sub in e.NewItems) {
                    sub.Chunk.Selected = true;
                }
            }
        }


        void TrackHeader_MouseMove(object sender, MouseEventArgs e) {
            var trackHeader = (TrackHeader)sender;
            Point position = e.GetPosition(trackHeader);
            
            if (IsCursorAtHeaderResizeBorder(position, trackHeader)) {
                Cursor = Cursors.SizeNS;
            }
            else {
                Cursor = Cursors.Arrow;
            }
            

            if (draggingHeader == true) {
                double deltay = position.Y - prevPos.Y;

                trackHeader.Height += deltay;
                trackHeader.ParentTrack.TrackContent.Height = trackHeader.ActualHeight;
            }

            prevPos = position;
        }

        void TrackHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {            
            var trackHeader = (TrackHeader)sender;
            Point position = e.GetPosition(trackHeader);

            if (IsCursorAtHeaderResizeBorder(position, trackHeader)) {
                Mouse.Capture(trackHeader);
                draggingHeader = true;
            } 
        }

        void TrackHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { 
            if (draggingHeader == true) {
                Mouse.Capture(null);
                draggingHeader = false;
            }
        }

        void TrackHeader_MouseLeave(object sender, MouseEventArgs e) {
            Cursor = Cursors.Arrow;
        }

        void Timeline_ChunkUpdated(object sender, Chunk chunk) {            
            var subtitle = (Subtitle)chunk.DataContext;

            //correct values in 'subtitle'
            double start = chunk.Margin.Left / Pixelscale * Timescale;
            subtitle.Start = TimeSpan.FromSeconds(start);

            double dur = chunk.Width / Pixelscale * Timescale;
            double end = start + dur;
            subtitle.End = TimeSpan.FromSeconds(end);            
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


            if (e.HorizontalChange == 0) return;


            foreach (Track track in Tracks) {
                RecalculateStreamedSet(track);
            }
        }        

        void ScrollBar_Scroll(object sender, ScrollEventArgs e) {
            svHor.ScrollToHorizontalOffset(e.NewValue);
        }

        
        void Seekbar_MouseMove(object sender, MouseEventArgs e) {  
            if (e.LeftButton == MouseButtonState.Pressed) {
                Mouse.Capture(seekbar);
                SnapNeedleToCursor();
            }
            else {
                Mouse.Capture(null);               
            }
        }
               
        void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            ChunkContextMenuOpening?.Invoke(sender, e);
        }



        void Chunk_MouseMove(object sender, MouseEventArgs e) {
            var chunk = (Chunk)sender;
            Point position = e.GetPosition(headerStack);

            //resize kurzor po najetí na kraj chunku
            if (IsCursorHorizontallyAtChunkStartBorder(chunk) ||
                IsCursorHorizontallyAtChunkEndBorder(chunk)) 
            {
                Cursor = Cursors.SizeWE;
            }
            else {
                Cursor = Cursors.Arrow;
            }

            double deltax = position.X - prevPos.X;

            if (draggingPoint != DraggingPoint.None) {

                double startdeltax = position.X - startPos.X;
                if (startdeltax >= 4.0 || startdeltax <= -4.0) {
                    //Debug.WriteLine("after point " + DateTime.Now);
                    if (isEditingChunk == false) {
                        isEditingChunk = true;
                        deltax += startdeltax;
                    }
                }

                if (isEditingChunk) {
                    //Debug.WriteLine($"deltax: {deltax}");

                    if (draggingPoint == DraggingPoint.Start) {
                        ResizeChunk(chunk, -deltax, deltax);
                    }

                    else if (draggingPoint == DraggingPoint.End) {
                        ResizeChunk(chunk, deltax, 0);
                    }                    

                    //user is moving his mouse after he clicked in the middle of a chunk
                    if (draggingPoint == DraggingPoint.Middle) {
                        foreach (Subtitle subtitle in SelectedItems) {

                            //cant modify chunks directly, coz they might not be streamed in
                            TimeSpan timeDelta = TimeSpan.FromSeconds(deltax / Pixelscale * Timescale);
                            subtitle.Start += timeDelta;
                            subtitle.End += timeDelta;
                        }
                    }
                } 
            }

            prevPos = position;
        }

        void ResizeChunk(Chunk chunk, double addWidth, double addLeftMargin) {
            chunk.Width += addWidth;
            chunk.Margin = new Thickness(chunk.Margin.Left + addLeftMargin, 0, 0, 0);
            ChunkUpdated?.Invoke(this, chunk);
        }

        void Chunk_MouseEnter(object sender, MouseEventArgs e) {
            var chunk = (Chunk)sender;
            chunk.Hilit = true;            
        }

        void Chunk_MouseLeave(object sender, MouseEventArgs e) {
            var chunk = (Chunk)sender;
            chunk.Hilit = false;            
            Cursor = Cursors.Arrow;
        }        

        void Chunk_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {            
            var chunk = (Chunk)sender;
            startPos = e.GetPosition(headerStack);

            if (Keyboard.Modifiers != ModifierKeys.Control) {

                if (e.ClickCount == 1) {
                    Mouse.Capture(chunk);

                    if (IsCursorHorizontallyAtChunkStartBorder(chunk)) {
                        draggingPoint = DraggingPoint.Start;
                    }
                    else if (IsCursorHorizontallyAtChunkEndBorder(chunk)) {
                        draggingPoint = DraggingPoint.End;
                    }
                    else {
                        draggingPoint = DraggingPoint.Middle;
                    }
                }  
                if (e.ClickCount >= 2) {
                    afterDblClick = true; //e.Handled nestačí

                    draggingPoint = DraggingPoint.None;
                    isEditingChunk = false;
                    Mouse.Capture(null);

                    SnapNeedleToChunkStart(chunk);
                }
            }
        }        

        void Chunk_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var chunk = (Chunk)sender;
            e.Handled = true;


            //nedělat nic po puštění tl. po double clicku, ten je handled zvlášť
            if (afterDblClick) {
                afterDblClick = false;                
            }
            else {               
                if (!isEditingChunk) {

                    //když táhnu chunk a jsem před bodem snapu, (liberty zone) a ještě nic neroztahuju, tak po puštění ltm snapnout jehlu na začátek nebo konec chunku 
                    if (draggingPoint == DraggingPoint.Start) {
                        SnapNeedleToChunkStart(chunk);
                    }
                    else if (draggingPoint == DraggingPoint.End) {
                        SnapNeedleToChunkEnd(chunk);
                    }
                    //kliknul jsem doprostřed, tak dej jehlu kam jsem kliknul
                    else if (draggingPoint == DraggingPoint.Middle) {
                        SnapNeedleToCursor();
                    }
                }

                //když táhnu chunk a jsem za bodem snapu - už měním délku - tak po puštění levýho tlačítko nic dalšího nedělat

                draggingPoint = DraggingPoint.None;
                Mouse.Capture(null);
                isEditingChunk = false;
            }
        }

        void SnapNeedleToCursor() {
            Point position = Mouse.GetPosition(contentStack);
            needle.Margin = new Thickness(position.X, 0, 0, 0);
            NeedleMoved?.Invoke(this);
        }

        void Timeline_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            //kliknul jsem do volnýho místa - mimo chunk - tak tam prdni jehlu

            //myš je napravo od headeru a není na scrollbaru                 
            if ((e.GetPosition(headerStack).X > headerStack.ActualWidth) &&
                (e.GetPosition(scrollbar).Y < 0)) {

                SnapNeedleToCursor();
            }
        }

        void SnapNeedleToChunkEnd(Chunk chunk) {
            needle.Margin = new Thickness(chunk.Margin.Left + chunk.Width, 0, 0, 0);
            NeedleMoved?.Invoke(this);
        }

        void SnapNeedleToChunkStart(Chunk chunk) {
            needle.Margin = new Thickness(chunk.Margin.Left, 0, 0, 0);
            NeedleMoved?.Invoke(this);
        }

        void Chunk_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            
            var chunk = (Chunk)sender;
            var subtitle = (Subtitle)chunk.DataContext;
            if (!chunk.Selected) {
                SelectedItems.Clear();
                SelectedItems.Add(subtitle);                
            }            
        }


        #region Helper Methods
       
        public void RecalculateStreamedSet(Track track) {
            var scrollHorizonLeft = TimeSpan.FromSeconds(svHor.HorizontalOffset / Pixelscale * Timescale);
            var scrollHorizonRight = TimeSpan.FromSeconds((svHor.ViewportWidth + svHor.HorizontalOffset) / Pixelscale * Timescale);

            foreach (Subtitle subtitle in track.Items) {
                if (subtitle.Start <= scrollHorizonRight && subtitle.End >= scrollHorizonLeft) {
                    if (!track.StreamedItems.Contains(subtitle)) {
                        track.StreamedItems.Add(subtitle);
                        Chunk chunk = new Chunk(subtitle) {
                            ContextMenu = ChunkContextMenu
                        };
                        //chunk.ContextMenuOpening += Chunk_ContextMenuOpening;
                        subtitle.Chunk = chunk;
                        AddChunkToTrack(chunk, track);
                    }
                }
                else {
                    track.StreamedItems.Remove(subtitle);
                    RemoveChunkFromTrack(subtitle.Chunk, track);
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


        bool IsCursorVerticallyInChunkBounds(Chunk chunk) {
            var cursorPos = Mouse.GetPosition(chunk);
            return cursorPos.Y >= 0 && cursorPos.Y <= chunk.ActualHeight;
        }

        bool IsCursorHorizontallyAtChunkStartBorder(Chunk chunk) {
            var cursorPos = Mouse.GetPosition(chunk);
            return cursorPos.X >= 0 && cursorPos.X <= DRAG_SIZE;
        }


        bool IsCursorHorizontallyAtChunkEndBorder(Chunk chunk) {
            var cursorPos = Mouse.GetPosition(chunk);
            return cursorPos.X >= chunk.ActualWidth - DRAG_SIZE && cursorPos.X <= chunk.ActualWidth;
        }

        #endregion
    }
}
