using System;

namespace ClientWatcher.Models
{
    public struct FileEvent
    {
        DateTime date;
        public DateTime Date { get => date; set => date = value; }

        string filename;
        public string Filename { get => filename; set => filename = value; }

        string eventDescription;
        public string EventDescription { get => eventDescription; set => eventDescription = value; }

        public string ConcatenatedInfo {
            get => "jaja";//String.Join("||", Date.ToString(), EventDescription, Filename);
            
        }

        public override string ToString()
        {
            return String.Join(" || ", Date.ToString( "dd-MM-yyy HH:mm:ss"), EventDescription, Filename);
        }
    }
}