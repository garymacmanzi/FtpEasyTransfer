using System.Collections.Generic;
using System.IO;

namespace FtpEasyTransfer.Options
{
    public class SourceSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string RemotePath { get; set; }
        public List<string> FileTypesToDownload { get; set; } = new List<string>();
        public bool DeleteOnceDownloaded { get; set; } = false;
        public bool OverwriteExisting { get; set; }
        public bool RemotePathIsFile
        {
            get
            {
                return !string.IsNullOrEmpty(Path.GetExtension(RemotePath));
            }
        }
    }
}
