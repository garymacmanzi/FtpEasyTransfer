using System;
using System.Threading.Tasks;
using TransactionDataPollerService.Options;

namespace TransactionDataPollerService
{
    public interface IFtpWorker
    {
        Task RunAsync(TransferSettingsOptions options);
    }
}