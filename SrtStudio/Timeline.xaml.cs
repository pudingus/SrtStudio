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
using System.Windows.Threading;

namespace SrtStudio
{
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl {


        TrackMeta draggedMeta;
        int dragSize = 8;
        Point point;

        public Timeline() {
            InitializeComponent();


        }


        public void AddTrack(Track track) {
            track.TrackMeta.MouseMove += TrackMeta_MouseMove;
            track.TrackMeta.MouseLeftButtonDown += TrackMeta_MouseLeftButtonDown;
            track.TrackMeta.MouseLeftButtonUp += TrackMeta_MouseLeftButtonUp;
            track.TrackMeta.MouseLeave += TrackMeta_MouseLeave;

            //stack.Children.Add(track.TrackLine);
            //stackMeta.Children.Add(track.TrackMeta);


            stack.Children.Insert(0, track.TrackLine);
            stackMeta.Children.Insert(0, track.TrackMeta);
        }

        public void ClearTracks() {
            stack.Children.Clear();
            stackMeta.Children.Clear();
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
        }

        private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            svHor.ScrollToHorizontalOffset(e.NewValue);
        }
    }
}
