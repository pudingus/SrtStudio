using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private string _number;
        public string Number {
            get { return _number; }
            set {
                _number = value;
                RaisePropertyChanged("Number");
            }
        }
        private string _start;
        public string Start {
            get { return _start; }
            set {
                _start = value;
                RaisePropertyChanged("Start");
            }
        }
        private string _dur;
        public string Dur {
            get { return _dur; }
            set {
                _dur = value;
                RaisePropertyChanged("Dur");
            }
        }
        private string _text;
        public string Text {
            get { return _text; }
            set {
                _text = value;
                RaisePropertyChanged("Text");
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

        public Chunk Chunk { get; set; }
    }
}
