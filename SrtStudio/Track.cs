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

        public bool Locked { get; }
        public ReadOnlyCollection<Subtitle> StreamedItems => streamedItems.AsReadOnly();
        public Subtitle ItemUnderNeedle { get; set; }
        public TrackHeader TrackHeader { get; }
        public Grid TrackContent { get; }
        public Timeline ParentTimeline { get; }

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

        enum ChunkBorder
        {
            Start,
            Middle,
            End
        }

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

        void CreateChunk(Subtitle subtitle)
        {
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
            UpdateSubtitleChunk(subtitle);
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

            if (e.ClickCount == 1) {
                e.Handled = true;
                startPos = e.GetPosition(ParentTimeline.headerStack);
                border.CaptureMouse();

                ParentTimeline.SelectedItems.Clear();
                ParentTimeline.SelectedItems.Add(chunk.Subtitle);
            }
            if (e.ClickCount >= 2) {
                border.ReleaseMouseCapture();
                afterDblClick = true; //e.Handled nestačí
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
            if (isEditingChunk) e.Handled = true;  //stop routing the event to timeline mouseup, which puts needle under cursor

            if (afterDblClick) {
                afterDblClick = false;
                
            }
            else {
                var border = (Border)sender;
                border.ReleaseMouseCapture();


                isEditingChunk = false;
            }
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

        void TryResizeChunk(Chunk chunk, ChunkBorder chunkBorder)
        {
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
                if (chunkBorder == ChunkBorder.Middle) {
                    foreach (Subtitle subtitle in ParentTimeline.SelectedItems) {

                        //cant modify chunks directly, coz they might not be streamed in
                        TimeSpan timeDelta = TimeSpan.FromSeconds(deltax / ParentTimeline.Pixelscale * ParentTimeline.Timescale);
                        subtitle.Start += timeDelta;
                        subtitle.End += timeDelta;
                    }
                }
            }


            prevPos = position;
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
                    streamedItems.Clear();
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

        void ResizeChunk(Chunk chunk, double addWidth, double addLeftMargin)
        {
            chunk.Width += addWidth;
            chunk.Margin = new Thickness(chunk.Margin.Left + addLeftMargin, 0, 0, 0);
            ChunkUpdated?.Invoke(this, chunk);
        }

        void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ChunkContextMenuOpening?.Invoke(sender, e);
        }
    }
}
