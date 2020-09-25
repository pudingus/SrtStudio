using Microsoft.Win32;
using Mpv.NET.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using static SrtStudio.StringOperations;
using Path = System.IO.Path;

namespace SrtStudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///

    public partial class MainWindow : Window
    {
        public MpvPlayer player;
        TextBox activeTextBox;
        readonly AirWindow airWindow = new AirWindow();

        SettingsStorage settings;
        Project project;

        const string SRT_FILTER = "Srt - SubRip(*.srt)|*.srt";
        const string PROJ_EXT = "sprj";
        const string PROJ_FILTER = "SrtStudio Project (*.sprj)|*.sprj";
        const string VIDEO_FILTER = "Common video files (*.mkv;*.mp4;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts)|" +
            "*.mkv;*.mp4;*.avi;*.flv;*.webm;*.mov;*.m4v;*.3gp;*.wmv;*.ts|" +
            "All files (*.*)|*.*";

        readonly DispatcherTimer bakTimer = new DispatcherTimer() {
            Interval = TimeSpan.FromSeconds(10)
        };

        //readonly MainViewModel viewModel = new MainViewModel();
        readonly MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            //DataContext = viewModel;
            viewModel = (MainViewModel)DataContext;
            airWindow.DataContext = viewModel;

            OverrideLanguage();                       

            settings = Settings.Load();
            if (settings.Maximized) {
                WindowState = WindowState.Maximized;
            }

            player = CreatePlayer();
            project = RestoreBackup(settings);

            if (settings.SafelyExited) {
                settings.SafelyExited = false;
                Settings.Save(settings);
            }

            Title = GetTitle(project, listView.SelectedIndex);

            InitTimeline(timeline);

            LoadSubtitles(project, timeline);
            LoadRefSubtitles(project, timeline);

            playerGrid.Children.Remove(overlayGrid);
            airWindow.contentGrid.Children.Add(overlayGrid);

            bakTimer.Tick += BakTimer_Tick;
            bakTimer.Start();

            MpvPlayer CreatePlayer()
            {
                var player = new MpvPlayer(playerHost.Handle) {
                    Loop = true,
                    Volume = 100
                };
                player.API.SetPropertyString("sid", "no");  //disable subtitles
                player.API.SetPropertyString("keep-open", "yes");
                player.PositionChanged += Player_PositionChanged;
                player.MediaFinished += Player_MediaFinished;
                player.MediaLoaded += Player_MediaLoaded;
                return player;
            }

            void InitTimeline(Timeline timeline)
            {
                var itemContextMenu = (ContextMenu)FindResource("ItemContextMenu");
                var insertContextMenu = (ContextMenu)FindResource("InsertContextMenu");

                timeline.NeedleMoved += Timeline_NeedleMoved;
                timeline.SelectedItems.CollectionChanged += Timeline_SelectedItems_CollectionChanged;
                timeline.ChunkContextMenu = itemContextMenu;

                timeline.ContextMenu = insertContextMenu;

                var refTrack = new Track(timeline, true) {
                    Height = 50,
                    Name = "",
                };                

                var editTrack = new Track(timeline, false) {
                    Name = "",
                };

                timeline.Tracks.Add(refTrack);
                timeline.Tracks.Add(editTrack);

                editTrack.ChunkContextMenuOpening += Track_ChunkContextMenuOpening;
            }

            void OverrideLanguage()
            {
                //use OS culture for bindings
                LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(
                        XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)
                    )
                );
            }
        }

        Project OpenProject(string filename, bool asBackup = false) {
            var project = Project.Read(filename, asBackup);
            settings.LastProject = filename;
            Title = GetTitle(project, project.SelIndex);
            if (!string.IsNullOrEmpty(project.VideoPath))
                player.Load(project.VideoPath);

            //project.Subtitles = project.Subtitles.OrderBy(subtitle => subtitle.Start).ToList();

            RecalculateIndexes(project.Subtitles);
            RecalculateIndexes(project.RefSubtitles);

            LoadSubtitles(project, timeline);
            LoadRefSubtitles(project, timeline);

            Task.Delay(200).ContinueWith(t => {
                Dispatcher.Invoke(() => {
                    timeline.HorizontalOffset = project.ScrollPos;
                    Seek(TimeSpan.FromSeconds(project.VideoPos), null);
                });
            });


            timeline.SelectedItems.CollectionChanged -= Timeline_SelectedItems_CollectionChanged;
            listView.SelectionChanged -= ListView_SelectionChanged;
            timeline.SelectedItems.Add(project.Subtitles[project.SelIndex]);
            listView.SelectedIndex = project.SelIndex;
            listView.SelectionChanged += ListView_SelectionChanged;
            timeline.SelectedItems.CollectionChanged += Timeline_SelectedItems_CollectionChanged;

            return project;
        }

        Project RestoreBackup(SettingsStorage settings) {
            Project project = null;
            if (!settings.SafelyExited && settings.LastProject != null) {
                var lastProject = Path.GetFileName(settings.LastProject);
                if (Dialogs.RestoreBackup(lastProject) == MessageBoxResult.Yes) {
                    if (File.Exists(settings.LastProject + ".temp")) {
                        try {
                            project = OpenProject(settings.LastProject, true);
                        }
                        catch (Exception) {
                            MessageBox.Show("error");
                        }
                    }
                    else {
                        try {
                            project = OpenProject(settings.LastProject, false);
                        }
                        catch (Exception) {
                            MessageBox.Show("error");
                        }
                    }
                }
            }
            if (project == null) project = new Project();
            return project;
        }

        void LoadSubtitles(Project project, Timeline timeline) {
            var editTrack = timeline.Tracks[1];

            viewModel.Items = project.Subtitles;
            editTrack.Items = project.Subtitles;
            editTrack.Name = project.TrackName;

            project.Subtitles.CollectionChanged += Subtitles_CollectionChanged;

            foreach (Subtitle sub in project.Subtitles) {
                sub.PropertyChanged += Subtitle_PropertyChanged;
            }
        }

        void LoadRefSubtitles(Project project, Timeline timeline) {
            var refTrack = timeline.Tracks[0];
            refTrack.Items = project.RefSubtitles;
            refTrack.Name = project.RefTrackName;
        }

        bool CloseProject() {
            var projectFilename = Path.GetFileName(project.FileName);

            if (project.UnsavedChanges && Dialogs.SaveChanges(projectFilename) == MessageBoxResult.Cancel) {
                return false;
            }

            var refTrack = timeline.Tracks[0];
            var editTrack = timeline.Tracks[1];

            refTrack.Items.Clear();
            editTrack.Items.Clear();

            refTrack.Name = "";
            editTrack.Name = "";

            player.Stop();
            player.PlaylistClear();
            project = new Project();
            viewModel.Items = project.Subtitles;
            viewModel.ActiveItem = null;
            Title = GetTitle(project, listView.SelectedIndex);
            return true;
        }

        void UpdateOverlay()
        {
            if (wfHost.IsVisible) {
                Point hostTopLeft = wfHost.PointToScreen(new Point(0, 0));

                airWindow.Left = hostTopLeft.X;
                airWindow.Top = hostTopLeft.Y;
                airWindow.Width = wfHost.ActualWidth;
                airWindow.Height = wfHost.ActualHeight;
            }            
        }      

        void SignalChange(Project project) {
            project.SignalChange();
            Title = GetTitle(project, listView.SelectedIndex);
        }

        Subtitle GetItemUnderNeedle(TimeSpan position, IList<Subtitle> subtitles) {
            Subtitle underNeedle = null;

            foreach (Subtitle subtitle in subtitles) {
                if (position >= subtitle.Start && position <= subtitle.End) {
                    underNeedle = subtitle;
                    break;
                }
            }
            return underNeedle;
        }

        Subtitle FindSubtitleBefore(TimeSpan position, IList<Subtitle> items) {
            Subtitle beforeNeedle = null;
            for (int index = items.Count - 1; index >= 0; --index) {
                if (position >= items[index].Start) {
                    beforeNeedle = items[index];
                    break;
                }
            }
            return beforeNeedle;
        }

        void Seek(TimeSpan position, object except = null)
        {
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
                    if (position > player.Duration) player.Position = player.Duration;
                    else player.Position = position;
                }
            }

            Subtitle underNeedle = GetItemUnderNeedle(position, project.Subtitles);  

            if (underNeedle != null && player.IsPlaying) {
                listView.SelectionMode = SelectionMode.Single;
                listView.SelectedItem = underNeedle;
                listView.SelectionMode = SelectionMode.Extended;
            }            

            viewModel.ActiveItem = underNeedle;

            slider.ValueChanged += Slider_ValueChanged;
            timeline.NeedleMoved += Timeline_NeedleMoved;
        }

        void Seek(double offset)
        {
            var newPos = player.Position + TimeSpan.FromMilliseconds(offset);
            Seek(newPos);
        }

        string GetTitle(Project project, int selIndex)
        {
            string currentFile = "Untitled";
            if (project.FileName != null) {
                currentFile = Path.GetFileName(project.FileName);
            }
            string star = "";
            if (project.UnsavedChanges) star = "*";
            int index = 0;
            int count = project.Subtitles.Count;
            double perc = 0.0;

            if (count > 0 && selIndex != -1) {
                index = selIndex + 1;
                count = project.Subtitles.Count;
                perc = (double)index / count * 100;
            }

            return $"{currentFile} {star} - {Local.PROGRAM_NAME} - {index}/{count} - {perc:N1} %";
        }    

        void RecalculateIndexes(IList subtitles)
        {
            foreach (Subtitle subtitle in subtitles) {
                subtitle.Index = subtitles.IndexOf(subtitle) + 1;                
            }
        }

        IList SortByList(IList items, IList ordered) 
        {
            IList sl = new List<object>();
            foreach (var subtitle in ordered)
                if (items.Contains(subtitle)) sl.Add(subtitle);
            return sl;
        }

        void Merge(IList items, IList collection, bool asDialog = false)
        {
            //because 'items' could be in wrong order, listview SelectedItems are in selection order
            var sl = SortByList(items, collection);

            for (int i = 0; i < sl.Count-1; i++) {
                var subtitle = (Subtitle)sl[i];
                var nextSub = (Subtitle)sl[i+1];
                //next subtitle is 'neighbor' to current subtitle
                if (nextSub.Index == subtitle.Index + 1) {
                    subtitle.End = nextSub.End;
                    if (!asDialog)
                        subtitle.Text += " " + nextSub.Text;
                    else
                        subtitle.Text = "-" + subtitle.Text + Environment.NewLine + "-" + nextSub.Text;

                    collection.Remove(nextSub);

                    i++;
                }
            }
            RecalculateIndexes(project.Subtitles);
        }

        void UpdateSelection(Timeline timeline, IList removedItems, IList addedItems) {
            foreach (Subtitle subtitle in removedItems) {
                subtitle.TbxEnabled = false;
                //subtitle.Selected = false;
                timeline.SelectedItems.Remove(subtitle);
            }

            foreach (Subtitle subtitle in addedItems) {
                subtitle.TbxEnabled = true;
                //subtitle.Selected = true;
                timeline.SelectedItems.Add(subtitle);
            }
        }


        #region Events
        void Subtitles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            Debug.WriteLine("Subtitles_CollectionChanged");

            SignalChange(project);
        }

        void Subtitle_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            var subtitle = (Subtitle)sender;
            var position = timeline.Position;
            if (position >= subtitle.Start && position <= subtitle.End) {
                viewModel.ActiveItem = subtitle;
            }
            else viewModel.ActiveItem = null;


            //Debug.WriteLine($"Sub_PropertyChanged {e.PropertyName}");
            //Debug.WriteLine($"nameof {nameof(Subtitle.TbxEnabled)}");

            if (e.PropertyName != nameof(Subtitle.TbxEnabled)) {                
                SignalChange(project);
            }
        }

        void WfHost_LayoutUpdated(object sender, EventArgs e) {
            UpdateOverlay();
        }

        void BakTimer_Tick(object sender, EventArgs e)
        {
            if (project.UnwrittenChanges) {
                try {
                    Debug.WriteLine("saving backup...");
                    project.Write(asBackup: true);
                    settings.LastProject = project.FileName;
                }
                catch (IOException) {
                    MessageBox.Show("IOException Error");
                }
            }
        }

        void Timeline_NeedleMoved(object sender)
        {
            var timeline = (Timeline)sender;
            Seek(timeline.Position, timeline);
        }

        void OnSelectionChanged() {
            Debug.WriteLine("Selection changed");

            Title = GetTitle(project, listView.SelectedIndex);
        }

        void Timeline_SelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            listView.SelectionChanged -= ListView_SelectionChanged;

            foreach (Subtitle subtitle in listView.SelectedItems) {
                subtitle.TbxEnabled = false;
            }
            listView.SelectedItems.Clear();

            foreach (Subtitle subtitle in timeline.SelectedItems) {
                listView.SelectedItems.Add(subtitle);
                subtitle.TbxEnabled = true;
                listView.ScrollIntoView(subtitle);
            }

            OnSelectionChanged();

            listView.SelectionChanged += ListView_SelectionChanged;
        }

        void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var addedItems = e.AddedItems;
            var removedItems = e.RemovedItems;

            timeline.SelectedItems.CollectionChanged -= Timeline_SelectedItems_CollectionChanged;

            foreach (Subtitle subtitle in removedItems) {
                subtitle.TbxEnabled = false;
                timeline.SelectedItems.Remove(subtitle);
            }

            foreach (Subtitle subtitle in addedItems) {
                subtitle.TbxEnabled = true;
                timeline.SelectedItems.Add(subtitle);
            }

            OnSelectionChanged();

            timeline.SelectedItems.CollectionChanged += Timeline_SelectedItems_CollectionChanged;
        }

        void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is Subtitle subtitle) {
                //player.Position = subtitle.Sub.Start;
                Seek(subtitle.Start);
                timeline.RevealNeedle();
            }
        }
        #endregion


        #region TextBox Events
        void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (e.Key == Key.F4) {
                Action_ShiftLineBreak(textBox);
                e.Handled = true;
            }

            if (e.Key == Key.I && (Keyboard.Modifiers == ModifierKeys.Control)) {
                Action_MakeSelectionItalic(textBox);
            }
        }

        void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            activeTextBox = textBox;
            Debug.WriteLine(textBox.CaretIndex);
        }

        void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var subtitle = (Subtitle)textBox.DataContext;
            subtitle.Text = textBox.Text;
        }

        void TextBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
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
        void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (player.IsPlaying)
                player.Pause();
            else player.Resume();
        }

        void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player.IsMediaLoaded) {
                double seconds = slider.Value / 100 * player.Duration.TotalSeconds;
                Seek(TimeSpan.FromSeconds(seconds), sender);
            }
        }

        void Player_PositionChanged(object sender, MpvPlayerPositionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                if (player.IsPlaying) {
                    Seek(e.NewPosition, player);
                }
            }));
        }

        void Player_MediaFinished(object sender, EventArgs e) {
            player.Pause();
            player.Position = player.Duration;
        }

        void Player_MediaLoaded(object sender, EventArgs e) {
            Dispatcher.BeginInvoke(new Action(() => {
                var position = timeline.Position;

                if (position > player.Duration) player.Position = player.Duration;
                else player.Position = position;
                //Seek(timeline.Position, timeline);

                if (player.Duration.TotalSeconds != 0) {
                    slider.Value = player.Position.TotalSeconds / player.Duration.TotalSeconds * 100;
                }
            }));
            
        }
        #endregion


        #region Menu Events
        void MenuProjectNew_Click(object sender, RoutedEventArgs e)
        {
            CloseProject();
        }

        void MenuProjectOpen_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog {
                Filter = PROJ_FILTER
            };
            if (dialog.ShowDialog() == true && CloseProject()) {
                Settings.Save(settings);
                project = OpenProject(dialog.FileName);
            }
        }

        void MenuProjectSave_Click(object sender, RoutedEventArgs e)
        {
            SaveOrSaveAs(project, settings);

            Title = GetTitle(project, listView.SelectedIndex);
        }

        void MenuProjectSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAs(project, settings);

            Title = GetTitle(project, listView.SelectedIndex);
        }

        void MenuProjectClose_Click(object sender, RoutedEventArgs e)
        {
            CloseProject();
        }

        void MenuVideoOpen_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog {
                Filter = VIDEO_FILTER
            };
            if (dialog.ShowDialog() == true) {
                player.Load(dialog.FileName);          
                project.VideoPath = dialog.FileName;
                btnPlay.IsEnabled = true;
                slider.IsEnabled = true;
            }
        }

        void MenuVideoClose_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();
            player.PlaylistClear();
            project.VideoPath = null;
            btnPlay.IsEnabled = false;
            slider.IsEnabled = false;
        }

        void MenuSrtImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog {
                Filter = SRT_FILTER
            };
            if (dialog.ShowDialog() == true) {
                project.Subtitles = Srt.Read(dialog.FileName);
                project.TrackName = dialog.SafeFileName;
                LoadSubtitles(project, timeline);
            }
        }

        void MenuSrtRefImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog {
                Filter = SRT_FILTER
            };
            if (dialog.ShowDialog() == true) {
                project.RefSubtitles = Srt.Read(dialog.FileName);
                project.RefTrackName = dialog.SafeFileName;
                LoadRefSubtitles(project, timeline);
            }
        }

        void MenuSrtExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog {
                AddExtension = true,
                DefaultExt = "srt",
                Filter = SRT_FILTER
            };
            if (dialog.ShowDialog() == true) {
                Srt.Write(dialog.FileName, project.Subtitles);
            }
        }

        void MenuEditInsert_Click(object sender, RoutedEventArgs e)
        {
            Action_InsertNewSubtitle();
        }

        void MenuEditTrimEnd_Click(object sender, RoutedEventArgs e)
        {
            Action_TrimEnd();
        }

        void MenuEditShiftLineBreak_Click(object sender, RoutedEventArgs e)
        {
            Action_ShiftLineBreak(activeTextBox);
        }

        void MenuOptionsRipple_Click(object sender, RoutedEventArgs e) {
            //IsChecked reports value after click
            //MessageBox.Show($"checked: {menuOptionsRipple.IsChecked}");
            timeline.Ripple = menuOptionsRipple.IsChecked;
        }
        #endregion

        #region Window Events
        void Window_Loaded(object sender, RoutedEventArgs e) {
            wfHost.LayoutUpdated += WfHost_LayoutUpdated;
            LocationChanged += Window_LocationChanged;
            airWindow.Owner = this;
            airWindow.Show();
        }

        void Window_LocationChanged(object sender, EventArgs e) {
            UpdateOverlay();
        }

        void SaveAs(Project project, SettingsStorage settings) {
            var dialog = new SaveFileDialog {
                AddExtension = true,
                DefaultExt = PROJ_EXT,
                Filter = PROJ_FILTER,
                FileName = Path.GetFileNameWithoutExtension(project.FileName),
            };
            if (dialog.ShowDialog() == true) {
                project.Write();
                settings.LastProject = dialog.FileName;                
            }
        }

        void Save(Project project, SettingsStorage settings) {
            project.SelIndex = listView.SelectedIndex;
            project.VideoPos = player.Position.TotalSeconds;
            project.ScrollPos = timeline.HorizontalOffset;

            project.Write();
            settings.LastProject = project.FileName;            
        }

        void SaveOrSaveAs(Project project, SettingsStorage settings) {
            if (string.IsNullOrEmpty(project.FileName)) {                
                SaveAs(project, settings);
            }
            else {
                Save(project, settings);
            }
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            if (project.UnsavedChanges) {
                var projectFilename = Path.GetFileName(project.FileName);
                var result = Dialogs.SaveChanges(projectFilename);
                if (result == MessageBoxResult.Cancel) {
                    e.Cancel = true;
                    return;
                }                
                else if (result ==  MessageBoxResult.Yes) {
                    SaveOrSaveAs(project, settings);
                }
            }

            if (WindowState == WindowState.Maximized) {
                settings.Maximized = true;
            }
            settings.SafelyExited = true;
            Settings.Save(settings);

            //????
            if (string.IsNullOrEmpty(project.FileName)) MessageBox.Show("empty project.filename");
            else {
                var tempFile = project.FileName + ".temp";
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

            if (e.Key == Key.O && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                var dialog = new OpenFileDialog {
                    Filter = PROJ_FILTER
                };
                if (dialog.ShowDialog() == true && CloseProject()) {
                    Settings.Save(settings);
                    project = OpenProject(dialog.FileName);
                }
            }
            else if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                SaveOrSaveAs(project, settings);

                Title = GetTitle(project, listView.SelectedIndex);
            }
        }

        #endregion

        #region ACTIONS
        void Action_PlayPause()
        {
            if (player.IsPlaying) player.Pause();
            else player.Resume();
        }

        /*void Action_ShiftLineBreak(TextBox textBox)
        {
            int caretIndex = textBox.CaretIndex;
            if (caretIndex == 0)
                return;
            textBox.Text = ShiftLineBreak(textBox.Text, caretIndex);
        }*/
        void Action_ShiftLineBreak(TextBox textBox)
        {
            int caretIndex = textBox.CaretIndex;
            string str = RemoveNewlines(textBox.Text);
            str = str.Insert(caretIndex, Environment.NewLine);
            str = TrimSpaces(str);
            textBox.Text = str;
            textBox.CaretIndex = caretIndex + 1;
        }

        void Action_MakeSelectionItalic(TextBox textBox)
        {
            int start = textBox.SelectionStart;
            int length = textBox.SelectionLength;

            string newText = textBox.Text.Insert(start, "<i>");
            newText = newText.Insert(start + length + 3, "</i>");

            textBox.Text = newText;
            textBox.SelectionStart = start;
            textBox.SelectionLength = length + 7;
        }

        void Action_TrimEnd(Subtitle subtitle)
        {
            if (subtitle != null) {
                TimeSpan dur, pos, start;
                pos = timeline.Position;
                start = subtitle.Start;
                dur = pos - start;
                if (dur < TimeSpan.FromSeconds(1.2)) {
                    MessageBox.Show("too short, correcting...");
                    subtitle.End = subtitle.Start + TimeSpan.FromSeconds(1.2);
                }
                else {
                    subtitle.End = timeline.Position;
                }
            }
        }

        void Action_TrimEnd()
        {
            var underNeedle = GetItemUnderNeedle(timeline.Position, project.Subtitles);
            Action_TrimEnd(underNeedle);
        }

        void Action_InsertNewSubtitle()
        {
            Subtitle beforeNeedle = FindSubtitleBefore(timeline.Position, project.Subtitles);
            var sub = new Subtitle() {
                Start = timeline.Position,
                End = timeline.Position + TimeSpan.FromSeconds(1.5),
                Text = string.Empty
            };            

            if (beforeNeedle == null) {
                project.Subtitles.Insert(0, sub);
            }
            else {
                project.Subtitles.Insert(beforeNeedle.Index, sub);
            }

            RecalculateIndexes(project.Subtitles);

            timeline.SelectedItems.Add(sub);

            listView.SelectedItems.Clear();
            listView.SelectedItems.Add(sub);
        }
        #endregion


        #region Context Menu Events
        void MenuItemMerge_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItems.Count <= 1) return;

            Merge(listView.SelectedItems, project.Subtitles);
        }

        void MenuItemMergeDialog_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItems.Count <= 1) return;

            Merge(listView.SelectedItems, project.Subtitles, true);
        }

        void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var copy = new List<Subtitle>();
            foreach (Subtitle subtitle in listView.SelectedItems) copy.Add(subtitle);

            foreach (Subtitle subtitle in copy) {
                project.Subtitles.Remove(subtitle);
            }
            RecalculateIndexes(project.Subtitles);
        }

        void MenuItemInsert_Click(object sender, RoutedEventArgs e) {
            Action_InsertNewSubtitle();
        }

        void PrepareContextMenu(Subtitle subtitle, ContextMenu contextMenu, IList selectedItems)
        {
            var merge = (MenuItem)contextMenu.Items[0];
            var mergeDialog = (MenuItem)contextMenu.Items[1];
            var delete = (MenuItem)contextMenu.Items[2];
            merge.IsEnabled = false;
            mergeDialog.IsEnabled = false;
            delete.IsEnabled = false;
            if (!selectedItems.Contains(subtitle))
                return;
            int count = selectedItems.Count;
            if (count >= 1)
                delete.IsEnabled = true;
            if (count >= 2) {
                merge.IsEnabled = true;
                mergeDialog.IsEnabled = true;
            }
        }

        void ListViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var lvItem = (ListViewItem)sender;
            var subtitle = (Subtitle)lvItem.DataContext;
            ContextMenu contextMenu = lvItem.ContextMenu;
            PrepareContextMenu(subtitle, contextMenu, listView.SelectedItems);
        }

        void Track_ChunkContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Debug.WriteLine("Chunk_ContextMenuOpening");
            var chunk = (Chunk)sender;
            var subtitle = (Subtitle)chunk.DataContext;
            ContextMenu contextMenu = chunk.ContextMenu;
            PrepareContextMenu(subtitle, contextMenu, listView.SelectedItems);
        }
        #endregion

    }
}
