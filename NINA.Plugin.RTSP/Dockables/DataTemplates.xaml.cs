using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Plugin.RTSP.Dockables {
    [Export(typeof(ResourceDictionary))]
    partial class DataTemplates : ResourceDictionary {
        public DataTemplates() {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (sender is PasswordBox elem) {
                if (elem.DataContext is RTSPSource source) {
                    source.SetPassword(elem.SecurePassword);
                }
            }
        }

        private void PasswordBox_Loaded(object sender, RoutedEventArgs e) {            
            if (sender is PasswordBox elem) {
                if (elem.DataContext is RTSPSource source) {
                    elem.Password = Decrypt(source.Password);
                }
            }
        }
        private string Decrypt(string encrypt) {
            try {
                return DataProtection.Unprotect(Convert.FromBase64String(encrypt));
            } catch (Exception) {
                return "";
            }
        }
    }
}
