using FFmpeg.AutoGen;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using NINA.Plugin.RTSP.Dockables;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;

namespace NINA.Plugin.RTSP {

    [Export(typeof(IPluginManifest))]
    public class RTSPPlugin : PluginBase {
        public static RTSPPluginMediator Mediator { get; } = new RTSPPluginMediator();

        [ImportingConstructor]
        public RTSPPlugin() {
            if (DllLoader.IsX86()) {
                throw new Exception("This plugin is not available for x86 version of N.I.N.A.");
            }
            Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll"); 
            Unosquare.FFME.Library.FFmpegLoadModeFlags = FFmpegLoadMode.FullFeatures;
            Unosquare.FFME.Library.LoadFFmpeg();
            Unosquare.FFME.Library.EnableWpfMultiThreadedVideo = true;
            Mediator.RegisterPlugin(this);
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