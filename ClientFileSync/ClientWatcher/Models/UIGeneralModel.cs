using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientWatcher.Models
{
    public class UIGeneralModel:INotifyPropertyChanged
    {
        string signalRStatus;
        public string SignalRStatus { get => signalRStatus; set { signalRStatus = value; RaisePropertyChanged("SignalRStatus"); } }

        List<FileEvent> fileEvents = new List<FileEvent>();
        public List<FileEvent> FileEvents
        {
            get => fileEvents;
            set
            {
                fileEvents = value;
                RaisePropertyChanged("FileEvents");
            }
}

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
