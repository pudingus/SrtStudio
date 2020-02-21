using System.Windows.Controls;

namespace SrtStudio
{
    /// <summary>
    /// Interaction logic for TrackHeader.xaml
    /// </summary>
    public partial class TrackHeader : UserControl
    {
        public Track ParentTrack { get; set; }

        public TrackHeader()
        {
            InitializeComponent();
        }

        public TrackHeader(Track parent)
        {
            InitializeComponent();
            ParentTrack = parent;
        }
    }
}
