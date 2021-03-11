using System.Collections.Generic;
using System.IO;

namespace FtpEasyTransfer.Options
{
    public class TransferSettingsOptions
    {
        public string LocalPath { get; set; }
        public List<ChangeExtensionsOptions> ChangeExtensions { get; set; } = new List<ChangeExtensionsOptions>();
        public TransferDetails Source { get; set; }
        public TransferDetails Destination { get; set; }
        public bool LocalPathIsFile
        {
            get
            {
                return !string.IsNullOrEmpty(Path.GetExtension(LocalPath));
            }
        }
        public RunMode RunMode { get; set; }
    }
}
