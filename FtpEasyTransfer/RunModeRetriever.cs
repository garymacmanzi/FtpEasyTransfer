using FtpEasyTransfer.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FtpEasyTransfer
{
    public class RunModeRetriever : IRunModeRetriever
    {
        private readonly ILogger<RunModeRetriever> _logger;

        public RunModeRetriever(ILogger<RunModeRetriever> logger)
        {
            _logger = logger;
        }

        public RunMode RetrieveRunMode(TransferSettingsOptions options)
        {
            bool sourceIsNull = options.Source is null;
            bool destinationIsNull = options.Destination is null;

            if (options.LocalPathIsFile)
            {
                _logger.LogDebug("Local Path: {LocalPath} is file, RunMode determined as UploadFile", options.LocalPath);
                return RunMode.UploadFile;
            }

            if (!sourceIsNull && !destinationIsNull)
            {
                if (options.Destination.RemotePathIsFile)
                {
                    _logger.LogDebug("Source & Destination defined, Source.RemotePath: {RemotePath} is file, RunMode determined as SyncFile", options.Source.RemotePath);
                    return RunMode.UploadFile;
                }
                else
                {
                    _logger.LogDebug("Source & Destination defined, Source.RemotePath: {RemotePath} is directory, RunMode determined as SyncDirs", options.Source.RemotePath);
                    return RunMode.UploadDir;
                }
            }
            else if (sourceIsNull && !destinationIsNull)
            {
                if (options.Destination.RemotePathIsFile)
                {
                    _logger.LogDebug("Only Destination defined, Destination.RemotePath: {RemotePath} is file, RunMode determined as UploadFile", options.Destination.RemotePath);
                    return RunMode.UploadFile;
                }
                else
                {
                    _logger.LogDebug("Only Destination defined, Destination.RemotePath: {RemotePath} is directory, RunMode determined as UploadDir", options.Destination.RemotePath);
                    return RunMode.UploadDir;
                }
            }
            else
            {
                if (options.Source.RemotePathIsFile)
                {
                    _logger.LogDebug("Only Source defined, Source.RemotePath: {RemotePath} is file, RunMode determined as DownloadFile", options.Source.RemotePath);
                    return RunMode.DownloadFile;
                }
                else
                {
                    _logger.LogDebug("Only Source defined, Source.RemotePath: {RemotePath} is directory, RunMode determined as DownloadDir", options.Source.RemotePath);
                    return RunMode.DownloadDir;
                }
            }
        }
    }
}
