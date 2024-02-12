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
using Unosquare.FFME.Common;

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
                    e.Configuration.ReadTimeout = TimeSpan.FromSeconds(5);
                    e.Configuration.PrivateOptions["user_agent"] = CoreUtil.UserAgent;

                    e.Configuration.GlobalOptions.ProbeSize = 8192;
                    e.Configuration.GlobalOptions.MaxAnalyzeDuration = TimeSpan.FromSeconds(1);
                    e.Configuration.GlobalOptions.FlagDiscardCorrupt = true;
                    // AVIO Flags Direct (chewie#7309 from experimentation)
                    e.Configuration.GlobalOptions.FlagSortDts = true;
                }
                if (e.MediaSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     e.MediaSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                    e.Configuration.PrivateOptions["user_agent"] = CoreUtil.UserAgent;
                }
            }
        }

        private void Media_MediaFailed(object sender, Unosquare.FFME.Common.MediaFailedEventArgs e) {
            var reg = new Regex("(?<=\\//)(.*?)(?=\\@)");
            var match = reg.Match(e.ErrorException.Message);

            var error = e.ErrorException.Message;
            
            if(match.Success) {
                error = error.Replace(match.Value, "xxx:xxx");
            }

            Logger.Error(error);
            Notification.ShowError("Stream failed to load: " + error);
        }

        private void Media_MediaOpening(object sender, Unosquare.FFME.Common.MediaOpeningEventArgs e) {
            e.Options.MinimumPlaybackBufferPercent = 0;
            e.Options.IsTimeSyncDisabled = false;
            e.Options.DecoderParams.EnableFastDecoding = true;
            e.Options.DecoderParams.EnableLowDelayDecoding = true;
        }
    }
}
