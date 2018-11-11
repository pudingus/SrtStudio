using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

using Mpv.NET.Player;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SrtStudio {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///

    public partial class MainWindow : Window {
        private MpvPlayer player;

        DispatcherTimer timer = new DispatcherTimer();


        public ObservableCollection<Item> SuperList { get; set; } = new ObservableCollection<Item>();

        public MainWindow() {
            DataContext = this;

            InitializeComponent();

            player = new MpvPlayer(PlayerHost.Handle) {
                Loop = true,
                Volume = 50
            };
            //player.Load("http://techslides.com/demos/sample-videos/small.mp4");
            //player.Resume();

            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += Timer_Tick;


            timeline.SelectedChunks.CollectionChanged += SelectedChunks_CollectionChanged;
        }

        bool dont = false;
        private void SelectedChunks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) {
                foreach (Chunk chunk in e.OldItems) {

                    dont = true;
                    listView.SelectedItems.Remove(chunk.sub.item);
                }
            }

            if (e.NewItems != null) {
                foreach (Chunk chunk in e.NewItems) {
                    dont = true;
                    listView.SelectedItems.Add(chunk.sub.item);
                }
            }


        }

        private void Timer_Tick(object sender, EventArgs e) {
            timer.Stop();
            //listView.Items.Refresh();

            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => listView.Items.Refresh())
                );
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (player.IsPlaying)
                player.Pause();
            else player.Resume();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            player.Position = new TimeSpan(0, 0, 0, Convert.ToInt32(slider.Value / 100 * player.Duration.TotalSeconds));
        }

        private void menuVideoOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true) {
                player.Load(dialog.FileName);
                Project.data.VideoPath = dialog.FileName;
            }
        }

        private void menuProjectSaveAs_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog();
            if (dialog.ShowDialog() == true) {
                Project.Write(dialog.FileName);
            }
        }

        const int scale = 10;   //one page is 'scale' (30) seconds

        const int widthscale = 1000;

        private void menuSrtImport_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true) {
                Srt srt = new Srt();
                srt.Read(dialog.FileName);
                int i = 0;


                Grid track = new Grid();
                track.Height = 100;

                track.HorizontalAlignment = HorizontalAlignment.Left;
                track.VerticalAlignment = VerticalAlignment.Top;

                timeline.stack.Children.Add(track);


                TrackMeta trackMeta = new TrackMeta();
                trackMeta.Text = dialog.SafeFileName;
                trackMeta.Height = 100;
                trackMeta.Width = 100;

                trackMeta.Track = track;

                timeline.RegisterTrackMeta(trackMeta);

                timeline.stackMeta.Children.Add(trackMeta);


                foreach (Subtitle sub in srt.list) {
                    i++;
                    TimeSpan duration = sub.end - sub.start;

                    string sdur = duration.ToString("s\\,ff");
                    //string sstart = sub.start.ToString("hh\\:mm\\:ss\\,fff");

                    //string sstart = sub.start.ToShortForm();
                    string sstart = sub.start.ToString("h\\:mm\\:ss\\,ff");


                    //listBox.Items.Add("# " + i + " "+ sstart + " " + sdur + " | \n" + sub.text);

                    Item item = new Item { Number = i.ToString(), Start = sstart, Dur = sdur, Text = sub.text };
                    SuperList.Add(item);
                    //listView.Items.Add(item);
                    sub.item = item;





                    Chunk chunk = new Chunk();
                    chunk.sub = sub;
                    timeline.RegisterHandlers(chunk);
                    chunk.Text = sub.text;
                    chunk.HorizontalAlignment = HorizontalAlignment.Left;
                    chunk.VerticalAlignment = VerticalAlignment.Stretch;

                    item.Chunk = chunk;


                    double margin = sub.start.TotalSeconds / scale * widthscale;
                    double width = duration.TotalSeconds / scale * widthscale;

                    chunk.Dur = sdur;

                    chunk.Margin = new Thickness(margin, 0, 0, 0);
                    chunk.Width = width;

                    track.Children.Add(chunk);
                }

                timeline.OnChunkUpdated += (chunk) => {
                    Subtitle sub = chunk.sub;
                    Console.WriteLine("chunk updated");

                    double start = chunk.Margin.Left / widthscale * scale;

                    sub.start = new TimeSpan(0, 0, 0, 0, (int)(start * widthscale) + 1);
                    //sub.item.Start = sub.start.ToString();
                    //sub.item.Start = sub.start.ToShortForm();
                    sub.item.Start = sub.start.ToString("h\\:mm\\:ss\\,ff");

                    double dur = chunk.Width / widthscale * scale;
                    TimeSpan duration = new TimeSpan(0, 0, 0, 0, (int)(dur * widthscale) + 1);
                    string sdur = duration.ToString("s\\,ff");
                    sub.item.Dur = sdur;
                    chunk.Dur = sdur;


                    double end = start + dur;
                    sub.end = new TimeSpan(0, 0, 0, 0, (int)(end * widthscale) + 1);
                    //textboxEnd.Text = sub.end.ToString();

                    //timer.Start();

                    //listView.Items.Refresh();

                };


                /*
                Subtitle first = srt.list[0];

                TimeSpan duration =  first.end - first.start;

                double margin = first.start.TotalSeconds / scale * 1000;

                //first.start.TotalSeconds / 30 * 1000 = margin        | / 1000
                //first.start.TotalSeconds / 30 = margin / 1000        | * 30
                //first.start.TotalSeconds = margin / 1000 * 30


                double width = duration.TotalSeconds / scale * 1000;

                timeline.mLeft = margin;
                timeline.width = width;


                textboxStart.Text = first.start.ToString();
                textboxEnd.Text = first.end.ToString();
                textboxDur.Text = duration.ToString();
                */

            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            listView.Items.Refresh();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Console.WriteLine("OriginalSource = " + e.OriginalSource);

            if (dont == false) {
                foreach (Item item in e.RemovedItems) {
                    item.Enabled = false;
                    timeline.SelectedChunks.Remove(item.Chunk);
                }

                foreach (Item item in e.AddedItems) {
                    item.Enabled = true;
                    timeline.SelectedChunks.Add(item.Chunk);
                }
            }
            else dont = false;

            //foreach (Item item in e.RemovedItems) {
            //    item.Enabled = false;
            //    timeline.SelectedChunks.Remove(item.Chunk);
            //}

            //foreach (Item item in e.AddedItems)
            //{
            //    item.Enabled = true;
            //    timeline.SelectedChunks.Add(item.Chunk);
            //}

        }
    }
}
