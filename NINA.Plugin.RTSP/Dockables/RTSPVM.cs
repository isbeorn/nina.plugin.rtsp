using Accord.Statistics.Visualizations;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Protobuf.WellKnownTypes;
using LibVLCSharp.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.Plugin.RTSP.Dockables {

    [Export(typeof(IDockableVM))]
    public class RTSPVM : DockableVM {

        [ImportingConstructor]
        public RTSPVM(IProfileService profileService) : base(profileService) {
            this.Title = "RTSP Player";
            var dict = new ResourceDictionary();
            dict.Source = new Uri("NINA.Plugin.RTSP;component/Dockables/DataTemplates.xaml", UriKind.RelativeOrAbsolute);
            ImageGeometry = (System.Windows.Media.GeometryGroup)dict["NINA.Plugin.RTSP_CameraSVG"];
            ImageGeometry.Freeze();

            var assembly = this.GetType().Assembly;
            var id = assembly.GetCustomAttribute<GuidAttribute>().Value;
            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(id));

            SetTheGridCommand = new RelayCommand(SetTheGrid);
            StartStreamCommand = new AsyncCommand<bool>(StartStream);
            StopAllStreamsCommand = new RelayCommand(StopAllStreams);
            StopStreamCommand = new RelayCommand(StopStream);
            AddSourceCommand = new RelayCommand(AddSource);
            DeleteSourceCommand = new RelayCommand(DeleteSource);

            RTSPPlugin.Mediator.RegisterRTSPPlayer(this);

            ReadSources();
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            LibVLCSharp.Shared.Core.Initialize();
            _libVLC = new LibVLC();
            _libVLC.Log += _libVLC_Log;
        }

        private void _libVLC_Log(object sender, LogEventArgs e) {
            if (e.Message.ToLower().Contains("authentication failed")) {
                Notification.ShowError("Failed to open stream - authentication failed");
            }
            if (e.Message.ToLower().Contains("connection timeout")) {
                Notification.ShowError("Failed to open stream - connection timeout");
            }
            Logger.Trace(e.FormattedLog);
        }

        private void ReadSources() {
            Sources = new AsyncObservableCollection<RTSPSource>(FromStringToList<RTSPSource>(pluginSettings.GetValueString(nameof(Sources), "")));

            foreach(var source in Sources) {
                source.PropertyChanged += Source_PropertyChanged;
                source.IsLoading = false;
                source.IsRunning = false;
            }

            if (Sources.Count == 0) {
                var source = new RTSPSource {
                    Username = pluginSettings.GetValueString("Username", "<username>"),
                    Password = pluginSettings.GetValueString("Password", ""),
                    Protocol = pluginSettings.GetValueString("Protocol", "rtsp://"),
                    MediaUrl = pluginSettings.GetValueString("MediaUrl", "<media url>")
                };
                source.PropertyChanged += Source_PropertyChanged;
                Sources.Add(source);
            }
        }

        private void DeleteSource(object obj) {
            var source = obj as RTSPSource;
            if (Sources.Count > 0) {
                if (MyMessageBox.Show($"Delete source @ {source.MediaUrl}?", "Delete source", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                    Sources.Remove(source);
                    source.PropertyChanged -= Source_PropertyChanged;
                    SaveSources();
                }
            }

            if (Sources.Count == 0) {
                AddSource(null);
            }
        }

        private void AddSource(object obj) {
            var source = new RTSPSource {
                Username = "<username>",
                Password = "",
                Protocol = "rtsp://",
                MediaUrl = "<media url>"
            };
            Sources.Add(source);
            source.PropertyChanged += Source_PropertyChanged;
        }

        private void Source_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(RTSPSource.Protocol)
                || e.PropertyName == nameof(RTSPSource.Username)
                || e.PropertyName == nameof(RTSPSource.Password)
                || e.PropertyName == nameof(RTSPSource.MediaUrl)) {
                SaveSources();
            }            
        }

        private void SaveSources() {
            var sourcesAsString = FromListToString<RTSPSource>(Sources);
            pluginSettings.SetValueString(nameof(Sources), sourcesAsString);
        }

        public static IList<T> FromStringToList<T>(string collection) {
            try {
                return JsonConvert.DeserializeObject<IList<T>>(collection, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto }) ?? new List<T>();
            } catch (Exception) {
                return new List<T>();
            }

        }

        public static string FromListToString<T>(IList<T> l) {
            try {
                return JsonConvert.SerializeObject(l, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto }) ?? "";
            } catch (Exception) {
                return "";
            }
        }

        private string Decrypt(string encrypt) {
            try {
                return DataProtection.Unprotect(Convert.FromBase64String(encrypt));
            } catch (Exception) {
                return "";
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            StopAllStreams(null);
            ReadSources();
        }

        private void StopAllStreams(object o) {
            foreach(var stream in activeStreams.ToList()) {
                stream.source.IsRunning = false;
            }
        }

        private void StopStream(object o) {
            if(o is RTSPSource source) {
                source.IsRunning = false;
            }
        }

        private PluginOptionsAccessor pluginSettings;



        public List<string> Protocols => new List<string>() {
            "rtsp://",
            "rtsps://",
            "http://",
            "https://"
        };


        public bool IsMuted {
            get => pluginSettings.GetValueBoolean(nameof(IsMuted), false);
            set {
                pluginSettings.SetValueBoolean(nameof(IsMuted), value);
                RaisePropertyChanged();
                _ = MutePlayer(value);
            }
        }

        public double Volume {
            get => pluginSettings.GetValueDouble(nameof(Volume), 1.0d);
            set {
                pluginSettings.SetValueDouble(nameof(Volume), value);
                RaisePropertyChanged();
                _ = SetVolumePlayer(value);
            }
        }

        private AsyncObservableCollection<RTSPSource> sources;
        public AsyncObservableCollection<RTSPSource> Sources {
            get => sources;
            set {
                sources = value;

            }
        }

        private LibVLC _libVLC;
        private Grid panel;

        private async Task<bool> OpenPlayer(MediaPlayer player, RTSPSource source) {
            Uri uri = new Uri($"{source.Protocol}{source.MediaUrl}");
            if (!string.IsNullOrEmpty(source.Username)) {
                if (string.IsNullOrWhiteSpace(source.Password)) {
                    throw new Exception("Password must not be empty");
                }
            }

            return await Application.Current.Dispatcher.InvokeAsync(() => {
                if (player == null) { return false; }
                var media = new Media(_libVLC, uri.ToString(), FromType.FromLocation);
                media.AddOption($":network-caching={RTSPPlugin.Mediator.Plugin.CachingMs}");
                if (!string.IsNullOrEmpty(source.Username)) {                    
                    media.AddOption($":{source.Protocol.Substring(0, source.Protocol.Length - 3)}-user={source.Username}");
                    media.AddOption($":{source.Protocol.Substring(0, source.Protocol.Length - 3)}-pwd={Decrypt(source.Password)}");
                }
                return player.Play(media);
            });
        }
        private async Task MutePlayer(bool value) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                foreach (var child in panel.Children) {
                    if(child is VideoHwndHost host) {
                        host.Player.Mute = value;
                    }
                }
            });
        }
        private async Task SetVolumePlayer(double value) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                foreach (var child in panel.Children) {
                    if(child is VideoHwndHost host) {
                        host.Player.Volume = (int)(value * 100);
                    }
                }
            });
        }

        private async Task StopPlayer(VideoHwndHost host) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                if (host?.Player == null) { return; }
                host.Player.Stop();
                host.Width = 0;
                host.Height = 0;
            });
        }

        private async Task<VLCState> GetPlayerState(MediaPlayer player) {
            return await Application.Current.Dispatcher.InvokeAsync(() => {
                if (player == null) { return VLCState.Ended; }
                return player.State;
            });
        }

        private async Task<bool> IsPlaying(MediaPlayer player) {
            return await Application.Current.Dispatcher.InvokeAsync(() => {
                if (player == null) { return false; }
                return player.IsPlaying;
            });
        }

        private async Task<VideoHwndHost> CreateHost(RTSPSource source, Grid panel) {
            return await Application.Current.Dispatcher.InvokeAsync(() => {
                var player = new MediaPlayer(_libVLC);
                var host = new VideoHwndHost(player);
                host.VerticalAlignment = VerticalAlignment.Top;
                host.HorizontalAlignment = HorizontalAlignment.Left;
                host.Width = 0;
                host.Height = 0;
                panel.Children.Add(host);
                activeStreams.Add(new StreamInfo(source, host));
                return host;
            });
        }

        private async Task DestroyHost(RTSPSource source, Grid panel, VideoHwndHost host) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                panel.Children.Remove(host);
                var streamInfo = activeStreams.FirstOrDefault(x => x.source == source);
                source.IsRunning = false;
                source.IsLoading = false;
                activeStreams.Remove(streamInfo);
            });

        }

        private void SetTheGrid(object o) {
            panel = o as Grid;
        }


        private List<StreamInfo> activeStreams = new List<StreamInfo>();
        private record StreamInfo(RTSPSource source, VideoHwndHost host);

        private async Task<bool> StartStream(object o) {
            _ = Task.Run(async () => {
                using (var cts = new CancellationTokenSource()) {
                    var token = cts.Token;
                    var source = o as RTSPSource;
                    source.IsLoading = true;
                    try {
                        panel.SizeChanged -= RTSPVM_SizeChanged;
                        panel.SizeChanged += RTSPVM_SizeChanged;

                        VideoHwndHost host = null;
                        host = await CreateHost(source, panel);
                        
                        
                        var player = host.Player;
                        player.Hwnd = host.Handle;

                        await MutePlayer(IsMuted);
                        await SetVolumePlayer(Volume);

                        var successfullyOpened = await OpenPlayer(player, source);

                        if (successfullyOpened) {
                            while (await GetPlayerState(player) <= VLCState.Opening) {
                                await Task.Delay(TimeSpan.FromSeconds(1), token);
                            }
                            try {
                                uint videoWidth = 0, videoHeight = 0;
                                while (videoWidth == 0) {
                                    if (await GetPlayerState(player) >= VLCState.Stopped) { throw new OperationCanceledException(); }
                                    player.Size(0, ref videoWidth, ref videoHeight);
                                    token.ThrowIfCancellationRequested();
                                }

                                RTSPVM_SizeChanged(panel, null);

                                source.IsLoading = false;
                                source.IsRunning = true;
                                while (source.IsRunning && await IsPlaying(player)) {
                                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                                }
                            } catch (OperationCanceledException) {
                            } finally {
                            }
                        }
                        await StopPlayer(host);
                        await DestroyHost(source, panel, host);
                        RTSPVM_SizeChanged(panel, null);
                    } catch (OperationCanceledException) {
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    } finally {
                        source.IsLoading = false;
                        source.IsRunning = false;
                    }
                }
                return true;
            });
            return true;
        }

        private async void RTSPVM_SizeChanged(object sender, SizeChangedEventArgs e) {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (!(sender is Grid panel)) return;

                double parentWidth = panel.ActualWidth;
                double parentHeight = panel.ActualHeight;

                // Collect valid VideoHwndHosts with their video dimensions
                var videoHosts = panel.Children.OfType<VideoHwndHost>()
                    .Select(host => {
                        uint vWidth = 0, vHeight = 0;
                        host.Player.Size(0, ref vWidth, ref vHeight);
                        return new { Host = host, Width = (double)vWidth, Height = (double)vHeight };
                    })
                    .Where(x => x.Width > 0 && x.Height > 0)
                    .ToList();

                int totalPlayers = videoHosts.Count;
                if (totalPlayers == 0) return;

                // Determine the optimal grid configuration
                int bestCols = 1, bestRows = totalPlayers;
                double bestScale = 0;
                double optimalCellWidth = parentWidth, optimalCellHeight = parentHeight;

                for (int cols = 1; cols <= totalPlayers; cols++) {
                    int rows = (int)Math.Ceiling((double)totalPlayers / cols);
                    double cellWidth = parentWidth / cols;
                    double cellHeight = parentHeight / rows;

                    double minScaleForConfig = double.MaxValue;
                    foreach (var vh in videoHosts) {
                        double widthScale = cellWidth / vh.Width;
                        double heightScale = cellHeight / vh.Height;
                        double scale = Math.Min(widthScale, heightScale);
                        if (scale < minScaleForConfig) minScaleForConfig = scale;
                    }

                    if (minScaleForConfig > bestScale) {
                        bestScale = minScaleForConfig;
                        bestCols = cols;
                        bestRows = rows;
                        optimalCellWidth = cellWidth;
                        optimalCellHeight = cellHeight;
                    }
                }

                // Layout each video host in the grid
                int currentCol = 0, currentRow = 0;
                foreach (var vh in videoHosts) {
                    double widthScale = optimalCellWidth / vh.Width;
                    double heightScale = optimalCellHeight / vh.Height;
                    double scale = Math.Min(widthScale, heightScale);

                    double scaledWidth = vh.Width * scale;
                    double scaledHeight = vh.Height * scale;

                    double offsetX = (optimalCellWidth - scaledWidth) / 2;
                    double offsetY = (optimalCellHeight - scaledHeight) / 2;

                    vh.Host.Width = Math.Floor(scaledWidth);
                    vh.Host.Height = Math.Floor(scaledHeight);
                    vh.Host.Margin = new Thickness(
                        currentCol * optimalCellWidth + offsetX,
                        currentRow * optimalCellHeight + offsetY,
                        0,
                        0);

                    currentCol++;
                    if (currentCol >= bestCols) {
                        currentCol = 0;
                        currentRow++;
                    }
                }
            });
        }

        public IAsyncCommand StartStreamCommand { get; }
        public ICommand StopStreamCommand { get; }
        public ICommand StopAllStreamsCommand { get; }
        public ICommand AddSourceCommand { get; }
        public ICommand DeleteSourceCommand { get; }
        public ICommand SetTheGridCommand { get; }
    }

    /// <summary>
    /// https://docs.microsoft.com/de-de/dotnet/api/system.security.cryptography.protecteddata.protect?view=dotnet-plat-ext-6.0
    /// </summary>
    internal class DataProtection {

        // Create byte array for additional entropy when using Protect method.
        private static byte[] s_additionalEntropy = { 186, 174, 223, 103, 198, 101, 125, 148, 1, 224 };

        public static byte[] Protect(string data) {
            if (string.IsNullOrWhiteSpace(data)) { return null; }
            return ProtectBytes(Encoding.ASCII.GetBytes(data));
        }

        public static string Unprotect(byte[] data) {
            if (data?.Length == 0) { return string.Empty; }
            var bytes = UnprotectBytes(data);
            return Encoding.ASCII.GetString(bytes);
        }

        private static byte[] ProtectBytes(byte[] data) {
            try {
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
                // only by the same current user.
                return ProtectedData.Protect(data, s_additionalEntropy, DataProtectionScope.CurrentUser);
            } catch (CryptographicException e) {
                Logger.Error("Data was not encrypted. An error occurred.", e);
                return null;
            }
        }

        private static byte[] UnprotectBytes(byte[] data) {
            try {
                //Decrypt the data using DataProtectionScope.CurrentUser.
                return ProtectedData.Unprotect(data, s_additionalEntropy, DataProtectionScope.CurrentUser);
            } catch (CryptographicException e) {
                Logger.Error("Data was not decrypted. An error occurred.", e);
                return null;
            }
        }
    }



    [JsonObject(MemberSerialization.OptOut)]
    public partial class RTSPSource : BaseINPC {
        [ObservableProperty]
        private string username;
        [ObservableProperty]
        private string password;
        [ObservableProperty]
        private string protocol;
        [ObservableProperty]
        private string mediaUrl;
        [ObservableProperty]
        private bool isLoading = false;
        [ObservableProperty]
        private bool isRunning = false;
        public void SetPassword(SecureString s) {
            var pw = SecureStringToString(s);
            var encrypt = DataProtection.Protect(pw);
            if(encrypt == null) {
                Password = "";
            } else {
                Password = Convert.ToBase64String(encrypt);
            }
            
        }
        private string SecureStringToString(SecureString value) {
            IntPtr valuePtr = IntPtr.Zero;
            try {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            } finally {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}