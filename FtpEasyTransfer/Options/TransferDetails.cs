using FluentFTP;
using System.Collections.Generic;
using System.IO;

namespace FtpEasyTransfer.Options
{
    public class TransferDetails
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string RemotePath { get; set; }
        public List<string> FileTypesToTransfer { get; set; } = new List<string>();
        public bool DeleteOnceTransferred { get; set; } = false;
        public bool OverwriteExisting { get; set; } = false;
        public FtpFolderSyncMode FolderSyncMode { get; set; } = FtpFolderSyncMode.Update;
        public bool RemotePathIsFile
        {
            get
            {
                return !string.IsNullOrEmpty(Path.GetExtension(RemotePath));
            }
        }
    }
}
