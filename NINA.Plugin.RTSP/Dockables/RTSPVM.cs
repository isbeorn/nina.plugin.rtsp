using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private string password;
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
            var encrypt = pluginSettings.GetValueString(nameof(Password), "");
            try {
                var pw = DataProtection.Unprotect(Convert.FromBase64String(encrypt));
                Password = pw;
            } catch (Exception) {
                Password = "";
            }

            OptionsExpanded = true;
            StartStreamCommand = new AsyncCommand<bool>(StartStream);
            StopStreamCommand = new GalaSoft.MvvmLight.Command.RelayCommand(StopStream);
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
            "http://",
            "https://"
        };

        public string Protocol {
            get => pluginSettings.GetValueString(nameof(Protocol), "rtsp://");
            set {
                pluginSettings.SetValueString(nameof(Protocol), value);
                RaisePropertyChanged();
            }
        }

        public string MediaUrl {
            get => pluginSettings.GetValueString(nameof(MediaUrl), "");
            set {
                pluginSettings.SetValueString(nameof(MediaUrl), value);
                RaisePropertyChanged();
            }
        }

        public string Username {
            get => pluginSettings.GetValueString(nameof(Username), "");
            set {
                pluginSettings.SetValueString(nameof(Username), value);
                RaisePropertyChanged();
            }
        }

        public string Password {
            get => password;
            set {
                password = value;

                if (!string.IsNullOrWhiteSpace(value)) {
                    var encrypt = DataProtection.Protect(value);
                    pluginSettings.SetValueString(nameof(Password), Convert.ToBase64String(encrypt));
                } else {
                    pluginSettings.SetValueString(nameof(Password), "");
                }

                RaisePropertyChanged();
            }
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

        private async Task<bool> StartStream(object o) {
            return await Task.Run(async () => {
                using (cts = new CancellationTokenSource()) {
                    try {
                        var media = o as MediaElement;
                        Uri uri;
                        if (string.IsNullOrEmpty(Username)) {
                            uri = new Uri($"{Protocol}{MediaUrl}");
                        } else {
                            if (string.IsNullOrWhiteSpace(Password)) {
                                throw new Exception("Password must not be empty");
                            }
                            uri = new Uri($"{Protocol}{System.Web.HttpUtility.UrlEncode(Username)}:{System.Web.HttpUtility.UrlEncode(Password)}@{MediaUrl}");
                        }

                        var successfullyOpened = await media.Open(uri);

                        if (successfullyOpened) {
                            try {
                                OptionsExpanded = false;
                                while (media.IsOpen) {
                                    await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
                                }
                            } catch (OperationCanceledException) {
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

        public ICommand StartStreamCommand { get; }
        public ICommand StopStreamCommand { get; }
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
}