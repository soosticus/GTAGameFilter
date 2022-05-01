using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GTAGameFilter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PacketSniffer sniffer;
        ObservableConcurrentDictionary<string, IpListing> session_ips = new ObservableConcurrentDictionary<string, IpListing>();
        ObservableCollection<Packet> packets = new ObservableCollection<Packet>();
        Thread ipSeenUpdateThread;
        bool _running = false;
        public MainWindow()
        {
            InitializeComponent();
            sniffer = new PacketSniffer();
            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
            session_ip_view.ItemsSource = session_ips;
            friend_list_view.ItemsSource = session_ips;
            ipSeenUpdateThread = new Thread(async () => {
                while (_running)
                {
                    try
                    {
                        await Task.Delay(1 * 1000);
                        foreach (var kvp in session_ips)
                        {
                            await Dispatcher.BeginInvoke(() => kvp.Value.RefreshActiveStatus());
                        }
                    }
                    catch (TaskCanceledException e) { }
                }
            });
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _running = false;
            sniffer.OnPacketRecieved -= Sniffer_OnPacketRecieved;
            sniffer.Stop();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _running = true;
            sniffer.OnPacketRecieved += Sniffer_OnPacketRecieved;
            sniffer.Start();
            ipSeenUpdateThread.Start();
            GetLocalIpAddress();
            GetPublicIpAddress();
        }

        private void AddSessionIp(string ipAddress)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!session_ips.ContainsKey(ipAddress))
                {
                    session_ips.Add(ipAddress, new IpListing(ipAddress));
                }
                session_ips[ipAddress].LastSeen = DateTimeOffset.Now;
            });
        }

        private void AddSessionIp(IpListing listing)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!session_ips.ContainsKey(listing.IpAddress))
                {
                    session_ips.Add(listing.IpAddress, listing);
                }
                else
                {
                    session_ips[listing.IpAddress].Update(listing);
                }
            });
        }

        private async void Sniffer_OnPacketRecieved(object sender, PacketRecievedEventArgs e)
        {
            e.Packet.IsAllowed = true;
            packets.Add(e.Packet);
            AddSessionIp(e.Packet.SrcAddress);
            AddSessionIp(e.Packet.DestAddress);
        }
        private void GetLocalIpAddress()
        {
            new Thread(() =>
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                    Dispatcher.BeginInvoke(() =>
                    {
                        local_ip_view.Text = endPoint?.Address.ToString();
                    });
                }
            }).Start();
        }
        private void GetPublicIpAddress()
        {
            new Thread(() =>
            {
                String address = "";
                WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
                using (WebResponse response = request.GetResponse())
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    address = stream.ReadToEnd();
                }

                int first = address.IndexOf("Address: ") + 9;
                int last = address.LastIndexOf("</body>");
                address = address.Substring(first, last - first);

                Dispatcher.BeginInvoke(() =>
                {
                    public_ip_view.Text = address;
                });
            }).Start();
        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastHeaderDirection = ListSortDirection.Ascending;
        private void session_ip_view_header_Click(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastHeaderDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort((ListView)e.Source, sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastHeaderDirection = direction;
                }
            }
        }
        private void Sort(ListView lv, string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView =
              CollectionViewSource.GetDefaultView(
                  lv.ItemsSource is ObservableConcurrentDictionary<string, IpListing> 
                    ? ((ObservableConcurrentDictionary<string, IpListing>)lv.ItemsSource).Values 
                    : lv.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void add_friend_from_session_button_Click(object sender, RoutedEventArgs e)
        {
            if (session_ip_view.SelectedItem is null)
                return;

            var ipListing = ((KeyValuePair<string, IpListing>)session_ip_view.SelectedItem).Value;

            FriendListPromptDialog result = FriendListPromptDialog.Prompt(this, ipListing.UserName);
            if (result is null)
                return;
            
            ipListing.UserName = result.UsernameResponse;
            ipListing.IsFriend = true;

            Debug.WriteLine(string.Format("Added friend: {0}", ipListing.Serialize()));
        }

        private void add_friend_button_Click(object sender, RoutedEventArgs e)
        {
            FriendListPromptDialog result = FriendListPromptDialog.Prompt(this, FriendListPromptDialog.DialogType.UserNameIpAddress);
            if (result is null)
                return;
            var ipListing = new IpListing(result.IpAddressResponse)
            {
                UserName = result.UsernameResponse,
                IsFriend = true
            };
            AddSessionIp(ipListing);

            Debug.WriteLine(string.Format("Added friend: {0}", ipListing.Serialize()));
        }

        private void remove_friend_button_Click(object sender, RoutedEventArgs e)
        {
            if (friend_list_view.SelectedItem is null)
                return;

            var ipListing = ((KeyValuePair<string, IpListing>)session_ip_view.SelectedItem).Value;
            ipListing.IsFriend = false;

            Debug.WriteLine(string.Format("Removed friend: {0}", ipListing.Serialize()));
        }
    }
}
