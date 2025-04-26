using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessWatcher
{
    public class BannWord : INotifyPropertyChanged
    {

        string? word;
        public int Id { get; set; }
        public string? Word
        {
            get { return word; }
            set
            {
                word = value;
                OnPropertyChanged("Word");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

}
