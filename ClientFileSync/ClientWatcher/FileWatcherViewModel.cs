using ClientWatcher.Models;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace ClientWatcher
{
    public class FileWatcherViewModel:INotifyPropertyChanged
    {
        private string _syncFolder = ConfigurationManager.AppSettings["SyncFolder"].ToString();
        private string _webApiURLtoLoad = ConfigurationManager.AppSettings["WepApiURLtoLoad"].ToString();
        private string _wepApiURLtoDownload = ConfigurationManager.AppSettings["WepApiURLtoDownload"].ToString();
        private string _wepApiURLExistsFile = ConfigurationManager.AppSettings["WepApiURLExistsFile"].ToString();
        private string _webApiURLtoDelete = ConfigurationManager.AppSettings["WepApiURLtoDelete"].ToString();
        private string _singalRHost = ConfigurationManager.AppSettings["SignalRHost"].ToString();
        FileSystemWatcher _watcher;

        public System.Threading.Thread Thread { get; set; }
        public IHubProxy Proxy { get; set; }
        public HubConnection Connection { get; set; }
        public bool Active { get; set; }

        UIGeneralModel model;
        public UIGeneralModel Model
        {
            get => model;
            set
            {
                model = value;
                RaisePropertyChanged("Model");
            }
        }
        

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public FileWatcherViewModel()
        {
            Model = new UIGeneralModel();

            InitializeWatcher();

            StartHub();
        }

        private void StartHub()
        {
            Active = true;
            Thread = new System.Threading.Thread(() =>
            {
                Connection = new HubConnection(_singalRHost);
                Connection.Error += Connection_Error;
                Connection.StateChanged += Connection_StateChanged;
                Proxy = Connection.CreateHubProxy("FileSyncHub");

                Proxy.On<string, string>("NewFileNotification", (fileName, CRC) => OnNewFileNotified(fileName, CRC));

                Connection.Start();

                while (Active)
                {
                    System.Threading.Thread.Sleep(10);
                }
            })
            { IsBackground = true };
            Thread.Start();
        }

        private void InitializeWatcher()
        {
            _watcher = new FileSystemWatcher();
            _watcher.Path = _syncFolder;
            //_watcher.NotifyFilter = NotifyFilters.Created;
            _watcher.Filter = "*.*";
            //_watcher.Changed += new FileSystemEventHandler(OnWatcherChanged);
            _watcher.Deleted += new FileSystemEventHandler(OnWatcherDeleted);
            _watcher.Created += new FileSystemEventHandler(OnWatcherCreated);
            _watcher.EnableRaisingEvents = true;
        }

        private void OnWatcherCreated(object source, FileSystemEventArgs e)
        {
            //if (!fileExistsOnServer(e.Name, "").Result)
            AddEventToCollection(e);
            Task.WaitAll(sendFile(e.Name));
        }

        private void AddEventToCollection(FileSystemEventArgs e)
        {
            var events = Model.FileEvents;
            events.Add(new FileEvent() { Date = DateTime.Now, EventDescription = e.ChangeType + ": " + e.FullPath, Filename = e.Name });
            Model.FileEvents = events;
        }

        private async Task sendFile(string fileName)
        {
            byte[] byteFile = tryToReadFile(_syncFolder + "\\" + fileName);
            HttpContent fileContent = new ByteArrayContent(byteFile);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.PostAsync(_webApiURLtoLoad, new MultipartFormDataContent()
                {
                    { new StringContent(fileName),"fileName"},
                    { fileContent, "file", fileName }
                });
            }
        }

        private byte[] tryToReadFile(string file, int tries = 0)
        {
            if (tries == 100)
                throw new Exception("Can't access file " + file);

            byte[] b;
            try
            {
                b = System.IO.File.ReadAllBytes(file);
            }
            catch (IOException)
            {
                Thread.Sleep(500);
                return tryToReadFile(file, ++tries);
            }

            return b;
        }

        private void OnWatcherDeleted(object source, FileSystemEventArgs e)
        {
            AddEventToCollection(e);
            Task.WaitAll(deleteFileOnServer(e.Name));
        }

        private async Task deleteFileOnServer(string fileName)
        {
            var lastDotIndex = fileName.LastIndexOf('.');
            var nameLength = lastDotIndex >= 0 ? lastDotIndex : fileName.Length;
            var extensionLength = fileName.Length - 1 - nameLength;
            var name = fileName.Substring(0, nameLength);

            string extension;
            if (lastDotIndex < fileName.Length - 1)
                extension = fileName.Substring(lastDotIndex + 1, extensionLength);
            else
                extension = "";
            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync(_webApiURLtoDelete + "/" + name + "/" + extension);
            }
        }

        private void OnNewFileNotified(string fileName, string CRC)
        {
            //Check if I have the File
            if (!alreadyHaveFile(fileName, CRC))
                //Download the New File
                getFile(fileName);
        }

        private bool alreadyHaveFile(string fileName, string CRC)
        {
            //Include CRC check ************************************************************************** TO DO
            return File.Exists(_syncFolder + "\\" + fileName);
        }

        private async void getFile(string fileName)
        {
            using (var client = new HttpClient())
            {
                var responseStream = await client.GetStreamAsync(_wepApiURLtoDownload + "?fileName=" + fileName);
                using (var fileWritter = File.Create(_syncFolder + "\\" + fileName))
                {
                    //responseStream.Seek(0, SeekOrigin.Begin);
                    responseStream.CopyTo(fileWritter);
                }
            }
        }

        private void Connection_StateChanged(StateChange obj)
        {
            model.SignalRStatus = "Status: " + obj.NewState;
        }

        private void Connection_Error(Exception obj)
        {
            model.SignalRStatus = "Connection error: " + obj.ToString();
        }

        private async Task<bool> fileExistsOnServer(string fileName, string CRC)
        {
            bool final;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(_wepApiURLExistsFile + "?fileName=" + fileName);
                var result = await response.Content.ReadAsStringAsync();
                final = JsonConvert.DeserializeObject<bool>(result);
            }

            return final;
        }
    }
}
