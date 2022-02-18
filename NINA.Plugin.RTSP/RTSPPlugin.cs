using FFmpeg.AutoGen;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;

namespace NINA.Plugin.RTSP {

    [Export(typeof(IPluginManifest))]
    public class RTSPPlugin : PluginBase {

        [ImportingConstructor]
        public RTSPPlugin() {
            if (DllLoader.IsX86()) {
                throw new Exception("This plugin is not available for x86 version of N.I.N.A.");
            }
            Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll"); 
            Unosquare.FFME.Library.FFmpegLoadModeFlags = FFmpegLoadMode.FullFeatures;
            Unosquare.FFME.Library.LoadFFmpeg();
            Unosquare.FFME.Library.EnableWpfMultiThreadedVideo = true;
        }
    }
}