using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProcessWatcher
{
    public class BannApp : INotifyPropertyChanged
    {

        string? appl;
        public int Id { get; set; }
        public string? Appl
        {
            get { return appl; }
            set
            {
                appl = value;
                OnPropertyChanged("Appl");
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
