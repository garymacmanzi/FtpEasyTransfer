using System.IO;

namespace FtpEasyTransfer.Options
{
    public class DestinationSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string RemotePath { get; set; }
        public bool DeleteOnceUploaded { get; set; } = false;
        public bool OverwriteExisting { get; set; } = false;
        public bool RemotePathIsFile 
        {
            get
            {
                return !string.IsNullOrEmpty(Path.GetExtension(RemotePath));
            }
        }
    }
}
