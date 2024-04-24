using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.RTSP.Instructions {
    [ExportMetadata("Name", "Stop RTSP Player")]
    [ExportMetadata("Description", "Stops the RTSP Video feed")]
    [ExportMetadata("Icon", "NINA.Plugin.RTSP_CameraSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StopRTSPPlayer : SequenceItem {
        [ImportingConstructor]
        public StopRTSPPlayer() { }
        private StopRTSPPlayer(StopRTSPPlayer cloneMe) {
            CopyMetaData(cloneMe);
        }
        public override object Clone() {
            return new StopRTSPPlayer(this);
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            RTSPPlugin.Mediator.Player.StopStreamCommand.Execute(null);
        }
    }
}
