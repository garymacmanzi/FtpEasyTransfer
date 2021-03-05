using System;
using System.Threading.Tasks;
using TransactionDataPollerService.Models;

namespace TransactionDataPollerService
{
    public interface IFtpWorker
    {
        Task RunAsync(TransferSettingsOptions options);
    }
}