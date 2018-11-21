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
using Path = System.IO.Path;

namespace SrtStudio {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///

    public partial class MainWindow : Window {
        private MpvPlayer player;

        public ObservableCollection<Item> SuperList { get; set; } = new ObservableCollection<Item>();
        public ObservableCollection<Item> SuperListRef { get; set; } = new ObservableCollection<Item>();


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

        DispatcherTimer bakTimer = new DispatcherTimer() {
            Interval = TimeSpan.FromMinutes(1.0)
        };

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
            if (!Settings.Data.SafelyExited && Settings.Data.LastProject != null) {
                var result = MessageBox.Show(
                    "Program didn't safely exit last time, \nDo you want to restore backup?",
                    "Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
                if (result == MessageBoxResult.Yes) {
                    try {
                        OpenProject(Settings.Data.LastProject);
                    }
                    catch (Exception) {
                        MessageBox.Show("error");
                    }

                }
            }
            if (Settings.Data.SafelyExited) {
                Settings.Data.SafelyExited = false;
                Settings.Write();
            }

            videoGrid.Children.Remove(wfHost);
            videoGrid.Children.Remove(topGrid);
            airControl.Back = wfHost;
            airControl.Front = topGrid;
            contextMenu = (ContextMenu)FindResource("ItemContextMenu");

            timeline.SelectedItems.CollectionChanged += SelectedChunks_CollectionChanged;
            timeline.OnChunkUpdated += EditTrack_OnChunkUpdated;

            bakTimer.Tick += BakTimer_Tick;
            bakTimer.Start();

            timeline.SuperSource = SuperList;

            timeline.svHor.ScrollChanged += SvHor_ScrollChanged;

        }


        private void LoadSubtitles(List<Subtitle> subtitles, string trackName) {
            SuperList.Clear();
            if (editTrack != null)
                timeline.RemoveTrack(editTrack);
            int i = 0;

            if (subtitles.Count <= 0) return;

            if (editTrack != null) Tracks.Remove(editTrack);
            Track track = new Track {
                Name = trackName
            };
            editTrack = track;
            Tracks.Add(editTrack);

            timeline.AddTrack(track, true);
            foreach (Subtitle sub in subtitles) {
                i++;

                Item item = new Item(sub) {
                    Index = i
                };
                SuperList.Add(item);
            }

            track.Super = SuperList;
            track.Streamed = new List<Item>();


            Item lastItem = SuperList[SuperList.Count-1];
            double margin = lastItem.Start.TotalSeconds / timescale * pixelscale;
            double width = lastItem.Dur.TotalSeconds / timescale * pixelscale;

            timeline.seekbar.MinWidth = margin + width;
        }

        private void LoadRefSubtitles(List<Subtitle> subtitles, string trackName) {
            if (refTrack != null)
                timeline.RemoveTrack(refTrack);
            int i = 0;

            if (subtitles.Count <= 0) return;


            if (refTrack != null) Tracks.Remove(refTrack);
            Track track = new Track {
                Name = trackName,
                Height = 50,
                Locked = true
            };
            refTrack = track;
            Tracks.Add(refTrack);

            timeline.AddTrack(track);

            foreach (Subtitle sub in subtitles) {
                i++;
                Item item = new Item(sub) {
                    Index = i
                };
                SuperListRef.Add(item);
            }

            track.Super = SuperListRef;
            track.Streamed = new List<Item>();
        }


        List<Track> Tracks { get; set; } = new List<Track>();


        private void SvHor_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (SuperList.Count < 1) return;
            if (e.HorizontalChange == 0) return;

            double val = timeline.svHor.HorizontalOffset / pixelscale * timescale;
            TimeSpan horizonLeft = TimeSpan.FromSeconds(val);
            val = (timeline.svHor.ViewportWidth + timeline.svHor.HorizontalOffset) / pixelscale * timescale;
            TimeSpan horizonRight = TimeSpan.FromSeconds(val);

            foreach (Track track in Tracks) {

                foreach (Item item in track.Super) {
                    if (item.Start <= horizonRight && item.End >= horizonLeft) {
                        if (!track.Streamed.Contains(item)) {
                            track.Streamed.Add(item);
                            double margin = item.Start.TotalSeconds / timescale * pixelscale;
                            double width = item.Dur.TotalSeconds / timescale * pixelscale;
                            Chunk chunk = new Chunk {
                                Margin = new Thickness(margin, 0, 0, 0),
                                Width = width,
                                Item = item
                            };
                            chunk.ContextMenu = contextMenu;
                            chunk.ContextMenuOpening += Chunk_ContextMenuOpening;
                            item.Chunk = chunk;
                            timeline.AddChunk(chunk, track);
                        }
                    }
                    else {
                        track.Streamed.Remove(item);
                        timeline.RemoveChunk(item.Chunk, track);
                    }
                }
            }
        }

        private void BakTimer_Tick(object sender, EventArgs e) {
            //MessageBox.Show("saving...");
            try {
                Project.Write(Project.FileName, true);
            }
            catch (IOException) {
                MessageBox.Show("IOException Error");
            }

        }

        #region Methods

        Item underNeedle;
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
            string str = "";
            underNeedle = null;

            foreach (Item item in SuperList) {
                if (position >= item.Start && position <= item.End) {
                    if (player.IsPlaying && item != underNeedle) {
                        listView.SelectionMode = SelectionMode.Single;
                        listView.SelectedItem = item;
                        listView.SelectionMode = SelectionMode.Extended;
                    }
                    str = item.Text;
                    underNeedle = item;
                    break;
                }
            }
            subDisplay.Text = str;



            slider.ValueChanged += Slider_ValueChanged;
            timeline.OnNeedleMoved += Timeline_OnNeedleMoved;
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

        private bool CloseProject() {

            if (unsavedChanges && UnsavedChangesDialog() == MessageBoxResult.Cancel) {
                return false;
            }

            SuperList.Clear();
            player.Stop();
            player.PlaylistClear();
            timeline.ClearTracks();
            Project.Data.Subtitles = null;
            Project.Data.RefSubtitles = null;
            UpdateTitle("Untitled");
            return true;
        }

        private void OpenProject(string filename, bool asBackup = false) {
            Project.Read(filename, asBackup);
            UpdateTitle(Path.GetFileName(filename));
            if (!string.IsNullOrEmpty(Project.Data.VideoPath))
                player.Load(Project.Data.VideoPath);


            Project.Data.Subtitles = Project.Data.Subtitles.OrderBy(subtitle => subtitle.Start).ToList();

            LoadSubtitles(Project.Data.Subtitles, Project.Data.TrackName);
            LoadRefSubtitles(Project.Data.RefSubtitles, Project.Data.RefTrackName);

            Task.Delay(200).ContinueWith(t => {
                Dispatcher.Invoke(() => {
                    timeline.svHor.ScrollToHorizontalOffset(Project.Data.ScrollPos);
                    Seek(TimeSpan.FromSeconds(Project.Data.VideoPos), null);
                });
            });
        }

        private MessageBoxResult UnsavedChangesDialog() {
            var result = MessageBox.Show(
                "There are unsaved changes. \nDo you really want to quit?",
                "Unsaved changes",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Exclamation
            );
            return result;
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

                    timeline.RemoveChunk(nextItem.Chunk, editTrack);

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
            if (listView.SelectionMode == SelectionMode.Single) return;
            listView.SelectionChanged -= listView_SelectionChanged;
            foreach (Item item in listView.SelectedItems) {
                item.Enabled = false;
            }
            listView.SelectedItems.Clear();
            foreach (Item item in timeline.SelectedItems) {
                listView.SelectedItems.Add(item);
                item.Enabled = true;
                listView.ScrollIntoView(item);
            }
            listView.SelectionChanged += listView_SelectionChanged;
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            foreach (Item item in e.RemovedItems) {
                item.Enabled = false;
                item.Selected = false;
                timeline.SelectedItems.Remove(item);
            }

            foreach (Item item in e.AddedItems) {
                item.Enabled = true;
                item.Selected = true;

                timeline.SelectedItems.Add(item);
            }
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {

            if (((FrameworkElement)e.OriginalSource).DataContext is Item item && player.IsMediaLoaded) {
                //player.Position = item.Sub.Start;
                Seek(item.Sub.Start);

                double margin = timeline.needle.Margin.Left;

                //timeline.svHor.ScrollChanged -= SvHor_ScrollChanged;
                //timeline.svHor.ScrollToHorizontalOffset(margin);
                //timeline.svHor.ScrollChanged += SvHor_ScrollChanged;
                timeline.needle.BringIntoView(new Rect(new Size(50, 50)));


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
            item.Text = textBox.Text;
            if (item == underNeedle) {
                subDisplay.Text = item.Text;
            }
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
                    Seek(e.NewPosition, player);
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
            if (dialog.ShowDialog() == true && CloseProject()) {
                Settings.Data.LastProject = dialog.FileName;
                Settings.Write();
                OpenProject(dialog.FileName);
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
                Project.Data.SelIndex = listView.SelectedIndex;
                Project.Data.VideoPos = player.Position.TotalSeconds;
                Project.Data.ScrollPos = timeline.svHor.HorizontalOffset;
                Project.Write(Project.FileName);
                unsavedChanges = false;
                UpdateTitle(_currentFile);
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

            if (unsavedChanges && UnsavedChangesDialog() == MessageBoxResult.Cancel) {
                e.Cancel = true;
            }
            else {
                if (WindowState == WindowState.Maximized) {
                    Settings.Data.Maximized = true;
                }
                Settings.Data.SafelyExited = true;
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
                    Item item = new Item(sub);
                    SuperList.Insert(beforeNeedle.Index, item);
                    editTrack.Streamed.Add(item);
                    RecalculateIndexes();
                    double left = item.Start.TotalSeconds / 10.0 * 1000.0;
                    double num = item.Dur.TotalSeconds / 10.0 * 1000.0;
                    Chunk chunk = new Chunk() {
                        Margin = new Thickness(left, 0.0, 0.0, 0.0),
                        Width = num,
                        Item = item
                    };

                    item.Chunk = chunk;
                    timeline.AddChunk(chunk, editTrack);
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
                Project.Data.Subtitles.Remove(item.Sub);
                timeline.RemoveChunk(item.Chunk, editTrack);
                editTrack.Super.Remove(item);
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

        private void ListView_TextInput(object sender, TextCompositionEventArgs e) {
            Debug.WriteLine("text input");
        }

        private void ListViewItem_TextInput(object sender, TextCompositionEventArgs e) {
            Debug.WriteLine("item text input");

        }
    }
}
