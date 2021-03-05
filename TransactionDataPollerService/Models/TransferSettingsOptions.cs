using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionDataPollerService.Models
{
    public class TransferSettingsOptions
    {
        public string LocalDirectory { get; set; }
        public SourceSettings Source { get; set; } = new SourceSettings();
        public DestinationSettings Destination { get; set; } = new DestinationSettings();
    }
}
