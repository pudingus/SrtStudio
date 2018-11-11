using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrtStudio
{
    class Track
    {

        public string Name {
            get {
                return trackMeta.trackName.Text;
            }
            set {
                trackMeta.trackName.Text = value;
            }
        }

        private TrackMeta trackMeta;

        public Track()
        {
            trackMeta = new TrackMeta();
        }
    }
}
