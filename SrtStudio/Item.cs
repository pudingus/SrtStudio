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

        private int _index;
        public int Index {
            get { return _index; }
            set {
                _index = value;
                RaisePropertyChanged("Index");
            }
        }
        private TimeSpan _start;
        public TimeSpan Start {
            get { return _start; }
            set {
                _start = value;
                RaisePropertyChanged("Start");
                UpdateDur();
            }
        }

        private TimeSpan _end;
        public TimeSpan End {
            get => _end;
            set {
                _end = value;
                RaisePropertyChanged("End");
                UpdateDur();
            }
        }


        private TimeSpan _dur;
        public TimeSpan Dur { get => _dur; }

        private void UpdateDur() {
            TimeSpan dur = _end - _start;
            if (dur.CompareTo(_dur) != 0) { //not the same
                _dur = dur;
                RaisePropertyChanged("Dur");
                UpdateCps();
            }
        }


        private double _cps;
        public double CPS { get => _cps; }

        private void UpdateCps() {
            if (_text != null) {
                _cps = _text.Length / _dur.TotalSeconds;
                RaisePropertyChanged("CPS");
            }
        }


        private string _text;
        public string Text {
            get => _text;
            set {
                _text = value;
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



        public Chunk Chunk { get; set; }
        public Subtitle Sub { get; set; }
    }
}
