﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SrtStudio
{
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        const int DRAG_SIZE = 10;
        Point prevPos;
        bool draggingHeader = false;

        public Timeline()
        {
            InitializeComponent();
            SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
            Tracks.CollectionChanged += Tracks_CollectionChanged;
        }

        public delegate void NeedleMovedEventHandler(object sender);
        public event NeedleMovedEventHandler NeedleMoved;

        public int Timescale => 10;   //one page is 'scale' (30) seconds
        public int Pixelscale => 1000;
        public bool Ripple { get; set; }
        public ContextMenu ChunkContextMenu { get; set; }
        public ObservableCollection<Track> Tracks { get; } = new ObservableCollection<Track>();
        public ObservableCollection<Subtitle> SelectedItems { get; } = new ObservableCollection<Subtitle>();

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

        public void RevealNeedle()
        {
            needle.BringIntoView(new Rect(new Size(50, 50)));
        }

        public void SnapNeedleToCursor()
        {
            Point position = Mouse.GetPosition(contentStack);
            needle.Margin = new Thickness(position.X, 0, 0, 0);
            NeedleMoved?.Invoke(this);
        }

        public void SnapNeedleToChunkEnd(Chunk chunk)
        {
            needle.Margin = new Thickness(chunk.Margin.Left + chunk.Width, 0, 0, 0);
            NeedleMoved?.Invoke(this);
        }

        public void SnapNeedleToChunkStart(Chunk chunk)
        {
            needle.Margin = new Thickness(chunk.Margin.Left, 0, 0, 0);
            NeedleMoved?.Invoke(this);
        }

        void Tracks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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

        void SelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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

        void TrackHeader_MouseMove(object sender, MouseEventArgs e)
        {
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

        void TrackHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var trackHeader = (TrackHeader)sender;
            Point position = e.GetPosition(trackHeader);

            if (IsCursorAtHeaderResizeBorder(position, trackHeader)) {
                Mouse.Capture(trackHeader);
                draggingHeader = true;
            }
        }

        void TrackHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (draggingHeader == true) {
                Mouse.Capture(null);
                draggingHeader = false;
            }
        }

        void TrackHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
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
                track.RecalculateStreamedSet();
            }
        }

        void ScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            svHor.ScrollToHorizontalOffset(e.NewValue);
        }

        void Seekbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) {
                Mouse.Capture(seekbar);
                SnapNeedleToCursor();
            }
            else {
                Mouse.Capture(null);
            }
        }

        void Timeline_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //kliknul jsem do volnýho místa - mimo chunk - tak tam prdni jehlu

            //myš je napravo od headeru a není na scrollbaru                 
            if ((e.GetPosition(headerStack).X > headerStack.ActualWidth) &&
                (e.GetPosition(scrollbar).Y < 0)) {

                SnapNeedleToCursor();
            }
        }

        bool IsCursorHorizontallyInHeaderBounds(Point cursorPos, TrackHeader trackHeader)
        {
            return cursorPos.X >= 0 && cursorPos.X <= trackHeader.ActualWidth;
        }

        bool IsCursorVerticallyAtHeaderResizeBorder(Point cursorPos, TrackHeader trackHeader)
        {
            return cursorPos.Y >= trackHeader.ActualHeight - DRAG_SIZE && cursorPos.Y <= trackHeader.ActualHeight;
        }

        bool IsCursorAtHeaderResizeBorder(Point cursorPos, TrackHeader trackHeader)
        {
            return IsCursorHorizontallyInHeaderBounds(cursorPos, trackHeader) &&
                IsCursorVerticallyAtHeaderResizeBorder(cursorPos, trackHeader);
        }
    }
}
