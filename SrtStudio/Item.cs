using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SrtStudio
{
    public class Item : INotifyPropertyChanged
    {
        /// PropertyChanged event handler
        public event PropertyChangedEventHandler PropertyChanged;

        /// Property changed Notification
        public void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Subtitle Sub { get; }

        public Item(Subtitle sub) {
            Sub = sub;
            UpdateDur();
            UpdateCps();
        }


        private int _index;
        public int Index {
            get { return _index; }
            set {
                _index = value;
                RaisePropertyChanged("Index");
            }
        }
        public TimeSpan Start {
            get => Sub.Start;
            set {
                Sub.Start = value;
                RaisePropertyChanged("Start");
                UpdateDur();
            }
        }

        public TimeSpan End {
            get => Sub.End;
            set {
                Sub.End = value;
                RaisePropertyChanged("End");
                UpdateDur();
            }
        }


        private TimeSpan _dur;
        public TimeSpan Dur { get => _dur; }

        private void UpdateDur() {
            TimeSpan dur = End - Start;
            if (dur !=_dur) {
                _dur = dur;
                RaisePropertyChanged("Dur");
                UpdateCps();
            }
        }


        private double _cps;
        public double CPS { get => _cps; }

        private void UpdateCps() {
            if (Text != null) {
                _cps = Text.Length / _dur.TotalSeconds;
                RaisePropertyChanged("CPS");
            }
        }


        public string Text {
            get => Sub.Text;
            set {
                Sub.Text = value;
                RaisePropertyChanged("Text");
                UpdateCps();
            }
        }

        private bool _enabled;
        public bool Enabled {
            get { return _enabled; }
            set {
                _enabled = value;
                RaisePropertyChanged("Enabled");
            }
        }

        private Thickness _borderThickness;
        public Thickness BorderThickness {
            get { return _borderThickness; }
            set {
                _borderThickness = value;
                RaisePropertyChanged("BorderThickness");
            }
        }

        private bool _selected;
        public bool Selected {
            get { return _selected; }
            set {
                _selected = value;
                RaisePropertyChanged("Selected");
            }
        }

        public Chunk Chunk { get; set; }
    }
}
