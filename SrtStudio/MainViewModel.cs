using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SrtStudio
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        ObservableCollection<Subtitle> items;
        public ObservableCollection<Subtitle>  Items {
            get => items;
            set => SetProperty(ref items, value);
        }

        //Subtitle activeItem = new Subtitle() { Text = "Umírám hlady." };
        Subtitle activeItem;
        public Subtitle ActiveItem {
            get => activeItem;
            set => SetProperty(ref activeItem, value);
        }


        //from intellitect.com
        void SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            if (!EqualityComparer<T>.Default.Equals(field, newValue)) {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));                
            }
        }
    }
}
