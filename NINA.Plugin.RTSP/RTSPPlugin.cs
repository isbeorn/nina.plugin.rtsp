using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using NINA.Plugin.RTSP.Dockables;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NINA.Plugin.RTSP {

    [Export(typeof(IPluginManifest))]
    public class RTSPPlugin : PluginBase, INotifyPropertyChanged {
        public static RTSPPluginMediator Mediator { get; } = new RTSPPluginMediator();
        public PluginOptionsAccessor PluginSettings { get; }

        [ImportingConstructor]
        public RTSPPlugin(IProfileService profileService) {
            if (DllLoader.IsX86()) {
                throw new Exception("This plugin is not available for x86 version of N.I.N.A.");
            }
            Mediator.RegisterPlugin(this);
            PluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
        }

        public bool UseRtspTcp {
            get {
                return PluginSettings.GetValueBoolean(nameof(UseRtspTcp), false);
            } 
            set {
                PluginSettings.SetValueBoolean(nameof(UseRtspTcp), value);
                RaisePropertyChanged();
            }
        }

        public ushort CachingMs {
            get {
                return PluginSettings.GetValueUInt16(nameof(CachingMs), 1000);
            }
            set {
                if(value < 0) { value = 0; }
                PluginSettings.SetValueUInt16(nameof(CachingMs), value);
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RTSPPluginMediator {
        public RTSPPlugin Plugin { get; private set; }
        public RTSPVM Player { get; private set; }

        public void RegisterPlugin(RTSPPlugin plugin) {
            this.Plugin = plugin;
        }

        public void RegisterRTSPPlayer(RTSPVM player) {
            this.Player = player;
        }


    }
}