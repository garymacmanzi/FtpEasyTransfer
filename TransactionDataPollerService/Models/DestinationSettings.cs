using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionDataPollerService.Models
{
    public class DestinationSettings
    {
        public string Name { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string DirectoryPath { get; set; }
        public bool DeleteOnceUploaded { get; set; } = false;
    }
}
