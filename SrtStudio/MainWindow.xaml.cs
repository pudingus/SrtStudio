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
        public MpvPlayer player;        

        public static MainWindow Instance { get; private set; }

        const string SRT_FILTER = "Srt - SubRip(*.srt)|*.srt";
        const string PROJ_EXT = "sprj";
        const string PROJ_FILTER = "SrtStudio Project (*.sprj)|*.sprj";
        const string VIDEO_FILTER = "Common video files (*.mkv;*.mp4;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts)|" +
            "*.mkv;*.mp4;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts|" +
            "All files (*.*)|*.*";


        ContextMenu itemContextMenu;
        ContextMenu insertContextMenu;

        Track editTrack;
        Track refTrack;

        readonly DispatcherTimer bakTimer = new DispatcherTimer() {
            Interval = TimeSpan.FromSeconds(10)
        };

        TextBox activeTextBox;

        #region Public Methods
        public MainWindow() {
            if (Instance == null) Instance = this;
            DataContext = this;
            InitializeComponent();
            OverrideLanguage();

            InitPlayer();


            UpdateTitle();

            Settings.Load();
            if (Settings.Data.Maximized) {
                WindowState = WindowState.Maximized;
            }
            RestoreBackup();
            if (Settings.Data.SafelyExited) {
                Settings.Data.SafelyExited = false;
                Settings.Save();
            }

            videoGrid.Children.Remove(wfHost);
            videoGrid.Children.Remove(topGrid);
            airControl.Back = wfHost;
            airControl.Front = topGrid;

            bakTimer.Tick += BakTimer_Tick;
            bakTimer.Start();

            InitTimeline();
        }

        public void LoadSubtitles(List<Subtitle> subtitles, string trackName) {
            if (editTrack != null) {
                editTrack.Items.Clear();
                timeline.Tracks.Remove(editTrack);
            }

            if (subtitles.Count <= 0) return;
            
            var track = new Track(timeline) {
                Name = trackName
            };
            editTrack = track;
            timeline.Tracks.Add(track);

            CreateItemsFromSubs(subtitles, track);
            
            //assign items to listview
            listView.ItemsSource = editTrack.Items;

            //ensure minimal width of the timeline
            Item lastItem = editTrack.Items[editTrack.Items.Count-1];
            double margin = lastItem.Start.TotalSeconds / timeline.Timescale * timeline.Pixelscale;
            double width = lastItem.Dur.TotalSeconds / timeline.Timescale * timeline.Pixelscale;
            timeline.seekbar.MinWidth = margin + width;
        }

        public void LoadRefSubtitles(List<Subtitle> subtitles, string trackName) {
            if (refTrack != null)
                timeline.Tracks.Remove(refTrack);


            if (subtitles.Count <= 0) return;
            
            var track = new Track(timeline) {
                Name = trackName,
                Height = 50,
                Locked = true
            };
            refTrack = track;
            timeline.Tracks.Insert(0, track);

            CreateItemsFromSubs(subtitles, track);
        }
                
        public void Seek(TimeSpan position, object except = null) {
            if (position.TotalMilliseconds < 0) position = TimeSpan.FromSeconds(0);
            slider.ValueChanged -= Slider_ValueChanged;
            timeline.NeedleMoved -= Timeline_NeedleMoved;

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


            string text = "";

            foreach (Item item in editTrack.Items) {
                if (position >= item.Start && position <= item.End) {
                    if (player.IsPlaying) {
                        listView.SelectionMode = SelectionMode.Single;
                        listView.SelectedItem = item;
                        listView.SelectionMode = SelectionMode.Extended;
                        /*
                        Task.Delay(100).ContinueWith(t => {
                            Dispatcher.Invoke(() => {
                                //listView.ScrollIntoView(item);
                            });
                        });
                        */
                    }
                    text = item.Text;
                    break;
                }
            }
            overlaySubs.Text = text;

            slider.ValueChanged += Slider_ValueChanged;
            timeline.NeedleMoved += Timeline_NeedleMoved;
        }

        public void Seek(double offset) {
            var newPos = player.Position + TimeSpan.FromMilliseconds(offset);
            Seek(newPos);
        }

        public void UpdateTitle() {
            string currentFile = Path.GetFileName(Project.FileName);
            string star = "";
            if (Project.UnsavedChanges) star = "*";
            int index = 0;
            int count = 0;
            double perc = 0.0;

            if (editTrack != null && editTrack.Items.Count > 0 && listView.SelectedIndex != -1) {
                index = listView.SelectedIndex + 1;
                count = editTrack.Items.Count;
                perc = (double)index / count * 100;
            }

            Title = $"{currentFile} {star} - {Local.PROGRAM_NAME} - {index}/{count} - {perc.ToString("N1")} %";
        }

        #endregion

        #region Private Methods

        void CreateItemsFromSubs(List<Subtitle> subtitles, Track track) {
            int i = 0;
            foreach (Subtitle sub in subtitles) {
                i++;                
                track.Items.Add(new Item(sub, i));
            }
        }

        void OverrideLanguage() {
            //use OS culture for bindings
            LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)
                )
            );
        }

        void RestoreBackup() {
            if (!Settings.Data.SafelyExited && Settings.Data.LastProject != null) {
                if (Dialogs.RestoreBackup() == MessageBoxResult.Yes) {
                    if (File.Exists(Settings.Data.LastProject + ".temp")) {
                        try {
                            Project.Open(Settings.Data.LastProject, true);
                        }
                        catch (Exception) {
                            MessageBox.Show("error");
                        }
                    }
                    else {
                        try {
                            Project.Open(Settings.Data.LastProject, false);
                        }
                        catch (Exception) {
                            MessageBox.Show("error");
                        }
                    }
                }
            }
        }

        void InitPlayer() {
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
        }

        void InitTimeline() {
            itemContextMenu = (ContextMenu)FindResource("ItemContextMenu");
            insertContextMenu = (ContextMenu)FindResource("InsertContextMenu");

            timeline.NeedleMoved += Timeline_NeedleMoved;
            timeline.SelectedItems.CollectionChanged += SelectedItems_CollectionChanged;
            timeline.ChunkUpdated += Timeline_ChunkUpdated;
            timeline.ChunkContextMenuOpening += Chunk_ContextMenuOpening;
            timeline.ChunkContextMenu = itemContextMenu;

            timeline.ContextMenuOpening += Timeline_ContextMenuOpening;
            timeline.ContextMenu = insertContextMenu;
        }        

        void RecalculateIndexes() {
            foreach (Item item in editTrack.Items) {
                item.Index = editTrack.Items.IndexOf(item) + 1;
            }
        }

        List<Item> SortAscendingByIndex(IList items) {
            var sl = new List<Item>();
            foreach (Item item in editTrack.Items)
                if (items.Contains(item)) sl.Add(item);
            return sl;
        }

        void Merge(IList items, bool asDialog = false) {
            //because 'items' could be in wrong order, SelectedItems has it in selection order
            var sl = SortAscendingByIndex(items);

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

                    editTrack.Items.Remove(nextItem);
                    Project.Data.Subtitles.Remove(nextItem.Sub);

                    timeline.RemoveChunkFromTrack(nextItem.Chunk, editTrack);

                    i++;
                }
            }
            RecalculateIndexes();

            Project.SignalChange();
        }
        #endregion

        #region Events
        void BakTimer_Tick(object sender, EventArgs e) {

            if (Project.UnwrittenChanges) {
                try {
                    Debug.WriteLine("saving backup...");
                    Project.Write(Project.FileName, true);
                }
                catch (IOException) {
                    MessageBox.Show("IOException Error");
                }
            }
        }

        void Player_MediaFinished(object sender, EventArgs e) {
            player.Pause();
            player.Position = player.Duration;
        }

        void Timeline_ContextMenuOpening(object sender, ContextMenuEventArgs e) {

        }

        void MenuItemInsert_Click(object sender, RoutedEventArgs e) {
            Action_InsertNewSubtitle();
        }

        void Timeline_NeedleMoved(object sender) {
            var timeline = (Timeline)sender;
            Seek(timeline.Position, timeline);
        }

        void Timeline_ChunkUpdated(object sender, Chunk chunk) {
            Project.SignalChange();
            UpdateTitle();
        }

        void SelectedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (listView.SelectionMode == SelectionMode.Single) return;
            listView.SelectionChanged -= ListView_SelectionChanged;
            foreach (Item item in listView.SelectedItems) {
                item.Enabled = false;
            }
            listView.SelectedItems.Clear();
            foreach (Item item in timeline.SelectedItems) {
                listView.SelectedItems.Add(item);
                item.Enabled = true;
                listView.ScrollIntoView(item);
            }
            listView.SelectionChanged += ListView_SelectionChanged;
        }

        void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateTimelineSelection(e.RemovedItems, e.AddedItems);
            UpdateTitle();
        }

        void UpdateTimelineSelection(IList removedItems, IList addedItems) {
            foreach (Item item in removedItems) {
                item.Enabled = false;
                item.Selected = false;
                timeline.SelectedItems.Remove(item);
            }

            foreach (Item item in addedItems) {
                item.Enabled = true;
                item.Selected = true;

                timeline.SelectedItems.Add(item);
            }
        }

        void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {

            if (((FrameworkElement)e.OriginalSource).DataContext is Item item) {
                //player.Position = item.Sub.Start;
                Seek(item.Sub.Start);
                timeline.RevealNeedle();
            }
        }
        #endregion

        #region TextBox Events
        void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            Debug.WriteLine("textbox preview keydown");
            var textBox = (TextBox)sender;
            if (e.Key == Key.F4) {
                Action_ShiftLineBreak(textBox);
                e.Handled = true;
            }

            if (e.Key == Key.I && (Keyboard.Modifiers == ModifierKeys.Control)) {
                Action_MakeSelectionItalic(textBox);                
                Debug.WriteLine("its italic time");
            }
        }

        void TextBox_SelectionChanged(object sender, RoutedEventArgs e) {
            var textBox = (TextBox)sender;
            activeTextBox = textBox;
            Debug.WriteLine(textBox.CaretIndex);
        }

        void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            var textBox = (TextBox)sender;
            var item = (Item)textBox.DataContext;
            item.Text = textBox.Text;
            if (item == editTrack.ItemUnderNeedle) {
                overlaySubs.Text = item.Text;
            }
            Project.SignalChange();
            UpdateTitle();
        }

        void TextBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var textBox = (TextBox)sender;

            Task.Delay(50).ContinueWith(t => {
                Dispatcher.Invoke(() => {
                    textBox.Focus();
                    textBox.CaretIndex = textBox.Text.Length;
                });
            });
        }
        #endregion

        #region Player and player controls Events
        void Button_Click(object sender, RoutedEventArgs e) {
            if (player.IsPlaying)
                player.Pause();
            else player.Resume();
        }

        void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (player.IsMediaLoaded) {
                double seconds = slider.Value / 100 * player.Duration.TotalSeconds;
                Seek(TimeSpan.FromSeconds(seconds), sender);
            }
        }

        void Player_PositionChanged(object sender, MpvPlayerPositionChangedEventArgs e) {
            Dispatcher.Invoke(() => {
                if (player.IsPlaying) {
                    Seek(e.NewPosition, player);
                }
            });
        }
        #endregion

        #region Menu Events
        void MenuProjectNew_Click(object sender, RoutedEventArgs e) {
            Project.Close();
            editTrack.Items.Clear();
        }

        void MenuProjectOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = PROJ_FILTER
            };
            if (dialog.ShowDialog() == true && Project.Close()) {
                Settings.Save();
                Project.Open(dialog.FileName);
            }
        }

        void MenuProjectSave_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(Project.FileName)) {
                SaveFileDialog dialog = new SaveFileDialog {
                    AddExtension = true,
                    DefaultExt = PROJ_EXT,
                    Filter = PROJ_FILTER
                };
                if (dialog.ShowDialog() == true) {
                    Project.Write(dialog.FileName);
                    UpdateTitle();
                }
            }
            else {
                Project.Data.SelIndex = listView.SelectedIndex;
                Project.Data.VideoPos = player.Position.TotalSeconds;
                Project.Data.ScrollPos = timeline.svHor.HorizontalOffset;
                Project.Write(Project.FileName);
                UpdateTitle();
            }
        }

        void MenuProjectSaveAs_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog {
                AddExtension = true,
                DefaultExt = PROJ_EXT,
                Filter = PROJ_FILTER
            };
            if (dialog.ShowDialog() == true) {
                Project.Write(dialog.FileName);
                UpdateTitle();
            }
        }

        void MenuProjectClose_Click(object sender, RoutedEventArgs e) {
            Project.Close();
            editTrack.Items.Clear();
        }

        void MenuVideoOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = VIDEO_FILTER
            };
            if (dialog.ShowDialog() == true) {
                player.Load(dialog.FileName);
                Project.Data.VideoPath = dialog.FileName;
                playButton.IsEnabled = true;
                slider.IsEnabled = true;
            }
        }

        void MenuVideoClose_Click(object sender, RoutedEventArgs e) {
            player.Stop();
            player.PlaylistClear();
            Project.Data.VideoPath = null;
            playButton.IsEnabled = false;
            slider.IsEnabled = false;
        }

        void MenuSrtImport_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = SRT_FILTER
            };
            if (dialog.ShowDialog() == true) {

                Project.Data.Subtitles = Srt.Read(dialog.FileName);
                Project.Data.TrackName = dialog.SafeFileName;
                LoadSubtitles(Project.Data.Subtitles, Project.Data.TrackName);
            }
        }

        void MenuSrtRefImport_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = SRT_FILTER
            };
            if (dialog.ShowDialog() == true) {
                Project.Data.RefSubtitles = Srt.Read(dialog.FileName);
                Project.Data.RefTrackName = dialog.SafeFileName;
                LoadRefSubtitles(Project.Data.RefSubtitles, Project.Data.RefTrackName);
            }
        }

        void MenuSrtExport_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog {
                AddExtension = true,
                DefaultExt = "srt",
                Filter = SRT_FILTER
            };
            if (dialog.ShowDialog() == true) {
                Srt.Write(dialog.FileName, Project.Data.Subtitles);
            }
        }

        void MenuEditInsert_Click(object sender, RoutedEventArgs e) {
            Action_InsertNewSubtitle();
        }

        void MenuEditTrimEnd_Click(object sender, RoutedEventArgs e) {
            Action_TrimEnd();
        }

        void MenuEditShiftLineBreak_Click(object sender, RoutedEventArgs e) {
            Action_ShiftLineBreak(activeTextBox);
        }
        #endregion

        #region Window Events
        void Window_Closing(object sender, CancelEventArgs e) {

            if (Project.UnsavedChanges) {
                var result = Dialogs.UnsavedChanges();
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
                            DefaultExt = PROJ_EXT,
                            Filter = PROJ_FILTER
                        };
                        if (dialog.ShowDialog() == true) {
                            try {
                                Project.Write(dialog.FileName);
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
                            Project.Write(Project.FileName);
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
                var tempFile = Project.FileName + ".temp";
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        void Window_PreviewKeyDown(object sender, KeyEventArgs e) {            

            switch (e.Key) {
                case Key.F5:
                    Action_PlayPause();
                    break;
                case Key.F2:
                    Action_InsertNewSubtitle();
                    break;
                case Key.F8:
                    Action_TrimEnd();
                    break;
                case Key.Right:
                    if (activeTextBox != null && !activeTextBox.IsFocused)
                        Seek(100.0);
                    break;
                case Key.Left:
                    if (activeTextBox != null && !activeTextBox.IsFocused)
                        Seek(-100.0);
                    break;
            }
        }

        void MenuOptionsRipple_Click(object sender, RoutedEventArgs e) {
            //IsChecked reports value after click
            //MessageBox.Show($"checked: {menuOptionsRipple.IsChecked}");
            timeline.Ripple = menuOptionsRipple.IsChecked;
        }
        #endregion

        #region ACTIONS

        void Action_PlayPause() {
            if (player.IsPlaying)
                player.Pause();
            else player.Resume();
        }          

        void Action_ShiftLineBreak(TextBox textBox) {
            int caretIndex = textBox.CaretIndex;
            string str = RemoveNewlines(textBox.Text);
            str = str.Insert(caretIndex, Environment.NewLine);
            str = TrimSpaces(str);
            textBox.Text = str;
            textBox.CaretIndex = caretIndex + 1;
        }

        void Action_MakeSelectionItalic(TextBox textBox) {
            int start = textBox.SelectionStart;
            int length = textBox.SelectionLength;

            string newText = textBox.Text.Insert(start, "<i>");
            newText = newText.Insert(start + length + 3, "</i>");

            textBox.Text = newText;
            textBox.SelectionStart = start;
            textBox.SelectionLength = length + 7;
        }

        void Action_TrimEnd(Item item) {
            if (item != null) {
                TimeSpan dur, pos, start;
                pos = timeline.Position;
                start = item.Start;
                dur = pos - start;
                if (dur < TimeSpan.FromSeconds(1.2)) {
                    MessageBox.Show("too short, correcting...");
                    item.End = item.Start + TimeSpan.FromSeconds(1.2);
                    item.Chunk.Update();
                    Project.SignalChange();
                }
                else {
                    item.End = timeline.Position;
                    item.Chunk.Update();
                    Project.SignalChange();
                }
            }
        }

        void Action_TrimEnd() {
            Action_TrimEnd(editTrack.ItemUnderNeedle);
        }

        void Action_InsertNewSubtitle() {
            Item beforeNeedle = null;
            for (int index = editTrack.Items.Count - 1; index >= 0; --index) {
                if (player.Position >= editTrack.Items[index].Start) {
                    beforeNeedle = editTrack.Items[index];
                    break;
                }
            }
            if (beforeNeedle != null) {
                var sub = new Subtitle() {
                    Start = player.Position,
                    End = player.Position + TimeSpan.FromSeconds(1.5),
                    Text = string.Empty
                };
                Project.Data.Subtitles.Insert(beforeNeedle.Index, sub);

                var item = new Item(sub);
                editTrack.Items.Insert(beforeNeedle.Index, item);
                editTrack.StreamedItems.Add(item);
                RecalculateIndexes();

                var chunk = new Chunk(editTrack, item) {
                    ContextMenu = itemContextMenu,
                };
                chunk.ContextMenuOpening += Chunk_ContextMenuOpening;
                item.Chunk = chunk;
                timeline.AddChunkToTrack(chunk, editTrack);
                timeline.SelectedItems.Add(item);
                listView.SelectedItems.Clear();

                listView.SelectedItems.Add(item);
                item.Selected = true;

                Project.SignalChange();
            }
        }
        #endregion


        #region Context Menu Events
        void MenuItemMerge_Click(object sender, RoutedEventArgs e) {
            if (listView.SelectedItems.Count <= 1) return;

            Merge(listView.SelectedItems);
        }

        void MenuItemMergeDialog_Click(object sender, RoutedEventArgs e) {
            if (listView.SelectedItems.Count <= 1) return;

            Merge(listView.SelectedItems, true);
        }

        void MenuItemDelete_Click(object sender, RoutedEventArgs e) {
            var copy = new List<Item>();
            foreach (Item item in listView.SelectedItems) copy.Add(item);

            foreach (Item item in copy) {
                editTrack.Items.Remove(item);
                Project.Data.Subtitles.Remove(item.Sub);
                timeline.RemoveChunkFromTrack(item.Chunk, editTrack);
                editTrack.Items.Remove(item);
            }
            RecalculateIndexes();
            Project.SignalChange();
        }

        void PrepareContextMenu(Item item, ContextMenu contextMenu) {
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

        void ListViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            var lvItem = (ListViewItem)sender;
            Item item = (Item)lvItem.DataContext;
            ContextMenu contextMenu = lvItem.ContextMenu;
            PrepareContextMenu(item, contextMenu);
        }

        void Chunk_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
            Debug.WriteLine("Chunk_ContextMenuOpening");
            Chunk chunk = (Chunk)sender;
            Item item = (Item)chunk.DataContext;
            ContextMenu contextMenu = chunk.ContextMenu;
            PrepareContextMenu(item, contextMenu);
        }
        #endregion

        void ListView_TextInput(object sender, TextCompositionEventArgs e) {
            Debug.WriteLine("text input");
        }

        void ListViewItem_TextInput(object sender, TextCompositionEventArgs e) {
            Debug.WriteLine("item text input");
        }
    }
}
