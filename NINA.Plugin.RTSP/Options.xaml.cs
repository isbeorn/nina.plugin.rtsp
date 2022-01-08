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

namespace NINA.Plugin.RTSP {
    [Export(typeof(ResourceDictionary))]
    partial class Options : ResourceDictionary {
        public Options() {
            InitializeComponent();            
        }
    }
}
