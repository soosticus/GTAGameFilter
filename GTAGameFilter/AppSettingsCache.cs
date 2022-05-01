using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace GTAGameFilter
{
    internal class AppSettingsCache : INotifyPropertyChanged
    {
        private AppSettingsCache()
        { }
        
        // Singleton object
        private static AppSettingsCache? _instance;

        public static AppSettingsCache Instace()
        {
            if (_instance != null)
                return _instance;

            try
            {
                string? directory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                using (FileStream fs = File.Create(Path.Combine(directory, "GTAGameFilter", "settings.json")))
                {
                    _instance = JsonSerializer.Deserialize<AppSettingsCache>(fs);
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }

            if (_instance == null)
                _instance = new AppSettingsCache();
            return _instance;
        }

        public void Save()
        {

            try
            {
                string? directory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                using (FileStream fs = File.Create(Path.Combine(directory, "GTAGameFilter", "settings.json")))
                {
                    JsonSerializer.Serialize(fs, this);
                }
            }
            catch (Exception ex)
            {
                Debug.Write("Failed to save: {0}", ex == null?"Unknown Error":ex.Message);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void propertyChanged(string property)
        {
            Save();
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private bool _push_to_friends = false;
        public bool ShouldPushToFriends
        {
            get { return _push_to_friends; }
            set
            {
                _push_to_friends = value;
                propertyChanged("ShouldPushToFriends");
            }
        }

        private bool _accept_from_friends = false;
        public bool ShouldAcceptFromFriends
        {
            get { return _accept_from_friends; }
            set
            {
                _accept_from_friends = value;
                propertyChanged("ShouldAcceptFromFriends");
            }
        }

        public ObservableConcurrentDictionary<string, IpListing> FriendsList = new ObservableConcurrentDictionary<string, IpListing>();
    }
}
