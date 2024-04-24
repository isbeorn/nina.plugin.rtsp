using Newtonsoft.Json;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Unosquare.FFME;

namespace NINA.Plugin.RTSP.Dockables {

    [Export(typeof(IDockableVM))]
    public class RTSPVM : DockableVM {
        private CancellationTokenSource cts;
        private bool optionsExpanded;

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
            
            OptionsExpanded = true;
            StartStreamCommand = new AsyncCommand<bool>(StartStream);
            StopStreamCommand = new GalaSoft.MvvmLight.Command.RelayCommand(StopStream);
            AddSourceCommand = new RelayCommand(AddSource);
            DeleteSourceCommand = new RelayCommand(DeleteSource);

            RTSPPlugin.Mediator.RegisterRTSPPlayer(this);

            ReadSources();
            profileService.ProfileChanged += ProfileService_ProfileChanged;

        }

        private void ReadSources() {
            Sources = new AsyncObservableCollection<RTSPSource>(FromStringToList<RTSPSource>(pluginSettings.GetValueString(nameof(Sources), "")));

            if (Sources.Count == 0) {
                var source = new RTSPSource {
                    Username = pluginSettings.GetValueString(nameof(Username), "<username>"),
                    Password = pluginSettings.GetValueString(nameof(Password), ""),
                    Protocol = pluginSettings.GetValueString(nameof(Protocol), "rtsp://"),
                    MediaUrl = pluginSettings.GetValueString(nameof(MediaUrl), "<media url>")
                };
                Sources.Add(source);
                SelectedSource = source;
            } else {
                SelectedSource = Sources.First();
            }
        }

        private void DeleteSource(object obj) {
            if(Sources.Count > 0 && SelectedSource != null) {
                if(MyMessageBox.Show($"Delete source @ {SelectedSource.MediaUrl}?", "Delete source", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) { 
                    Sources.Remove(SelectedSource);
                    SaveSources();
                }
            }
            
            if(Sources.Count > 0) {
                SelectedSource = Sources.First();
            } else {
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
            SelectedSource = source;
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
            StopStream();
            ReadSources();
        }

        private void StopStream() {
            try { cts?.Cancel(); } catch (Exception) { }
        }

        private PluginOptionsAccessor pluginSettings;

        public bool OptionsExpanded {
            get => optionsExpanded;
            set {
                optionsExpanded = value;
                RaisePropertyChanged();
            }
        }

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
            }
        }

        public double Volume {
            get => pluginSettings.GetValueDouble(nameof(Volume), 1.0d);
            set {
                pluginSettings.SetValueDouble(nameof(Volume), value);
                RaisePropertyChanged();
            }
        }        

        public string Protocol {
            get => SelectedSource?.Protocol ?? "rtsp://";
            set {
                if (SelectedSource == null) return;

                SelectedSource.Protocol = value;
                RaisePropertyChanged();
                SaveSources();
            }
        }

        public string MediaUrl {
            get => SelectedSource?.MediaUrl ?? "";
            set {
                if (SelectedSource == null) return;

                SelectedSource.MediaUrl = value;
                RaisePropertyChanged();
                SaveSources();
            }
        }

        public string Username {
            get => SelectedSource?.Username ?? "";
            set {
                if (SelectedSource == null) return;

                SelectedSource.Username = value;
                RaisePropertyChanged();
                SaveSources();
            }
        }

        public string Password {
            get => Decrypt(SelectedSource?.Password ?? "");
            set {
                if (SelectedSource == null) return;

                if (!string.IsNullOrWhiteSpace(value)) {
                    var encrypt = DataProtection.Protect(value);
                    SelectedSource.Password = Convert.ToBase64String(encrypt);
                } else {
                    SelectedSource.Password = "";
                }

                RaisePropertyChanged();
                SaveSources();
            }
        }

        private AsyncObservableCollection<RTSPSource> sources;
        public AsyncObservableCollection<RTSPSource> Sources {
            get => sources;
            set {
                sources = value;

            }
        }

        private RTSPSource selectedSource;
        public RTSPSource SelectedSource {
            get => selectedSource;
            set {
                selectedSource = value;
                RaisePropertyChanged();
                if(selectedSource != null && media != null && media.IsOpen) {                    
                    StopStream();
                    if((SelectedSource.MediaUrl != "") && (SelectedSource.MediaUrl != "<media url>")) {
                        _ = RestartStream(media);
                    }                    
                }
            }
        }
        private async Task RestartStream(MediaElement media) {
            while(media.IsOpen) {
                await Task.Delay(100);
            }
            await StartStreamCommand.ExecuteAsync(media);
        }

        public void SetPassword(SecureString s) {
            Password = SecureStringToString(s);
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

        private MediaElement media;
        private DateTimeOffset lastFrameDecoded = DateTimeOffset.MinValue;

        private async Task<bool> StartStream(object o) {
            return await Task.Run(async () => {
                using (cts = new CancellationTokenSource()) {
                    try {
                        media = o as MediaElement;
                        Uri uri;
                        if (string.IsNullOrEmpty(Username)) {
                            uri = new Uri($"{Protocol}{MediaUrl}");
                        } else {
                            if (string.IsNullOrWhiteSpace(Password)) {
                                throw new Exception("Password must not be empty");
                            }
                            uri = new Uri($"{Protocol}{System.Web.HttpUtility.UrlEncode(Username)}:{System.Web.HttpUtility.UrlEncode(Password)}@{MediaUrl}");
                        }

                        lastFrameDecoded = DateTimeOffset.MinValue;
                        var successfullyOpened = await media.Open(uri);

                        if (successfullyOpened) {
                            try {
                                OptionsExpanded = false;
                                media.VideoFrameDecoded += Media_VideoFrameDecoded;
                                while (media.IsOpen) {
                                    if (lastFrameDecoded > DateTimeOffset.MinValue && DateTimeOffset.UtcNow - lastFrameDecoded > TimeSpan.FromSeconds(5)) {
                                        Logger.Info("Restarting RTSP Stream as the last decoded frame is longer than 5 seconds ago");
                                        cts.Cancel();
                                        _ = RestartStream(media);
                                    }
                                    
                                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                                }
                            } catch (OperationCanceledException) {
                            } finally {
                                media.VideoFrameDecoded -= Media_VideoFrameDecoded;
                            }
                        }
                        await media.Close();
                    } catch(OperationCanceledException) {
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(ex.Message);
                    } finally {
                        OptionsExpanded = true;
                    }
                }
                return true;
            });
        }

        private void Media_VideoFrameDecoded(object sender, Unosquare.FFME.Common.FrameDecodedEventArgs e) {
            lastFrameDecoded = DateTimeOffset.UtcNow;
        }


        public IAsyncCommand StartStreamCommand { get; }
        public ICommand StopStreamCommand { get; }
        public ICommand AddSourceCommand { get; }
        public ICommand DeleteSourceCommand { get; }
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
    public class RTSPSource {
        public string Username { get; set; }

        public string Password { get; set; }

        public string Protocol { get; set; }
        public string MediaUrl { get; set; }
    }
}