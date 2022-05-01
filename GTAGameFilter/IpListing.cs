using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTAGameFilter
{
    public class IpListing : INotifyPropertyChanged
    {
        public static uint ActiveThreshold = 3 * 60 * 1000; // 3 minutes
        public IpListing(string ipAddress)
        {
            LastSeen = DateTimeOffset.UnixEpoch;
            _userName = "Unknown";
            _ipAddress = ipAddress;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void propertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private string _userName;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                propertyChanged("UserName");
            }
        }
        private string _ipAddress;
        public string IpAddress
        {
            get
            {
                return _ipAddress;
            }
            set
            {
                _ipAddress = value;
                propertyChanged("IpAddress");
            }
        }
        private bool _isFriend;
        [JsonIgnore]
        public bool IsFriend
        {
            get
            {
                return _isFriend;
            }
            set
            {
                _isFriend = value;
                propertyChanged("IsFriend");
            }
        }
        private DateTimeOffset _lastSeen;
        [JsonIgnore]
        public DateTimeOffset LastSeen
        {
            get
            {
                return _lastSeen;
            }
            set
            {
                _lastSeen = value;
            }
        }
        [JsonIgnore]
        public bool IsActive
        {
            get
            {
                return (DateTimeOffset.Now - LastSeen).TotalMilliseconds < ActiveThreshold;
            }
        }

        public void Update(IpListing listing)
        {
            this.UserName = listing.UserName;
            this.IsFriend = listing.IsFriend;
            this.IpAddress = listing.IpAddress;
            this.LastSeen = listing.LastSeen;
        }

        public void RefreshActiveStatus()
        {
            propertyChanged("IsActive");
        }

        public string Serialize(JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(this, options);
        }

        public static IpListing? Deserialize(string jsonString)
        {
            return JsonSerializer.Deserialize<IpListing>(jsonString);
        }
    }

    public class SessionIpListingEqualityComparer : IEqualityComparer<IpListing>
    {
        public bool Equals(IpListing? x, IpListing? y)
        {
            if (x == null && y == null)
                return true;
            if (x == null)
                return false;
            if (y == null)
                return false;
            return x.IpAddress.CompareTo(y.IpAddress) == 0;
        }

        public int GetHashCode([DisallowNull] IpListing obj)
        {
            return obj.IpAddress.GetHashCode();
        }
    }
}
