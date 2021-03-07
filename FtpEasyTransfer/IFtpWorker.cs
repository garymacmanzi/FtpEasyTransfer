using System;
using System.Threading.Tasks;
using FtpEasyTransfer.Options;

namespace FtpEasyTransfer
{
    public interface IFtpWorker
    {
        Task RunAsync(TransferSettingsOptions options);
    }
}