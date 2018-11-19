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
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Windows.Markup;
using System.Globalization;

using static SrtStudio.StringOperations;

namespace SrtStudio {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///

    public partial class MainWindow : Window {
        private MpvPlayer player;

        public ObservableCollection<Item> SuperList { get; set; } = new ObservableCollection<Item>();

        Track editTrack;
        Track refTrack;


        const string programName = "SrtStudio";
        const int timescale = 10;   //one page is 'scale' (30) seconds
        const int pixelscale = 1000;

        const string srtFilter = "Srt - SubRip(*.srt)|*.srt";
        const string projExt = "sprj";
        const string projFilter = "SrtStudio Project (*.sprj)|*.sprj";
        const string videoFilter = "Common video files (*.mkv;*.mp4;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts)|" +
            "*.mkv;*.mp4;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts|" +
            "All files (*.*)|*.*";

        ContextMenu contextMenu;

        private bool unsavedChanges = false;
        private string _currentFile = "";

        public MainWindow() {
            DataContext = this;
            InitializeComponent();

            //use OS culture for bindings
            LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)
                )
            );

            player = new MpvPlayer(PlayerHost.Handle) {
                Loop = true,
                Volume = 100
            };
            player.API.SetPropertyString("sid", "no");  //disable subtitles
            player.PositionChanged += Player_PositionChanged;

            timeline.OnNeedleMoved += Timeline_OnNeedleMoved;

            UpdateTitle("Untitled");

            Settings.Read();
            if (Settings.Data.Maximized) {
                WindowState = WindowState.Maximized;
            }

            videoGrid.Children.Remove(wfHost);
            videoGrid.Children.Remove(topGrid);
            airControl.Back = wfHost;
            airControl.Front = topGrid;
            contextMenu = (ContextMenu)FindResource("ItemContextMenu");
        }

        #region Methods
        private void Seek(TimeSpan position, object except = null) {
            slider.ValueChanged -= Slider_ValueChanged;
            timeline.OnNeedleMoved -= Timeline_OnNeedleMoved;

            if (except != timeline) {
                double margin = position.TotalSeconds / timescale * pixelscale;
                timeline.needle.Margin = new Thickness(margin, 0, 0, 0);
            }

            if (except != slider) {
                if (player.Duration.TotalSeconds != 0) {
                    slider.Value = position.TotalSeconds / player.Duration.TotalSeconds * 100;
                }
            }

            if (except != player) {
                if (player.IsMediaLoaded) {
                    //player.SeekAsync(position);
                    player.Position = position;
                }
            }

            slider.ValueChanged += Slider_ValueChanged;
            timeline.OnNeedleMoved += Timeline_OnNeedleMoved;
        }

        private void LoadSubtitles(List<Subtitle> subtitles, string trackName) {
            SuperList.Clear();
            if (editTrack != null)
                timeline.RemoveTrack(editTrack);
            int i = 0;

            if (subtitles.Count <= 0) return;

            Track track = new Track {
                Name = trackName
            };
            editTrack = track;
            editTrack.SelectedChunks.CollectionChanged += SelectedChunks_CollectionChanged;

            timeline.AddTrack(track, true);
            foreach (Subtitle sub in subtitles) {
                i++;

                Item item = new Item(sub) {
                    Index = i
                };
                SuperList.Add(item);

                double margin = item.Start.TotalSeconds / timescale * pixelscale;
                double width = item.Dur.TotalSeconds / timescale * pixelscale;
                Chunk chunk = new Chunk {
                    Margin = new Thickness(margin, 0, 0, 0),
                    Width = width
                };
                chunk.DataContext = item;
                chunk.ContextMenu = contextMenu;
                chunk.ContextMenuOpening += new ContextMenuEventHandler(Chunk_ContextMenuOpening);

                item.Chunk = chunk;

                track.AddChunk(chunk);
            }

            editTrack.OnChunkUpdated += EditTrack_OnChunkUpdated;
        }

        private void LoadRefSubtitles(List<Subtitle> subtitles, string trackName) {
            if (refTrack != null)
                timeline.RemoveTrack(refTrack);
            int i = 0;

            if (subtitles.Count <= 0) return;

            Track track = new Track {
                Name = trackName,
                Height = 50,
                Locked = true
            };
            refTrack = track;

            timeline.AddTrack(track);

            foreach (Subtitle sub in subtitles) {
                i++;
                TimeSpan duration = sub.End - sub.Start;

                double margin = sub.Start.TotalSeconds / timescale * pixelscale;
                double width = duration.TotalSeconds / timescale * pixelscale;
                Item item = new Item(sub);
                Chunk chunk = new Chunk {
                    Margin = new Thickness(margin, 0, 0, 0),
                    Width = width,
                    DataContext = item
                };

                track.AddChunk(chunk);
            }
        }

        private void UpdateTitle(string currentFile) {
            _currentFile = currentFile;
            string str = "";
            if (unsavedChanges) str = "*";
            int index = 0;
            int count = 0;
            double perc = 0.0;

            if (SuperList.Count > 0 && listView.SelectedIndex != -1) {
                index = listView.SelectedIndex + 1;
                count = SuperList.Count;
                perc = index / count * 100.0;
            }

            Title = currentFile + str + " - SrtStudio - " + index + "/" + count + " - " + perc.ToString("N1") + " %";
        }

        private void CloseProject() {
            SuperList.Clear();
            player.Stop();
            player.PlaylistClear();
            timeline.ClearTracks();
            Project.Data.Subtitles = null;
            Project.Data.RefSubtitles = null;
            UpdateTitle("Untitled");
        }

        private void RecalculateIndexes() {
            foreach (Item item in SuperList) {
                item.Index = SuperList.IndexOf(item) + 1;
            }
        }

        private void Merge(IList items, bool asDialog = false) {
            var sl = new List<Item>();
            foreach (Item item in SuperList)
                if (items.Contains(item)) sl.Add(item);

            for (int i = 0; i < sl.Count-1; i++) {
                Item item = sl[i];
                Item nextItem = sl[i+1];
                //next item is 'neighbor' to current item
                if (nextItem.Index == item.Index + 1) {
                    item.End = nextItem.End;
                    if (!asDialog)
                        item.Text += " " + nextItem.Text;
                    else
                        item.Text = "-" + item.Text + Environment.NewLine + "-" + nextItem.Text;

                    SuperList.Remove(nextItem);

                    double margin = item.Start.TotalSeconds / timescale * pixelscale;
                    double width = item.Dur.TotalSeconds / timescale * pixelscale;
                    item.Chunk.Margin = new Thickness(margin, 0, 0, 0);
                    item.Chunk.Width = width;

                    editTrack.RemoveChunk(nextItem.Chunk);

                    i++;
                }
            }
            RecalculateIndexes();
        }
        #endregion


        private void Timeline_OnNeedleMoved() {
            double start = timeline.needle.Margin.Left / pixelscale * timescale;
            Seek(TimeSpan.FromSeconds(start), timeline);
        }

        private void EditTrack_OnChunkUpdated(Chunk chunk) {
            Item item = (Item)chunk.DataContext;
            Subtitle sub = item.Sub;

            double start = chunk.Margin.Left / pixelscale * timescale;
            item.Start = TimeSpan.FromSeconds(start);

            double dur = chunk.Width / pixelscale * timescale;

            double end = start + dur;
            item.End = TimeSpan.FromSeconds(end);

            unsavedChanges = true;
            UpdateTitle(_currentFile);
        }

        private void SelectedChunks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            listView.SelectionChanged -= listView_SelectionChanged;
            foreach (Item item in listView.SelectedItems) {
                item.Enabled = false;
            }
            listView.SelectedItems.Clear();
            foreach (Chunk chunk in editTrack.SelectedChunks) {
                if (chunk != null) {
                    Item item = (Item)chunk.DataContext;
                    listView.SelectedItems.Add(item);
                    item.Enabled = true;
                    listView.ScrollIntoView(item);
                }
            }
            listView.SelectionChanged += listView_SelectionChanged;
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            foreach (Item item in e.RemovedItems) {
                item.Enabled = false;
                editTrack.SelectedChunks.Remove(item.Chunk);
            }

            foreach (Item item in e.AddedItems) {
                item.Enabled = true;
                editTrack.SelectedChunks.Add(item.Chunk);

                item.Chunk.BringIntoView();
            }
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {

            if (((FrameworkElement)e.OriginalSource).DataContext is Item item && player.IsMediaLoaded) {
                //player.Position = item.Sub.Start;
                Seek(item.Sub.Start);
            }
        }

        #region TextBox Events
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            Debug.WriteLine("textbox preview keydown");
            TextBox textBox = (TextBox)sender;
            if (e.Key == Key.F4) {
                int caretIndex = textBox.CaretIndex;
                string str = RemoveNewlines(textBox.Text);
                str = str.Insert(caretIndex, Environment.NewLine);
                str = TrimSpaces(str);
                textBox.Text = str;
                textBox.CaretIndex = caretIndex + 1;
                e.Handled = true;
            }
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e) {
            Debug.WriteLine(((TextBox)sender).CaretIndex);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            Item item = (Item)textBox.DataContext;
            item.Sub.Text = textBox.Text;
            item.Text = textBox.Text;
            unsavedChanges = true;
            UpdateTitle(_currentFile);
        }

        private void TextBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            TextBox textBox = (TextBox)sender;

            Task.Delay(20).ContinueWith(t => {
                Dispatcher.Invoke(() => {
                    textBox.Focus();
                    textBox.CaretIndex = textBox.Text.Length;
                });
            });
        }
        #endregion

        #region Player and player controls Events
        private void Button_Click(object sender, RoutedEventArgs e) {
            if (player.IsPlaying)
                player.Pause();
            else player.Resume();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (player.IsMediaLoaded) {
                double seconds = slider.Value / 100 * player.Duration.TotalSeconds;
                Seek(TimeSpan.FromSeconds(seconds), sender);
            }
        }

        private void Player_PositionChanged(object sender, MpvPlayerPositionChangedEventArgs e) {
            Dispatcher.Invoke(() => {
                if (player.IsPlaying) {
                    var pos = e.NewPosition;
                    Seek(e.NewPosition, player);

                    foreach (Item item in SuperList) {
                        TimeSpan end = item.Start + item.Dur;
                        if (pos >= item.Start && pos <= end) {
                            listView.SelectedItem = item;
                            break;
                        }
                    }
                }
            });
        }
        #endregion

        #region Menu Events
        private void menuVideoOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = videoFilter
            };
            if (dialog.ShowDialog() == true) {
                player.Load(dialog.FileName);
                Project.Data.VideoPath = dialog.FileName;
            }
        }

        private void menuSrtImport_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = srtFilter
            };
            if (dialog.ShowDialog() == true) {

                Project.Data.Subtitles = Srt.Read(dialog.FileName);
                Project.Data.TrackName = dialog.SafeFileName;
                LoadSubtitles(Project.Data.Subtitles, Project.Data.TrackName);
            }
        }

        private void menuSrtRefImport_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = srtFilter
            };
            if (dialog.ShowDialog() == true) {
                Project.Data.RefSubtitles = Srt.Read(dialog.FileName);
                Project.Data.RefTrackName = dialog.SafeFileName;
                LoadRefSubtitles(Project.Data.RefSubtitles, Project.Data.RefTrackName);
            }
        }

        private void menuSrtExport_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog {
                AddExtension = true,
                DefaultExt = "srt",
                Filter = srtFilter
            };
            if (dialog.ShowDialog() == true) {
                Srt.Write(dialog.FileName, Project.Data.Subtitles);
            }
        }

        private void menuProjectNew_Click(object sender, RoutedEventArgs e) {
            CloseProject();
        }

        private void menuProjectOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = projFilter
            };
            if (dialog.ShowDialog() == true) {
                CloseProject();
                Project.Read(dialog.FileName);
                UpdateTitle(dialog.SafeFileName);
                if (!string.IsNullOrEmpty(Project.Data.VideoPath))
                    player.Load(Project.Data.VideoPath);

                LoadSubtitles(Project.Data.Subtitles, Project.Data.TrackName);
                LoadRefSubtitles(Project.Data.RefSubtitles, Project.Data.RefTrackName);


                Task.Delay(200).ContinueWith(t => {
                    Dispatcher.Invoke(() => {
                        timeline.svHor.ScrollToHorizontalOffset(Project.Data.ScrollPos);
                        Seek(TimeSpan.FromSeconds(Project.Data.VideoPos), null);

                    });
                });
            }
        }

        private void menuProjectSave_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(Project.FileName)) {
                SaveFileDialog dialog = new SaveFileDialog {
                    AddExtension = true,
                    DefaultExt = projExt,
                    Filter = projFilter
                };
                if (dialog.ShowDialog() == true) {
                    Project.Write(dialog.FileName);
                    unsavedChanges = false;
                    UpdateTitle(dialog.SafeFileName);
                }
            }
            else {
                Project.Data.SelIndex = this.listView.SelectedIndex;
                Project.Data.VideoPos = player.Position.TotalSeconds;
                Project.Data.ScrollPos = timeline.scrollbar.Value;
                Project.Write(Project.FileName);
                unsavedChanges = false;
                UpdateTitle(this._currentFile);
            }
        }

        private void menuProjectSaveAs_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog {
                AddExtension = true,
                DefaultExt = projExt,
                Filter = projFilter
            };
            if (dialog.ShowDialog() == true) {
                Project.Write(dialog.FileName);
                UpdateTitle(dialog.SafeFileName);
                unsavedChanges = false;
                UpdateTitle(dialog.SafeFileName);
            }
        }

        private void menuProjectClose_Click(object sender, RoutedEventArgs e) {
            CloseProject();
        }
        #endregion

        #region Window Events
        private void Window_Closing(object sender, CancelEventArgs e) {
            if (unsavedChanges && MessageBox.Show("There are unsaved changes. \nDo you really want to quit?", "Unsaved changes", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Cancel) {
                e.Cancel = true;
            }
            else {
                if (WindowState == WindowState.Maximized) {
                    Settings.Data.Maximized = true;
                }
                Settings.Write();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.F5) {
                if (player.IsPlaying)
                    player.Pause();
                else player.Resume();
            }
            if (e.Key == Key.F2) {

                Item beforeNeedle = null;
                for (int index = SuperList.Count - 1; index >= 0; --index) {
                    if (player.Position >= SuperList[index].Start) {
                        beforeNeedle = SuperList[index];
                        break;
                    }
                }
                if (beforeNeedle != null) {
                    Subtitle sub = new Subtitle() {
                        Start = player.Position,
                        End = player.Position + TimeSpan.FromSeconds(1.5),
                        Text = ""
                    };
                    Project.Data.Subtitles.Insert(beforeNeedle.Index, sub);
                    Item obj2 = new Item(sub);
                    SuperList.Insert(beforeNeedle.Index, obj2);
                    RecalculateIndexes();
                    double left = obj2.Start.TotalSeconds / 10.0 * 1000.0;
                    double num = obj2.Dur.TotalSeconds / 10.0 * 1000.0;
                    Chunk chunk1 = new Chunk();
                    chunk1.Margin = new Thickness(left, 0.0, 0.0, 0.0);
                    chunk1.Width = num;
                    Chunk chunk2 = chunk1;
                    chunk2.DataContext = (object)obj2;
                    obj2.Chunk = chunk2;
                    editTrack.AddChunk(chunk2);
                }
            }
        }
        #endregion

        #region Context Menu Events
        private void MenuItemMerge_Click(object sender, RoutedEventArgs e) {
            if (listView.SelectedItems.Count <= 1) return;

            Merge(listView.SelectedItems);
        }

        private void MenuItemMergeDialog_Click(object sender, RoutedEventArgs e) {
            if (listView.SelectedItems.Count <= 1) return;

            Merge(listView.SelectedItems, true);
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e) {
            var copy = new List<Item>();
            foreach (Item item in listView.SelectedItems) copy.Add(item);

            foreach (Item item in copy) {
                SuperList.Remove(item);
                editTrack.RemoveChunk(item.Chunk);
            }
            RecalculateIndexes();
        }

        private void ListViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            ListViewItem listViewItem = (ListViewItem)sender;
            Item dataContext = (Item)listViewItem.DataContext;
            ContextMenu contextMenu = listViewItem.ContextMenu;
            MenuItem menuItem1 = (MenuItem)contextMenu.Items[0];
            MenuItem menuItem2 = (MenuItem)contextMenu.Items[1];
            MenuItem menuItem3 = (MenuItem)contextMenu.Items[2];
            menuItem1.IsEnabled = false;
            menuItem2.IsEnabled = false;
            menuItem3.IsEnabled = false;
            if (!listView.SelectedItems.Contains(dataContext))
                return;
            int count = listView.SelectedItems.Count;
            if (count >= 1)
                menuItem3.IsEnabled = true;
            if (count >= 2) {
                menuItem1.IsEnabled = true;
                menuItem2.IsEnabled = true;
            }
        }

        private void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            Chunk chunk = (Chunk)sender;
            Item dataContext = (Item)chunk.DataContext;
            ContextMenu contextMenu = chunk.ContextMenu;
            MenuItem menuItem1 = (MenuItem)contextMenu.Items[0];
            MenuItem menuItem2 = (MenuItem)contextMenu.Items[1];
            MenuItem menuItem3 = (MenuItem)contextMenu.Items[2];
            menuItem1.IsEnabled = false;
            menuItem2.IsEnabled = false;
            menuItem3.IsEnabled = false;
            if (!listView.SelectedItems.Contains(dataContext))
                return;
            int count = listView.SelectedItems.Count;
            if (count >= 1)
                menuItem3.IsEnabled = true;
            if (count >= 2) {
                menuItem1.IsEnabled = true;
                menuItem2.IsEnabled = true;
            }
        }
        #endregion
    }
}
