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


namespace SrtStudio {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private MpvPlayer player;


        public MainWindow() {

            InitializeComponent();

            player = new MpvPlayer(PlayerHost.Handle) {
                Loop = true,
                Volume = 50
            };
            //player.Load("http://techslides.com/demos/sample-videos/small.mp4");
            //player.Resume();

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

        const int scale = 36;   //one segment is 'scale' (30) seconds

        private void menuSrtImport_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true) {
                Srt srt = new Srt();
                srt.Read(dialog.FileName);
                int i = 1;
                foreach (Subtitle sub in srt.list) {
                    listBox.Items.Add("# " + i + " "+ sub.start + " " + sub.end + " | " + sub.text);
                    i++;
                }

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

                timeline.grid1.LayoutUpdated += (o, ea) => {
                    Console.WriteLine("layout updated");

                    double start = timeline.mLeft / 1000 * scale;

                    first.start = new TimeSpan(0, 0, 0, 0, (int)(start * 1000)+1);
                    textboxStart.Text = first.start.ToString();

                    double dur = timeline.width / 1000 * scale;
                    duration = new TimeSpan(0, 0, 0, 0, (int)(dur * 1000)+1);
                    textboxDur.Text = duration.ToString();


                    double end = start + dur;
                    first.end = new TimeSpan(0, 0, 0, 0, (int)(end * 1000)+1);
                    textboxEnd.Text = first.end.ToString();
                };

            }
        }

    }
}
