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



        public ObservableCollection<Item> SuperList { get; set; } = new ObservableCollection<Item>();

        Track editTrack;

        public MainWindow() {
            DataContext = this;

            InitializeComponent();

            player = new MpvPlayer(PlayerHost.Handle) {
                Loop = true,
                Volume = 50
            };

            player.PositionChanged += Player_PositionChanged;
            //player.Load("http://techslides.com/demos/sample-videos/small.mp4");
            //player.Resume();

            UpdateTitle("Untitled");

            Settings.Read();
            if (Settings.Data.Maximized) {
                WindowState = WindowState.Maximized;
            }
        }

        private void Player_PositionChanged(object sender, MpvPlayerPositionChangedEventArgs e) {
            Dispatcher.Invoke(() => {
                slider.ValueChanged -= Slider_ValueChanged;
                if (player.Duration.TotalSeconds != 0) {
                    slider.Value = e.NewPosition.TotalSeconds / player.Duration.TotalSeconds * 100;
                }
                slider.ValueChanged += Slider_ValueChanged;

            });
        }

        private void SelectedChunks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            listView.SelectionChanged -= listView_SelectionChanged;

            foreach (Item item in listView.SelectedItems) {
                item.Enabled = false;
                //item.BorderThickness = new Thickness(0);
            }

            listView.SelectedItems.Clear();
            foreach (Chunk chunk in editTrack.SelectedChunks) {
                if (chunk != null) {
                    listView.SelectedItems.Add(chunk.Item);
                    chunk.Item.Enabled = true;
                    //chunk.Item.BorderThickness = new Thickness(1);

                    listView.ScrollIntoView(chunk.Item);
                }
            }



            listView.SelectionChanged += listView_SelectionChanged;
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            foreach (Item item in e.RemovedItems) {
                item.Enabled = false;
                //item.BorderThickness = new Thickness(0);

                editTrack.SelectedChunks.Remove(item.Chunk);
            }

            foreach (Item item in e.AddedItems) {
                item.Enabled = true;
                //item.BorderThickness = new Thickness(1);
                editTrack.SelectedChunks.Add(item.Chunk);

                item.Chunk.BringIntoView();
            }

            if (player.IsMediaLoaded) {
                Item item = (Item)listView.SelectedItem;
                if (item != null) {
                    player.Position = item.Chunk.sub.Start;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (player.IsPlaying)
                player.Pause();
            else player.Resume();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (player.IsMediaLoaded) {
                player.Position = new TimeSpan(0, 0, 0, Convert.ToInt32(slider.Value / 100 * player.Duration.TotalSeconds));
            }
        }




        const string startFormat = "h\\:mm\\:ss\\,ff";
        const string durFormat = "s\\,ff";

        private void LoadSubtitles(List<Subtitle> subtitles, string name) {
            int i = 0;

            if (subtitles.Count <= 0) return;

            Track track = new Track {
                Name = name
            };
            editTrack = track;
            editTrack.SelectedChunks.CollectionChanged += SelectedChunks_CollectionChanged;

            timeline.AddTrack(track);
            foreach (Subtitle sub in subtitles) {
                i++;
                TimeSpan duration = sub.End - sub.Start;

                string sdur = duration.ToString(durFormat);
                string sstart = sub.Start.ToString(startFormat);

                Item item = new Item {
                    Number = i.ToString(),
                    Start = sstart,
                    Dur = sdur,
                    Text = sub.Text
                };
                SuperList.Add(item);

                double margin = sub.Start.TotalSeconds / scale * widthscale;
                double width = duration.TotalSeconds / scale * widthscale;
                Chunk chunk = new Chunk {
                    sub = sub,
                    Text = sub.Text,
                    Dur = sdur,
                    Margin = new Thickness(margin, 0, 0, 0),
                    Width = width,
                    Item = item
                };

                item.Chunk = chunk;

                track.AddChunk(chunk);
            }

            editTrack.OnChunkUpdated += (chunk) => {
                Subtitle sub = chunk.sub;
                Console.WriteLine("chunk updated");

                double start = chunk.Margin.Left / widthscale * scale;

                sub.Start = new TimeSpan(0, 0, 0, 0, (int)(start * widthscale) + 1);
                chunk.Item.Start = sub.Start.ToString(startFormat);

                double dur = chunk.Width / widthscale * scale;
                TimeSpan duration = new TimeSpan(0, 0, 0, 0, (int)(dur * widthscale) + 1);
                string sdur = duration.ToString(durFormat);
                chunk.Item.Dur = sdur;
                chunk.Dur = sdur;


                double end = start + dur;
                sub.End = new TimeSpan(0, 0, 0, 0, (int)(end * widthscale) + 1);
            };
        }

        private void LoadRefSubtitles(List<Subtitle> subtitles, string name) {
            int i = 0;

            if (subtitles.Count <= 0) return;

            Track track = new Track {
                Name = name,
                Height = 50,
                Locked = true
            };

            timeline.AddTrack(track);

            foreach (Subtitle sub in subtitles) {
                i++;
                TimeSpan duration = sub.End - sub.Start;

                string sdur = duration.ToString(durFormat);
                string sstart = sub.Start.ToString(startFormat);

                double margin = sub.Start.TotalSeconds / scale * widthscale;
                double width = duration.TotalSeconds / scale * widthscale;
                Chunk chunk = new Chunk {
                    sub = sub,
                    Text = sub.Text,
                    Dur = sdur,
                    Margin = new Thickness(margin, 0, 0, 0),
                    Width = width
                };

                track.AddChunk(chunk);
            }
        }



        const string programName = "SrtStudio";

        private void UpdateTitle(string currentFile) {
            Title = currentFile + " - " + programName;
        }


        const int scale = 10;   //one page is 'scale' (30) seconds

        const int widthscale = 1000;

        private void CloseProject() {
            SuperList.Clear();
            player.Stop();
            player.PlaylistClear();
            timeline.ClearTracks();
            Project.Data.Subtitles = null;
            Project.Data.RefSubtitles = null;
            UpdateTitle("Untitled");
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            Item item = (Item)textBox.DataContext;
            item.Chunk.sub.Text = textBox.Text;
            item.Chunk.Text = textBox.Text;
            Console.WriteLine("textbox textchanged");
        }

        const string srtFilter = "Srt - SubRip(*.srt)|*.srt";
        const string projExt = "sprj";
        const string projFilter = "SrtStudio Project (*.sprj)|*.sprj";
        const string videoFilter = "Common video files (*.mkv;*.mp4;*;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts)|" +
            "*.mkv;*.mp4;*;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts|" +
            "All files (*.*)|*.*";



        private void menuVideoOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = videoFilter;
            if (dialog.ShowDialog() == true) {
                player.Load(dialog.FileName);
                Project.Data.VideoPath = dialog.FileName;
            }
        }


        private void menuSrtImport_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = srtFilter;
            if (dialog.ShowDialog() == true) {

                Project.Data.Subtitles = Srt.Read(dialog.FileName);
                Project.Data.TrackName = dialog.SafeFileName;
                LoadSubtitles(Project.Data.Subtitles, Project.Data.TrackName);
            }
        }

        private void menuSrtRefImport_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = srtFilter;
            if (dialog.ShowDialog() == true) {
                Project.Data.RefSubtitles = Srt.Read(dialog.FileName);
                Project.Data.RefTrackName = dialog.SafeFileName;
                LoadRefSubtitles(Project.Data.RefSubtitles, Project.Data.RefTrackName);
            }
        }

        private void menuSrtExport_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = "srt";
            dialog.Filter = srtFilter;
            if (dialog.ShowDialog() == true) {
                Srt.Write(dialog.FileName, Project.Data.Subtitles);
            }
        }

        private void menuProjectNew_Click(object sender, RoutedEventArgs e) {
            CloseProject();
        }

        private void menuProjectOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = projFilter;
            if (dialog.ShowDialog() == true) {
                CloseProject();
                Project.Read(dialog.FileName);
                UpdateTitle(dialog.SafeFileName);
                if (!string.IsNullOrEmpty(Project.Data.VideoPath))
                    player.Load(Project.Data.VideoPath);

                LoadSubtitles(Project.Data.Subtitles, Project.Data.TrackName);
                LoadRefSubtitles(Project.Data.RefSubtitles, Project.Data.RefTrackName);
            }
        }

        private void menuProjectSave_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(Project.FileName)) {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.AddExtension = true;
                dialog.DefaultExt = projExt;
                dialog.Filter = projFilter;
                if (dialog.ShowDialog() == true) {
                    Project.Write(dialog.FileName);
                    UpdateTitle(dialog.SafeFileName);
                }
            }
            else {
                Project.Write(Project.FileName);
            }
        }

        private void menuProjectSaveAs_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = projExt;
            dialog.Filter = projFilter;
            if (dialog.ShowDialog() == true) {
                Project.Write(dialog.FileName);
                UpdateTitle(dialog.SafeFileName);
            }
        }

        private void menuProjectClose_Click(object sender, RoutedEventArgs e) {
            CloseProject();
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            if (WindowState == WindowState.Maximized) {
                Settings.Data.Maximized = true;
            }
            Settings.Write();
        }
    }
}
