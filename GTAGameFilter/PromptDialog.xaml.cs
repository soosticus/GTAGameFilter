using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GTAGameFilter
{
    /// <summary>
    /// Interaction logic for PromptDialog.xaml
    /// </summary>
    public partial class FriendListPromptDialog : Window
    {
        public enum DialogType
        {
            UserName,
            UserNameIpAddress
        }


        public FriendListPromptDialog(string defaultUsername, DialogType type)
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(PromptDialog_Loaded);
            Title = "Add new Friend";
            description_view.Text = "Provide Friend information";
            username_view.Text = defaultUsername;
            ip_address_view.Text = "";
            if (type == DialogType.UserName)
                IpField.Visibility = Visibility.Collapsed;
        }

        void PromptDialog_Loaded(object sender, RoutedEventArgs e)
        {
            username_view.Focus();
        }

        public static FriendListPromptDialog Prompt(Window owner, string defaultUsername, DialogType type = DialogType.UserName)
        {
            FriendListPromptDialog inst = new FriendListPromptDialog(defaultUsername, type);
            inst.Owner = owner;
            inst.ShowDialog();
            if (inst.DialogResult == true)
                return inst;
            return null;
        }

        public static FriendListPromptDialog Prompt(Window owner, DialogType type = DialogType.UserName)
        {
            return Prompt(owner, "", type);
        }

        public string UsernameResponse
        {
            get
            {
                return username_view.Text;
            }
        }

        public string IpAddressResponse
        {
            get
            {
                return ip_address_view.Text;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
