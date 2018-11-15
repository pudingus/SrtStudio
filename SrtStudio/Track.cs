using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SrtStudio
{
    public class Track
    {

        public delegate void ChunkUpdated(Chunk chunk);
        public event ChunkUpdated OnChunkUpdated;

        public bool Locked { get; set; }


        public string Name {
            get {
                return TrackMeta.trackName.Text;
            }
            set {
                TrackMeta.trackName.Text = value;
            }
        }

        private double _height = 100;
        public double Height {
            get {
                return _height;
            }
            set {
                _height = value;
                TrackMeta.Height = _height;
                TrackLine.Height = _height;
            }

        }


        public TrackMeta TrackMeta { get; set; }
        public Grid TrackLine { get; set; }

        public ObservableCollection<Chunk> SelectedChunks { get; } = new ObservableCollection<Chunk>();



        DispatcherTimer timer = new DispatcherTimer();

        public Track()
        {
            SelectedChunks.CollectionChanged += SelectedChunks_CollectionChanged;


            timer.Interval = new TimeSpan(0, 0, 0, 0, 150);
            timer.Tick += Timer_Tick;


            TrackMeta = new TrackMeta(this) {
                Height = _height
            };
            TrackLine = new Grid {
                Height = _height,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            //TrackLine.itemsControl.ItemsSource = TrackLine.ChunksSuper;

            TrackLine.PreviewMouseMove += TrackLine_PreviewMouseMove;
        }


        public void UnselectAll() {
            var copy = new ObservableCollection<Chunk>();
            foreach (Chunk chunk in SelectedChunks) {
                copy.Add(chunk);
            }

            foreach (Chunk chunk in copy) {
                SelectedChunks.Remove(chunk);
            }
        }

        public Color Darken(Color color, float perc) {
            var fcolor = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            fcolor = System.Windows.Forms.ControlPaint.Dark(fcolor, perc);
            return Color.FromRgb(fcolor.R, fcolor.G, fcolor.B);
        }

        public void AddChunk(Chunk chunk) {
            chunk.MouseMove += Chunk_MouseMove;
            chunk.MouseLeftButtonDown += Chunk_MouseLeftButtonDown;
            chunk.MouseLeftButtonUp += Chunk_MouseLeftButtonUp;
            chunk.MouseEnter += Chunk_MouseEnter;
            chunk.MouseLeave += Chunk_MouseLeave;

            if (Locked) {
                var bc = new BrushConverter();
                chunk.backRect.Fill = (Brush)bc.ConvertFrom("#FF3C3C3C");
            }
            //Brush brush = chunk.backRect.Fill;
            //Color color = ((SolidColorBrush)brush).Color;

            //color = Darken(color, 2.0f);


            //chunk.backRect.Stroke = new SolidColorBrush(color);

            TrackLine.Children.Add(chunk);

            //TrackLine.itemsControl.Items.Add(chunk);
            //TrackLine.ChunksSuper.Add(chunk);
        }

        Chunk draggedChunk;
        DraggingPoint draggingPoint;

        enum DraggingPoint {
            Start,
            Middle,
            End
        }

        Point point;

        const int dragSize = 8;

        bool beforetime = true;
        private void Timer_Tick(object sender, EventArgs e) {
            beforetime = false;
            timer.Stop();
        }


        private void SelectedChunks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
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


        private void TrackLine_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (!Locked) {
                double deltax = point.X;
                double deltay = point.Y;
                point = e.GetPosition(TrackLine);
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
            }
        }


        private void Chunk_MouseMove(object sender, MouseEventArgs e) {
            if (!Locked) {
                Chunk chunk = (Chunk)sender;
                Point point = e.GetPosition(chunk);

                Cursor cursor = Cursors.Arrow;
                if (point.Y >= 0 && point.Y <= chunk.ActualHeight) {
                    if ((point.X >= 0 && point.X <= dragSize) ||
                        (point.X >= chunk.Width - dragSize && point.X <= chunk.ActualWidth)) {
                        cursor = Cursors.SizeWE;
                    }
                }
                TrackLine.Cursor = cursor;
            }
        }


        private void Chunk_MouseEnter(object sender, MouseEventArgs e) {
            if (!Locked) {
                Chunk chunk = (Chunk)sender;
                chunk.hilitBorder.Visibility = Visibility.Visible;
            }
        }


        private void Chunk_MouseLeave(object sender, MouseEventArgs e) {
            if (!Locked) {
                TrackLine.Cursor = Cursors.Arrow;

                Chunk chunk = (Chunk)sender;
                chunk.hilitBorder.Visibility = Visibility.Hidden;
            }
        }

        private void Chunk_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (!Locked) {
                Chunk chunk = (Chunk)sender;
                Point pointc = e.GetPosition(chunk);

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
                    }
                    else {
                        draggingPoint = DraggingPoint.Middle;
                        draggedChunk = chunk;
                        Mouse.Capture(chunk);
                    }
                }

                if (Keyboard.Modifiers == ModifierKeys.Control) {
                    if (!chunk.Selected) {
                        SelectedChunks.Add(chunk);
                    }
                    else {
                        SelectedChunks.Remove(chunk);
                    }
                }
                else {
                    if (!chunk.Selected) {

                        UnselectAll();
                        //SelectedChunks.Clear();
                        SelectedChunks.Add(chunk);
                    }
                }

                beforetime = true;
                timer.Start();
            }
        }

        private void Chunk_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (!Locked) {
                draggedChunk = null;
                Mouse.Capture(null);

                Chunk chunk = (Chunk)sender;

                if (beforetime) {
                    if (Keyboard.Modifiers != ModifierKeys.Control) {
                        UnselectAll();
                        //SelectedChunks.Clear();
                        SelectedChunks.Add(chunk);
                    }
                }
            }
        }
    }
}
