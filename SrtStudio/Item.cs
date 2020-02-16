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

        public Item(Subtitle sub, int index) {
            Sub = sub;
            UpdateDur();
            UpdateCps();
            Index = index;
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


        private double cps;
        public double CPS { get => cps; }

        private void UpdateCps() {
            if (Text != null) {
                cps = Text.Length / _dur.TotalSeconds;
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

        
        private bool enabled;
        public bool Enabled {
            get => enabled;
            set {
                if (enabled != value) {
                    enabled = value;
                    RaisePropertyChanged(nameof(Enabled));
                }
            }
        }
        


        public Chunk Chunk { get; set; }
    }
}
