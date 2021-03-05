using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionDataPollerService.Models
{
    public class SourceSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string DirectoryPath { get; set; }
        public List<string> FileTypesToDownload { get; set; } = new List<string>();
        public bool DeleteOnceDownloaded { get; set; } = false;
    }
}
