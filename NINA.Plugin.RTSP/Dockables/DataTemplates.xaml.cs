using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
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
                if (elem.DataContext is RTSPVM vm) {
                    vm.SetPassword(elem.SecurePassword);
                }
            }
        }

        private void PasswordBox_Loaded(object sender, RoutedEventArgs e) {            
            if (sender is PasswordBox elem) {
                if (elem.DataContext is RTSPVM vm) {
                    elem.Password = vm.Password;
                }
            }
        }

        private void Media_MediaInitializing(object sender, Unosquare.FFME.Common.MediaInitializingEventArgs e) {
            if(sender is Unosquare.FFME.MediaElement elem) {
                elem.RendererOptions.VideoImageType = Unosquare.FFME.Common.VideoRendererImageType.InteropBitmap;
                
                if (e.MediaSource.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase)) { 
                    e.Configuration.PrivateOptions["rtsp_transport"] = "tcp";
                    e.Configuration.GlobalOptions.FlagNoBuffer = true;
                    e.Configuration.GlobalOptions.EnableReducedBuffering = true;
                    e.Configuration.ReadTimeout = TimeSpan.FromSeconds(30);
                    e.Configuration.PrivateOptions["user_agent"] = CoreUtil.UserAgent;
                }
                if (e.MediaSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     e.MediaSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                    e.Configuration.PrivateOptions["user_agent"] = CoreUtil.UserAgent;
                }
            }
        }

        private void Media_MediaFailed(object sender, Unosquare.FFME.Common.MediaFailedEventArgs e) {
            Logger.Error(e.ErrorException);
            Notification.ShowError("Stream failed to load: " + e.ErrorException.Message);
        }

        private void Media_MediaOpening(object sender, Unosquare.FFME.Common.MediaOpeningEventArgs e) {
            e.Options.MinimumPlaybackBufferPercent = 0;
            e.Options.IsTimeSyncDisabled = true;
        }
    }
}
