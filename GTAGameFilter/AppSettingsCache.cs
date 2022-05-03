using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTAGameFilter
{
    internal class AppSettingsCache : INotifyPropertyChanged
    {
        static private string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "GTAGameFilter");
        static private string _fileName = "settings.json";
        [JsonIgnore]
        public ObservableConcurrentDictionary<string, IpListing> FriendsListCollection = new ObservableConcurrentDictionary<string, IpListing>();
        [JsonInclude, JsonPropertyName("FriendsList")]
        public List<IpListing> _friendsList = new List<IpListing>();
        public AppSettingsCache()
        {
            Directory.CreateDirectory(_directory);
        }

        private void FriendsList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (IpListing friend in e.NewItems)
                        if (!_friendsList.Any(a => a.IpAddress.CompareTo(friend.IpAddress) == 0))
                        {
                            _friendsList.Add(friend);
                        }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (IpListing friend in e.OldItems)
                        _friendsList.RemoveAll(a => a.IpAddress.CompareTo(friend.IpAddress) == 0);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    _friendsList = new List<IpListing>(((ObservableConcurrentDictionary<string,IpListing>)sender).Values);
                    break;
            }
            Save();
        }

        // Singleton object
        private static AppSettingsCache? _instance;

        public static AppSettingsCache Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                try
                {
                    using (FileStream fs = File.Open(Path.Combine(_directory, _fileName), FileMode.Open))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            fs.CopyTo(ms);
                            var buffer = ms.GetBuffer();
                            var data = new sbyte[buffer.Length];
                            Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
                            unsafe
                            {
                                fixed (sbyte* p = data)
                                {
                                    string jsonString = new string(p, 0, (int)ms.Length, Encoding.UTF8);

                                    _instance = JsonSerializer.Deserialize<AppSettingsCache>(jsonString, new JsonSerializerOptions()
                                    {
                                        ReadCommentHandling = JsonCommentHandling.Skip
                                    });
                                }
                            }
                            if (_instance != null)
                            {
                                foreach (IpListing friend in _instance._friendsList)
                                {
                                    friend.IsFriend = true;
                                    _instance.FriendsListCollection.Add(friend.IpAddress, friend);
                                }
                                _instance.FriendsListCollection.CollectionChanged += _instance.FriendsList_CollectionChanged;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                }

                if (_instance == null)
                {
                    _instance = new AppSettingsCache();
                    _instance.FriendsListCollection.CollectionChanged += _instance.FriendsList_CollectionChanged;
                }
                return _instance;
            }
        }

        public void UpdateFriendListFromJson(string jsonString)
        {
            List<IpListing> newList = JsonSerializer.Deserialize<List<IpListing>>(jsonString);
            foreach (var key in FriendsListCollection.Keys)
            {
                FriendsListCollection.Remove(key);
            }
            foreach (var friend in newList)
            {
                friend.IsFriend = true;
                _friendsList.Add(friend);   
            }
        }

        public string GetFriendsListJson()
        {
            return JsonSerializer.Serialize(_friendsList);
        }

        public void Save()
        {

            try
            {
                using (FileStream fs = File.Create(Path.Combine(_directory, _fileName)))
                {
                    fs.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })));
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
    }
}
