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
            get => _index;
            set {
                if (_index != value) {
                    _index = value;
                    RaisePropertyChanged(nameof(Index));
                }
            }
        }
        public TimeSpan Start {
            get => Sub.Start;
            set {
                if (Sub.Start != value) {
                    Sub.Start = value;
                    RaisePropertyChanged(nameof(Start));
                    UpdateDur();
                }
            }
        }

        public TimeSpan End {
            get => Sub.End;
            set {
                if (Sub.End != value) {
                    Sub.End = value;
                    RaisePropertyChanged(nameof(End));
                    UpdateDur();
                }
            }
        }


        private TimeSpan _dur;
        public TimeSpan Dur { get => _dur; }

        private void UpdateDur() {
            TimeSpan dur = End - Start;
            if (dur !=_dur) {
                _dur = dur;
                RaisePropertyChanged(nameof(Dur));
                UpdateCps();
            }
        }


        private double _cps;
        public double CPS { get => _cps; }

        private void UpdateCps() {
            if (Text != null) {
                _cps = Text.Length / _dur.TotalSeconds;
                RaisePropertyChanged(nameof(CPS));
            }
        }


        public string Text {
            get => Sub.Text;
            set {
                if (Sub.Text != value) {
                    Sub.Text = value;
                    RaisePropertyChanged(nameof(Text));
                    UpdateCps();
                }
            }
        }

        private bool _enabled;
        public bool Enabled {
            get => _enabled;
            set {
                if (_enabled != value) {
                    _enabled = value;
                    RaisePropertyChanged(nameof(Enabled));
                }
            }
        }

        private Thickness _borderThickness;
        public Thickness BorderThickness {
            get => _borderThickness;
            set {
                if (_borderThickness != value) {
                    _borderThickness = value;
                    RaisePropertyChanged(nameof(BorderThickness));
                }
            }
        }

        private bool _selected;
        public bool Selected {
            get => _selected;
            set {
                if (_selected != value) {

                    _selected = value;
                    RaisePropertyChanged(nameof(Selected));
                }
            }
        }

        public Chunk Chunk { get; set; }
    }
}
