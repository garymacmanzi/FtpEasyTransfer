using System.Collections.Generic;
using System.IO;

namespace FtpEasyTransfer.Options
{
    public class TransferSettingsOptions
    {
        public string LocalPath { get; set; }
        public List<ChangeExtensionsOptions> ChangeExtensions { get; set; } = new List<ChangeExtensionsOptions>();
        public SourceSettings Source { get; set; }
        public DestinationSettings Destination { get; set; }
        public bool LocalPathIsFile
        {
            get
            {
                return !string.IsNullOrEmpty(Path.GetExtension(LocalPath));
            }
        }
    }
}
