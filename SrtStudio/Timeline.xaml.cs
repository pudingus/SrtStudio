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
        public ObservableCollection<Chunk> SelectedChunks { get; } = new ObservableCollection<Chunk>();
        bool beforetime = true;
        double startdeltax;
        bool afterPoint = false;
        Chunk draggedChunk;
        DraggingPoint draggingPoint;

        public ObservableCollection<Item> SuperSource { get; set; }

        public enum DraggingPoint {
            Start,
            Middle,
            End
        }


        public Timeline() {
            InitializeComponent();

            //SelectedChunks.CollectionChanged += SelectedChunks_CollectionChanged;


            timer.Interval = new TimeSpan(0, 0, 0, 0, 150);
            timer.Tick += Timer_Tick;
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
        }


        public void RemoveTrack(Track track) {
            stack.Children.Remove(track.TrackLine);
            stackMeta.Children.Remove(track.TrackMeta);
        }

        public void ClearTracks() {
            stack.Children.Clear();
            stackMeta.Children.Clear();
        }


        public void AddChunk(Chunk chunk, Track track) {
            chunk.MouseMove += Chunk_MouseMove;
            chunk.MouseLeftButtonDown += Chunk_MouseLeftButtonDown;
            chunk.MouseLeftButtonUp += Chunk_MouseLeftButtonUp;
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
            if (draggedMeta != null) {
                draggedMeta.ParentTrack.TrackLine.Height = draggedMeta.ActualHeight;
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


            TrackMeta trackMeta = draggedMeta;

            if (trackMeta != null) {
                trackMeta.Height -= deltay;
            }

            if (draggedChunk != null) {
                startdeltax = point.X - startPoint.X;
                if (startdeltax >= 5.0 || startdeltax <= -5.0) {
                    Debug.WriteLine("after point " + DateTime.Now);
                    if (afterPoint == false) {
                        afterPoint = true;
                        //deltax -= startdeltax;

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

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer sv = (ScrollViewer)sender;
            scrollbar.Maximum = sv.ScrollableWidth;
            scrollbar.ViewportSize = e.ViewportWidth;
            scrollbar.Value = e.HorizontalOffset;
            //scrollbar.SmallChange = 1000;
            //scrollbar.LargeChange = 1000;

            Console.WriteLine("scroll changed");
            Console.WriteLine(e.ViewportWidth);
            Console.WriteLine(e.HorizontalOffset);
            Console.WriteLine(e.ExtentWidth);
            Console.WriteLine(sv.ScrollableWidth);
        }

        private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            //svHor.ScrollToHorizontalOffset(e.NewValue);
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


        //private void SelectedChunks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
        //    if (e.OldItems != null) {
        //        foreach (Chunk chunk in e.OldItems) {
        //            chunk.Selected = false;
        //        }
        //    }

        //    if (e.NewItems != null) {
        //        foreach (Chunk chunk in e.NewItems) {
        //            chunk.Selected = true;
        //        }
        //    }
        //}

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
                        SelectedChunks.Add(chunk);
                    }
                }
            }
        }

    }
}
