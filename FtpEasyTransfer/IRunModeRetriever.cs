using FtpEasyTransfer.Options;

namespace FtpEasyTransfer
{
    public interface IRunModeRetriever
    {
        RunMode RetrieveRunMode(TransferSettingsOptions options);
    }
}