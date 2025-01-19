using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.CustomControlLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        private void DataGrid_CellGotFocus(object sender, RoutedEventArgs e) {
            // Lookup for the source to be DataGridCell
            if (e.OriginalSource.GetType() == typeof(DataGridCell)) {
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);

                Control control = GetFirstChildByType<Control>(e.OriginalSource as DataGridCell);
                if (control != null) {
                    if (control is HintTextBox htb) {
                        GetFirstChildByType<TextBox>(htb).Focus();
                    } else if (control is ComboBox cb) {
                        cb.Focus();
                        cb.IsDropDownOpen = true;
                    } else {
                        control.Focus();
                    }
                }
            }
        }

        private T GetFirstChildByType<T>(DependencyObject prop) where T : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(prop); i++) {
                DependencyObject child = VisualTreeHelper.GetChild((prop), i) as DependencyObject;
                if (child == null)
                    continue;

                T castedProp = child as T;
                if (castedProp != null)
                    return castedProp;

                castedProp = GetFirstChildByType<T>(child);

                if (castedProp != null)
                    return castedProp;
            }
            return null;
        }
    }
}
