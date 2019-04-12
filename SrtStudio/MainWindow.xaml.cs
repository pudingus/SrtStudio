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

        /// <summary>
        /// K čemu to kurňa je?
        /// </summary>
        public ObservableCollection<Item> SuperList { get; set; } = new ObservableCollection<Item>();
        public ObservableCollection<Item> SuperListRef { get; set; } = new ObservableCollection<Item>();


        Track editTrack;
        Track refTrack;





        const string srtFilter = "Srt - SubRip(*.srt)|*.srt";
        const string projExt = "sprj";
        const string projFilter = "SrtStudio Project (*.sprj)|*.sprj";
        const string videoFilter = "Common video files (*.mkv;*.mp4;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts)|" +
            "*.mkv;*.mp4;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts|" +
            "All files (*.*)|*.*";

        ContextMenu contextMenu;

        private string _currentFile = "";

        DispatcherTimer bakTimer = new DispatcherTimer() {
            Interval = TimeSpan.FromSeconds(10)
        };

        private TextBox curTextBox;

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
            player.API.SetPropertyString("keep-open", "yes");
            player.PositionChanged += Player_PositionChanged;
            player.MediaFinished += Player_MediaFinished;
            //player.API.SetPropertyString("demuxer-max-back-bytes", "50MiB");
            //player.API.SetPropertyString("demuxer-max-bytes", "150MiB");


            //MessageBox.Show(player.API.GetPropertyString("cache"));
            //MessageBox.Show(player.API.GetPropertyString("cache-secs"));
            //MessageBox.Show(player.API.GetPropertyString("demuxer-max-back-bytes"));

            //MessageBox.Show(player.API.GetPropertyString("demuxer-max-bytes"));



            UpdateTitle("Untitled");

            Settings.Load();
            if (Settings.Data.Maximized) {
                WindowState = WindowState.Maximized;
            }
            if (!Settings.Data.SafelyExited && Settings.Data.LastProject != null) {
                var result = MessageBox.Show(
                    $"Program didn't safely exit last time, \ndo you want to restore backup for {Path.GetFileName(Settings.Data.LastProject)}?",
                    "Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
                if (result == MessageBoxResult.Yes) {
                    try {
                        OpenProject(Settings.Data.LastProject, true);
                    }
                    catch (Exception) {
                        MessageBox.Show("error");
                    }

                }
            }
            if (Settings.Data.SafelyExited) {
                Settings.Data.SafelyExited = false;
                Settings.Save();
            }

            videoGrid.Children.Remove(wfHost);
            videoGrid.Children.Remove(topGrid);
            airControl.Back = wfHost;
            airControl.Front = topGrid;
            contextMenu = (ContextMenu)FindResource("ItemContextMenu");

            bakTimer.Tick += BakTimer_Tick;
            bakTimer.Start();

            timeline.OnNeedleMoved += Timeline_OnNeedleMoved;
            timeline.SelectedItems.CollectionChanged += SelectedChunks_CollectionChanged;
            timeline.OnChunkUpdated += Timeline_OnChunkUpdated;
            timeline.ChunkContextMenuOpening += Chunk_ContextMenuOpening;
            timeline.ChunkContextMenu = contextMenu;

            timeline.ContextMenuOpening += Timeline_ContextMenuOpening;
            timeline.ContextMenu = (ContextMenu)FindResource("InsertContextMenu");
        }

        private void Player_MediaFinished(object sender, EventArgs e) {
            player.Pause();
            player.Position = player.Duration;
        }

        private void Timeline_ContextMenuOpening(object sender, ContextMenuEventArgs e) {

        }


        private void MenuItemInsert_Click(object sender, RoutedEventArgs e) {
            Action_Insert();
        }

        private void LoadSubtitles(List<Subtitle> subtitles, string trackName) {
            SuperList.Clear();
            if (editTrack != null)
                timeline.RemoveTrack(editTrack);

            if (subtitles.Count <= 0) return;

            Track track = new Track {
                Name = trackName,
                Super = SuperList,
                Streamed = new List<Item>()
            };
            editTrack = track;
            timeline.AddTrack(track, true);

            CreateItemsFromSubs(subtitles, track);

            Item lastItem = SuperList[SuperList.Count-1];
            double margin = lastItem.Start.TotalSeconds / timeline.timescale * timeline.pixelscale;
            double width = lastItem.Dur.TotalSeconds / timeline.timescale * timeline.pixelscale;

            timeline.seekbar.MinWidth = margin + width;
        }

        private void LoadRefSubtitles(List<Subtitle> subtitles, string trackName) {
            if (refTrack != null)
                timeline.RemoveTrack(refTrack);

            if (subtitles.Count <= 0) return;

            Track track = new Track {
                Name = trackName,
                Height = 50,
                Locked = true,
                Super = SuperListRef,
                Streamed = new List<Item>()
            };
            refTrack = track;
            timeline.AddTrack(track);

            CreateItemsFromSubs(subtitles, track);
        }

        private void CreateItemsFromSubs(List<Subtitle> subtitles, Track track) {
            int i = 0;
            foreach (Subtitle sub in subtitles) {
                i++;
                Item item = new Item(sub) {
                    Index = i
                };
                track.Super.Add(item);
            }
        }

        private void BakTimer_Tick(object sender, EventArgs e) {

            if (Project.UnwrittenChanges) {
                try {
                    Debug.WriteLine("saving backup...");
                    Project.Save(Project.FileName, true);
                }
                catch (IOException) {
                    MessageBox.Show("IOException Error");
                }
            }
        }

        #region Methods

        Item underNeedle;
        private void Seek(TimeSpan position, object except = null) {
            slider.ValueChanged -= Slider_ValueChanged;
            timeline.OnNeedleMoved -= Timeline_OnNeedleMoved;

            if (except != timeline) {
                timeline.Position = position;
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

                        Task.Delay(100).ContinueWith(t => {
                            Dispatcher.Invoke(() => {
                                //listView.ScrollIntoView(item);

                            });
                        });
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
            if (Project.UnsavedChanges) str = "*";
            int index = 0;
            int count = 0;
            double perc = 0.0;

            if (SuperList.Count > 0 && listView.SelectedIndex != -1) {
                index = listView.SelectedIndex + 1;
                count = SuperList.Count;
                perc = (double)index / count * 100;
            }

            Title = currentFile + str + " - SrtStudio - " + index + "/" + count + " - " + perc.ToString("N1") + " %";
        }

        private bool CloseProject() {

            if (Project.UnsavedChanges && UnsavedChangesDialog() == MessageBoxResult.Cancel) {
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
            Project.Load(filename, asBackup);
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

                    timeline.stack.MinWidth = player.Duration.TotalSeconds / timeline.timescale * timeline.pixelscale;

                });
            });

            listView.SelectedIndex = Project.Data.SelIndex;


        }

        private MessageBoxResult UnsavedChangesDialog() {
            var result = MessageBox.Show(
                $"Do you want to save changes you made to {Path.GetFileName(Project.FileName)}? \n\nYour changes will be lost if you don't save them.",
                "Save changes?",
                MessageBoxButton.YesNoCancel,
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
            //sort 'items' by subtitle index in ascending order
            var sl = new List<Item>();
            foreach (Item item in SuperList)
                if (items.Contains(item)) sl.Add(item);

            for (int i = 0; i < sl.Count-1; i++) {
                Item item = sl[i];
                Item nextItem = sl[i+1];
                //next item is 'neighbor' to current item
                if (nextItem.Index == item.Index + 1) {
                    item.End = nextItem.End;
                    item.Chunk.Update();
                    if (!asDialog)
                        item.Text += " " + nextItem.Text;
                    else
                        item.Text = "-" + item.Text + Environment.NewLine + "-" + nextItem.Text;

                    SuperList.Remove(nextItem);
                    Project.Data.Subtitles.Remove(nextItem.Sub);

                    timeline.RemoveChunk(nextItem.Chunk, editTrack);

                    i++;
                }
            }
            RecalculateIndexes();

            Project.FlagChange();
        }
        #endregion


        private void Timeline_OnNeedleMoved() {

            Seek(timeline.Position, timeline);
        }

        private void Timeline_OnChunkUpdated(Chunk chunk) {
            Project.FlagChange();
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
            UpdateTitle(_currentFile);
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {

            if (((FrameworkElement)e.OriginalSource).DataContext is Item item && player.IsMediaLoaded) {
                //player.Position = item.Sub.Start;
                Seek(item.Sub.Start);
                timeline.FocusNeedle();
            }
        }

        #region TextBox Events
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            Debug.WriteLine("textbox preview keydown");
            TextBox textBox = (TextBox)sender;
            if (e.Key == Key.F4) {
                Action_MoveLineEnd();
                e.Handled = true;
            }

            if (e.Key == Key.I && (Keyboard.Modifiers == ModifierKeys.Control)) {

                int start = curTextBox.SelectionStart;
                int length = curTextBox.SelectionLength;

                string newText = curTextBox.Text.Insert(start, "<i>");
                newText = newText.Insert(start + length + 3, "</i>");

                curTextBox.Text = newText;
                curTextBox.SelectionStart = start;
                curTextBox.SelectionLength = length + 7;

                Debug.WriteLine("its italic time");
            }
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            curTextBox = textBox;
            Debug.WriteLine(((TextBox)sender).CaretIndex);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            Item item = (Item)textBox.DataContext;
            item.Text = textBox.Text;
            if (item == underNeedle) {
                subDisplay.Text = item.Text;
            }
            Project.FlagChange();
            UpdateTitle(_currentFile);
        }

        private void TextBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            TextBox textBox = (TextBox)sender;

            Task.Delay(50).ContinueWith(t => {
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
        private void menuProjectNew_Click(object sender, RoutedEventArgs e) {
            CloseProject();
        }

        private void menuProjectOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = projFilter
            };
            if (dialog.ShowDialog() == true && CloseProject()) {
                Settings.Save();
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
                    Project.Save(dialog.FileName);
                    UpdateTitle(dialog.SafeFileName);
                }
            }
            else {
                Project.Data.SelIndex = listView.SelectedIndex;
                Project.Data.VideoPos = player.Position.TotalSeconds;
                Project.Data.ScrollPos = timeline.svHor.HorizontalOffset;
                Project.Save(Project.FileName);
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
                Project.Save(dialog.FileName);
                UpdateTitle(dialog.SafeFileName);
            }
        }

        private void menuProjectClose_Click(object sender, RoutedEventArgs e) {
            CloseProject();
        }

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

        private void MenuEditInsert_Click(object sender, RoutedEventArgs e) {
            Action_Insert();
        }

        private void MenuEditTrimEnd_Click(object sender, RoutedEventArgs e) {
            Action_TrimEnd();
        }

        private void MenuEditMoveLineEnd_Click(object sender, RoutedEventArgs e) {
            Action_MoveLineEnd();
        }
        #endregion

        #region Window Events
        private void Window_Closing(object sender, CancelEventArgs e) {

            if (Project.UnsavedChanges) {
                var result = UnsavedChangesDialog();
                if (result == MessageBoxResult.Cancel) {
                    e.Cancel = true;
                    return;
                }
                else if (result == MessageBoxResult.No) {
                    if (WindowState == WindowState.Maximized) {
                        Settings.Data.Maximized = true;
                    }

                }
                else if (result ==  MessageBoxResult.Yes) {
                    if (string.IsNullOrEmpty(Project.FileName)) {
                        SaveFileDialog dialog = new SaveFileDialog {
                            AddExtension = true,
                            DefaultExt = projExt,
                            Filter = projFilter
                        };
                        if (dialog.ShowDialog() == true) {
                            try {
                                Project.Save(dialog.FileName);
                            }
                            catch (IOException ex) {
                                MessageBox.Show(ex.Message, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                                e.Cancel = true;
                                return;
                            }
                        }
                    }
                    else {
                        Project.Data.SelIndex = listView.SelectedIndex;
                        Project.Data.VideoPos = player.Position.TotalSeconds;
                        Project.Data.ScrollPos = timeline.svHor.HorizontalOffset;
                        try {
                            Project.Save(Project.FileName);
                        }
                        catch (IOException ex) {
                            MessageBox.Show(ex.Message, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                            e.Cancel = true;
                            return;
                        }
                    }
                    if (WindowState == WindowState.Maximized) {
                        Settings.Data.Maximized = true;
                    }
                    Settings.Data.SafelyExited = true;
                    Settings.Save();
                }
            }

            Settings.Data.SafelyExited = true;
            Settings.Save();

            if (string.IsNullOrEmpty(Project.FileName)) MessageBox.Show("empty project.filename");
            else {
                if (File.Exists(Project.FileName + ".temp")) {
                    File.Delete(Project.FileName + ".temp");

                }
            }
        }

        private void Action_PlayPause() {
            if (player.IsPlaying)
                player.Pause();
            else player.Resume();
        }

        private void Action_Insert() {
            InsertNewSubtitle();
        }

        private void Action_TrimEnd() {
            TrimEnd(underNeedle);
        }

        private void Action_MoveLineEnd() {
            TextBox textBox = curTextBox;
            int caretIndex = textBox.CaretIndex;
            string str = RemoveNewlines(textBox.Text);
            str = str.Insert(caretIndex, Environment.NewLine);
            str = TrimSpaces(str);
            textBox.Text = str;
            textBox.CaretIndex = caretIndex + 1;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.F5) {
                Action_PlayPause();
            }
            if (e.Key == Key.F2) {
                Action_Insert();
            }
            if (e.Key == Key.F8) {
                Action_TrimEnd();
            }
            if (e.Key == Key.Right) {
                var newPos = player.Position + TimeSpan.FromMilliseconds(100);
                Seek(newPos);
            }
            if (e.Key == Key.Left) {
                var newPos = player.Position - TimeSpan.FromMilliseconds(100);
                Seek(newPos);
            }
        }

        private void MenuOptionsRipple_Click(object sender, RoutedEventArgs e) {
            //IsChecked reports value after click
            //MessageBox.Show($"checked: {menuOptionsRipple.IsChecked}");
            timeline.Ripple = menuOptionsRipple.IsChecked;
        }

        private void TrimEnd(Item item) {
            if (item != null) {
                TimeSpan dur, pos, start;
                pos = timeline.Position;
                start = item.Start;
                dur = pos - start;
                if (dur < TimeSpan.FromSeconds(1.2)) {
                    MessageBox.Show("too short, correcting...");
                    item.End = item.Start + TimeSpan.FromSeconds(1.2);
                    item.Chunk.Update();
                    Project.FlagChange();
                }
                else {
                    item.End = timeline.Position;
                    item.Chunk.Update();
                    Project.FlagChange();
                }
            }
        }

        private void InsertNewSubtitle() {
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
                Chunk chunk = new Chunk(timeline, item);
                chunk.ContextMenu = contextMenu;
                chunk.ContextMenuOpening += Chunk_ContextMenuOpening;
                item.Chunk = chunk;
                timeline.AddChunk(chunk, editTrack);
                timeline.SelectedItems.Add(item);
                listView.SelectedItems.Clear();

                listView.SelectedItems.Add(item);
                item.Selected = true;

                Project.FlagChange();
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
            Project.FlagChange();
        }

        private void PrepareContextMenu(Item item, ContextMenu contextMenu) {
            MenuItem merge = (MenuItem)contextMenu.Items[0];
            MenuItem mergeDialog = (MenuItem)contextMenu.Items[1];
            MenuItem delete = (MenuItem)contextMenu.Items[2];
            merge.IsEnabled = false;
            mergeDialog.IsEnabled = false;
            delete.IsEnabled = false;
            if (!listView.SelectedItems.Contains(item))
                return;
            int count = listView.SelectedItems.Count;
            if (count >= 1)
                delete.IsEnabled = true;
            if (count >= 2) {
                merge.IsEnabled = true;
                mergeDialog.IsEnabled = true;
            }
        }

        private void ListViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            var lvItem = (ListViewItem)sender;
            Item item = (Item)lvItem.DataContext;
            ContextMenu contextMenu = lvItem.ContextMenu;
            PrepareContextMenu(item, contextMenu);
        }

        private void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            Debug.WriteLine("Chunk_ContextMenuOpening");
            Chunk chunk = (Chunk)sender;
            Item item = (Item)chunk.DataContext;
            ContextMenu contextMenu = chunk.ContextMenu;
            PrepareContextMenu(item, contextMenu);
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
