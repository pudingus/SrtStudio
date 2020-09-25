using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SrtStudio
{
    public class Track
    {
        public Track(Timeline parentTimeline, bool locked)
        {
            ParentTimeline = parentTimeline;
            TrackHeader = new TrackHeader() {
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

            TrackHeader.resizeBorder.MouseMove += Header_ResizeBorder_MouseMove;
            TrackHeader.resizeBorder.MouseLeftButtonDown += Header_ResizeBorder_MouseLeftButtonDown;
            TrackHeader.resizeBorder.MouseLeftButtonUp += Header_ResizeBorder_MouseLeftButtonUp;
        } 

        public delegate void ChunkUpdatedEventHandler(object sender, Chunk chunk);

        public event ContextMenuEventHandler ChunkContextMenuOpening;
        public event ChunkUpdatedEventHandler ChunkUpdated;

        readonly List<Subtitle> streamedItems = new List<Subtitle>();
        ObservableCollection<Subtitle> items = new ObservableCollection<Subtitle>();
        double height = 100;
        Point prevPos;
        Point startPos;
        bool isEditingChunk = false;
        bool afterDblClick;
        enum ChunkBorder { Start, Middle, End }

        public bool Locked { get; }
        public ReadOnlyCollection<Subtitle> StreamedItems => streamedItems.AsReadOnly();
        public TrackHeader TrackHeader { get; }
        public Grid TrackContent { get; }
        public Timeline ParentTimeline { get; }

        public ObservableCollection<Subtitle> Items {
            get => items;
            set {
                items.CollectionChanged -= Items_CollectionChanged;
                items = value;
                items.CollectionChanged += Items_CollectionChanged;
                RecalculateStreamedSet();
                SetMinWidth();
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

        public void RecalculateStreamedSet()
        {
            streamedItems.Clear();
            TrackContent.Children.Clear();
            if (Items.Count > 0) {
                var hoffset = ParentTimeline.svHor.HorizontalOffset;
                var viewportWidth = ParentTimeline.svHor.ViewportWidth;

                var scrollHorizonLeft = ParentTimeline.PixelsToTime(hoffset);
                var scrollHorizonRight = ParentTimeline.PixelsToTime(viewportWidth + hoffset);

                foreach (Subtitle subtitle in Items) {
                    if (subtitle.Start <= scrollHorizonRight && subtitle.End >= scrollHorizonLeft) {
                        if (!streamedItems.Contains(subtitle)) {
                            streamedItems.Add(subtitle);
                            CreateChunk(subtitle);
                        }
                    }
                    else {
                        streamedItems.Remove(subtitle);
                        RemoveChunk(subtitle.Chunk);
                    }
                }
            }
        }

        void RemoveChunk(Chunk chunk) {
            TrackContent.Children.Remove(chunk);
        }

        void CreateChunk(Subtitle subtitle) {
            var chunk = new Chunk(subtitle) {
                ContextMenu = ParentTimeline.ChunkContextMenu
            };
            subtitle.Chunk = chunk;

            chunk.Locked = Locked;

            if (!Locked) {
                chunk.ContextMenuOpening += Chunk_ContextMenuOpening;

                chunk.startBorder.MouseMove += StartBorder_MouseMove;
                chunk.startBorder.MouseLeftButtonDown += StartBorder_MouseLeftButtonDown;
                chunk.startBorder.MouseLeftButtonUp += StartBorder_MouseLeftButtonUp;

                chunk.endBorder.MouseMove += EndBorder_MouseMove;
                chunk.endBorder.MouseLeftButtonDown += EndBorder_MouseLeftButtonDown;
                chunk.endBorder.MouseLeftButtonUp += EndBorder_MouseLeftButtonUp;

                chunk.middleBorder.MouseLeftButtonDown += MiddleBorder_MouseLeftButtonDown;
                chunk.middleBorder.MouseLeftButtonUp += MiddleBorder_MouseLeftButtonUp;
                chunk.middleBorder.MouseMove += MiddleBorder_MouseMove;
            }

            TrackContent.Children.Add(chunk);
            subtitle.PropertyChanged += Subtitle_PropertyChanged;
            UpdateChunkForSubtitle(subtitle);
        }

        int FindLowestIndex(IList items, IList collection) {
            //find lowest index
            int min = collection.IndexOf(items[0]);
            for (int i = 1; i < items.Count; i++) {
                int index = collection.IndexOf(items[i]);
                if (index < min) min = index;
            }

            return min;
        }

        void MoveSubtitle(Subtitle subtitle, TimeSpan timeDelta) {
            subtitle.Start += timeDelta;
            subtitle.End += timeDelta;
        }

        void TryResizeChunk(Chunk chunk, ChunkBorder chunkBorder) {
            Point position = Mouse.GetPosition(ParentTimeline.headerStack);
            double deltax = position.X - prevPos.X;
            double startdeltax = position.X - startPos.X;
            if (startdeltax >= 4.0 || startdeltax <= -4.0) {
                //Debug.WriteLine("after point " + DateTime.Now);
                if (isEditingChunk == false) {
                    isEditingChunk = true;
                    deltax += startdeltax;
                }
            }

            if (isEditingChunk) {
                if (chunkBorder == ChunkBorder.Start) {
                    ResizeChunk(chunk, -deltax, deltax);
                }

                else if (chunkBorder == ChunkBorder.End) {
                    ResizeChunk(chunk, deltax, 0);
                }

                //user is moving his mouse after he clicked in the middle of a chunk
                else if (chunkBorder == ChunkBorder.Middle) {
                    IList items = ParentTimeline.SelectedItems;
                    if (items.Count > 0) {
                        if (ParentTimeline.Ripple) {
                            //find lowest index
                            int min = FindLowestIndex(items, Items);
                            for (int i = min; i < Items.Count; i++) {
                                var subtitle = Items[i];
                                //cant modify chunks directly, coz they might not be streamed in
                                var timeDelta = ParentTimeline.PixelsToTime(deltax);
                                MoveSubtitle(subtitle, timeDelta);
                            }
                        }
                        else {
                            foreach (Subtitle subtitle in items) {
                                //cant modify chunks directly, coz they might not be streamed in
                                var timeDelta = ParentTimeline.PixelsToTime(deltax);
                                MoveSubtitle(subtitle, timeDelta);
                            }
                        }
                    }
                }
            }

            prevPos = position;
        }

        void UpdateChunkForSubtitle(Subtitle subtitle) {
            var margin = ParentTimeline.TimeToPixels(subtitle.Start);
            var width = ParentTimeline.TimeToPixels(subtitle.Duration);
            
            if (margin > 0 && width > 0) {
                subtitle.Chunk.Margin = new Thickness(margin, 0, 0, 0);
                subtitle.Chunk.Width = width;
            }
        }

        void ResizeChunk(Chunk chunk, double addWidth, double addLeftMargin) {
            var width = chunk.Width + addWidth;
            var margin = chunk.Margin.Left + addLeftMargin;

            if (width > 0 && margin > 0) {
                chunk.Width = width;
                chunk.Margin = new Thickness(margin, 0, 0, 0);
                ChunkUpdated?.Invoke(this, chunk);
            }
        }

        void SetMinWidth() {
            if (Items.Count > 0) {
                var lastIndex = Items.Count - 1;
                Subtitle lastSub = Items[lastIndex];
                var margin = ParentTimeline.TimeToPixels(lastSub.Start);
                var width = ParentTimeline.TimeToPixels(lastSub.Duration);
                var minWidth = margin + width + 100;
                if (TrackContent.MinWidth < minWidth) TrackContent.MinWidth = minWidth;
            }
        }

        void Header_ResizeBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            border.CaptureMouse();
        }

        void Header_ResizeBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            border.ReleaseMouseCapture();
        }

        void Header_ResizeBorder_MouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(TrackHeader);

            if (e.LeftButton == MouseButtonState.Pressed) {
                double deltaY = position.Y - prevPos.Y;
                TrackHeader.Height += deltaY;
                TrackContent.Height += deltaY;
            }

            prevPos = position;
        }

        void StartBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            border.CaptureMouse();
            startPos = e.GetPosition(ParentTimeline.headerStack);
        }

        void MiddleBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;           
            var chunk = (Chunk)border.DataContext;

            var ctrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            if (e.ClickCount == 1) {
                e.Handled = true;
                startPos = e.GetPosition(ParentTimeline.headerStack);
                border.CaptureMouse();

                //if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {

                // ParentTimeline.SelectedItems.Clear();
                //}
                //ParentTimeline.SelectedItems.Add(chunk.Subtitle);

                if (!isEditingChunk) {
                    if (ctrlPressed) {
                        if (ParentTimeline.SelectedItems.Contains(chunk.Subtitle)) {
                            ParentTimeline.SelectedItems.Remove(chunk.Subtitle);
                        }
                        else {
                            ParentTimeline.SelectedItems.Add(chunk.Subtitle);
                        }
                    }
                    else {
                        if (!ParentTimeline.SelectedItems.Contains(chunk.Subtitle)) {
                            ParentTimeline.SelectedItems.Clear();
                            ParentTimeline.SelectedItems.Add(chunk.Subtitle);
                        }
                    }                    
                }
            }
            if (e.ClickCount >= 2) {
                border.ReleaseMouseCapture();
                afterDblClick = true; //e.Handled is not enough
                isEditingChunk = false;

                ParentTimeline.SnapNeedleToChunkStart(chunk);
            }
        }

        void EndBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            border.CaptureMouse();
            startPos = e.GetPosition(ParentTimeline.headerStack);
        }

        void StartBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            border.ReleaseMouseCapture();
            e.Handled = true;
            if (!isEditingChunk) {
                var chunk = (Chunk)border.DataContext;
                ParentTimeline.SnapNeedleToChunkStart(chunk);
            }
            isEditingChunk = false;
        }

        void MiddleBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var chunk = (Chunk)border.DataContext;
            var ctrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            //stop routing the event to timeline mouseup, which puts needle under cursor
            //so we can put it there ourselves when needed
            e.Handled = true;

            //if (!isEditingChunk) {                
            //    if (!ctrlPressed) {
            //        ParentTimeline.SelectedItems.Clear();
            //    }
            //    ParentTimeline.SelectedItems.Add(chunk.Subtitle);
            //}

            if (!isEditingChunk && !afterDblClick && !ctrlPressed) {
                ParentTimeline.SnapNeedleToCursor();
            }


            if (afterDblClick) {
                afterDblClick = false;
            }
            else {
                border.ReleaseMouseCapture();
            }
            isEditingChunk = false;
        }
        
        void EndBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var chunk = (Chunk)border.DataContext;
            border.ReleaseMouseCapture();
            e.Handled = true;
            if (!isEditingChunk) {  
                ParentTimeline.SnapNeedleToChunkEnd(chunk);
            }
            isEditingChunk = false;
        }

        void StartBorder_MouseMove(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            var chunk = (Chunk)border.DataContext;
            if (e.LeftButton == MouseButtonState.Pressed && border.IsMouseCaptured) {
                TryResizeChunk(chunk, ChunkBorder.Start);
            }
        }

        void MiddleBorder_MouseMove(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            var chunk = (Chunk)border.DataContext;
            if (e.LeftButton == MouseButtonState.Pressed && border.IsMouseCaptured) {
                TryResizeChunk(chunk, ChunkBorder.Middle);
            }            
        }

        void EndBorder_MouseMove(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            var chunk = (Chunk)border.DataContext;
            if (e.LeftButton == MouseButtonState.Pressed && border.IsMouseCaptured) {
                TryResizeChunk(chunk, ChunkBorder.End);
            }
        }

        void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RecalculateStreamedSet();
            SetMinWidth();
        }

        void Track_ChunkUpdated(object sender, Chunk chunk)
        {
            var subtitle = (Subtitle)chunk.DataContext;

            //correct values in 'subtitle'
            subtitle.Start = ParentTimeline.PixelsToTime(chunk.Margin.Left);
            var dur = ParentTimeline.PixelsToTime(chunk.Width);
            subtitle.End = subtitle.Start + dur;
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
                    UpdateChunkForSubtitle(subtitle);
                    break;
            }
        } 

        void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ChunkContextMenuOpening?.Invoke(sender, e);
        }
    }
}
