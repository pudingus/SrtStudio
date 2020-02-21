using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SrtStudio
{
    public class Track
    {
        const int DRAG_SIZE = 10;
        ObservableCollection<Subtitle> items = new ObservableCollection<Subtitle>();
        double height = 100;
        DraggingPoint draggingPoint;
        Point prevPos;
        Point startPos;
        bool isEditingChunk = false;
        bool afterDblClick;

        public Track(Timeline parentTimeline, bool locked)
        {
            ParentTimeline = parentTimeline;
            TrackHeader = new TrackHeader(this) {
                Height = height
            };
            TrackContent = new Grid {
                Height = height,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Locked = locked;
            ChunkUpdated += Track_ChunkUpdated;
            Items.CollectionChanged += Items_CollectionChanged;
        }

        public delegate void ChunkUpdatedEventHandler(object sender, Chunk chunk);

        public event ContextMenuEventHandler ChunkContextMenuOpening;
        public event ChunkUpdatedEventHandler ChunkUpdated;

        public enum DraggingPoint
        {
            None,
            Start,
            Middle,
            End,
        }

        public bool Locked { get; }
        public List<Subtitle> StreamedItems { get; } = new List<Subtitle>();
        public Subtitle ItemUnderNeedle { get; set; }
        public TrackHeader TrackHeader { get; }
        public Grid TrackContent { get; }
        public Timeline ParentTimeline { get; }

        public ObservableCollection<Subtitle> Items {
            get => items;
            set {
                items = value;
                RecalculateStreamedSet();
            }
        }

        public string Name {
            get => TrackHeader.trackName.Text;
            set => TrackHeader.trackName.Text = value;
        }

        public double Height {
            get => height;
            set {
                height = value;
                TrackHeader.Height = height;
                TrackContent.Height = height;
            }
        }

        public void RemoveChunk(Chunk chunk)
        {
            TrackContent.Children.Remove(chunk);
        }

        public void RecalculateStreamedSet()
        {
            var scrollHorizonLeft = TimeSpan.FromSeconds(ParentTimeline.svHor.HorizontalOffset / ParentTimeline.Pixelscale * ParentTimeline.Timescale);
            var scrollHorizonRight = TimeSpan.FromSeconds((ParentTimeline.svHor.ViewportWidth + ParentTimeline.svHor.HorizontalOffset) / ParentTimeline.Pixelscale * ParentTimeline.Timescale);

            foreach (Subtitle subtitle in Items) {
                if (subtitle.Start <= scrollHorizonRight && subtitle.End >= scrollHorizonLeft) {
                    if (!StreamedItems.Contains(subtitle)) {
                        StreamedItems.Add(subtitle);
                        CreateChunk(subtitle);
                    }
                }
                else {
                    StreamedItems.Remove(subtitle);
                    RemoveChunk(subtitle.Chunk);
                }
            }
        }

        void CreateChunk(Subtitle subtitle)
        {
            var chunk = new Chunk(subtitle) {
                ContextMenu = ParentTimeline.ChunkContextMenu
            };
            subtitle.Chunk = chunk;

            chunk.Locked = Locked;

            if (!Locked) {
                chunk.MouseMove += Chunk_MouseMove;
                chunk.MouseLeftButtonDown += Chunk_MouseLeftButtonDown;
                chunk.MouseLeftButtonUp += Chunk_MouseLeftButtonUp;
                chunk.MouseRightButtonDown += Chunk_MouseRightButtonDown;
                chunk.MouseEnter += Chunk_MouseEnter;
                chunk.MouseLeave += Chunk_MouseLeave;
                chunk.ContextMenuOpening += Chunk_ContextMenuOpening;
            }

            TrackContent.Children.Add(chunk);
            subtitle.PropertyChanged += Subtitle_PropertyChanged;
            UpdateSubtitleChunk(subtitle);
        }

        void UpdateSubtitleChunk(Subtitle subtitle)
        {
            double margin = subtitle.Start.TotalSeconds / ParentTimeline.Timescale * ParentTimeline.Pixelscale;
            subtitle.Chunk.Margin = new Thickness(margin, 0, 0, 0);
            subtitle.Chunk.Width = subtitle.Duration.TotalSeconds / ParentTimeline.Timescale * ParentTimeline.Pixelscale;
        }

        void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                    StreamedItems.Clear();
                    TrackContent.Children.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                    RecalculateStreamedSet();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RecalculateStreamedSet();
                    break;
            }
        }

        void Track_ChunkUpdated(object sender, Chunk chunk)
        {
            var subtitle = (Subtitle)chunk.DataContext;

            //correct values in 'subtitle'
            double start = chunk.Margin.Left / ParentTimeline.Pixelscale * ParentTimeline.Timescale;
            subtitle.Start = TimeSpan.FromSeconds(start);

            double dur = chunk.Width / ParentTimeline.Pixelscale * ParentTimeline.Timescale;
            double end = start + dur;
            subtitle.End = TimeSpan.FromSeconds(end);
        }

        void Subtitle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
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

        void Chunk_MouseMove(object sender, MouseEventArgs e)
        {
            var chunk = (Chunk)sender;
            Point position = e.GetPosition(ParentTimeline.headerStack);

            //resize kurzor po najetí na kraj chunku
            ParentTimeline.Cursor = IsCursorHorizontallyAtChunkStartBorder(chunk) ||
                IsCursorHorizontallyAtChunkEndBorder(chunk)
                ? Cursors.SizeWE
                : Cursors.Arrow;

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
                        foreach (Subtitle subtitle in ParentTimeline.SelectedItems) {

                            //cant modify chunks directly, coz they might not be streamed in
                            TimeSpan timeDelta = TimeSpan.FromSeconds(deltax / ParentTimeline.Pixelscale * ParentTimeline.Timescale);
                            subtitle.Start += timeDelta;
                            subtitle.End += timeDelta;
                        }
                    }
                }
            }
            prevPos = position;
        }

        void Chunk_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chunk = (Chunk)sender;
            startPos = e.GetPosition(ParentTimeline.headerStack);

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

                    ParentTimeline.SnapNeedleToChunkStart(chunk);
                }
            }
        }

        void Chunk_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
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
                        ParentTimeline.SnapNeedleToChunkStart(chunk);
                    }
                    else if (draggingPoint == DraggingPoint.End) {
                        ParentTimeline.SnapNeedleToChunkEnd(chunk);
                    }
                    //kliknul jsem doprostřed, tak dej jehlu kam jsem kliknul
                    else if (draggingPoint == DraggingPoint.Middle) {
                        ParentTimeline.SnapNeedleToCursor();
                    }
                }

                //když táhnu chunk a jsem za bodem snapu - už měním délku - tak po puštění levýho tlačítko nic dalšího nedělat

                draggingPoint = DraggingPoint.None;
                Mouse.Capture(null);
                isEditingChunk = false;
            }
        }

        void ResizeChunk(Chunk chunk, double addWidth, double addLeftMargin)
        {
            chunk.Width += addWidth;
            chunk.Margin = new Thickness(chunk.Margin.Left + addLeftMargin, 0, 0, 0);
            ChunkUpdated?.Invoke(this, chunk);
        }

        bool IsCursorHorizontallyAtChunkStartBorder(Chunk chunk)
        {
            var cursorPos = Mouse.GetPosition(chunk);
            return cursorPos.X >= 0 && cursorPos.X <= DRAG_SIZE;
        }

        bool IsCursorHorizontallyAtChunkEndBorder(Chunk chunk)
        {
            var cursorPos = Mouse.GetPosition(chunk);
            return cursorPos.X >= chunk.ActualWidth - DRAG_SIZE && cursorPos.X <= chunk.ActualWidth;
        }

        void Chunk_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chunk = (Chunk)sender;
            var subtitle = (Subtitle)chunk.DataContext;
            if (!chunk.Selected) {
                ParentTimeline.SelectedItems.Clear();
                ParentTimeline.SelectedItems.Add(subtitle);
            }
        }

        void Chunk_MouseEnter(object sender, MouseEventArgs e)
        {
            var chunk = (Chunk)sender;
            chunk.Hilit = true;
        }

        void Chunk_MouseLeave(object sender, MouseEventArgs e)
        {
            var chunk = (Chunk)sender;
            chunk.Hilit = false;
            ParentTimeline.Cursor = Cursors.Arrow;   //cursor
        }

        void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ChunkContextMenuOpening?.Invoke(sender, e);
        }
    }
}
