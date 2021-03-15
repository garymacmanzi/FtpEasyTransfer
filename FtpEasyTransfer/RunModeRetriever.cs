using FtpEasyTransfer.Options;
using Microsoft.Extensions.Logging;
using FtpEasyTransfer.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
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

            // If LocalPathIsFile, mode can only be UploadFile
            if (options.LocalPathIsFile)
            {
                _logger.LogDebug("Local Path: {LocalPath} is file, RunMode determined as UploadFile", options.LocalPath);
                options.Destination.RemotePath = options.Destination.RemotePath.EnsurePathIsFile(options.LocalPath);
                return RunMode.UploadFile;
            }

            // If both source & destination are defined in settings
            if (!sourceIsNull && !destinationIsNull)
            {
                // and the remote source is a file, mode must be SyncFile
                if (options.Source.RemotePathIsFile)
                {
                    _logger.LogDebug("Source & Destination defined, Source.RemotePath: {RemotePath} is file, RunMode determined as SyncFile", options.Source.RemotePath);
                    options.Destination.RemotePath = options.Destination.RemotePath.EnsurePathIsFile(options.Source.RemotePath);
                    options.LocalPath = options.LocalPath.EnsurePathIsFile(options.Source.RemotePath);
                    return RunMode.SyncFile;
                }
                else
                {
                    if (options.Destination.RemotePathIsFile)
                    {
                        throw new Exception("Destination RemotePath cannot be a file if Source RemotePath is not a file");
                    }
                    _logger.LogDebug("Source & Destination defined, Source.RemotePath: {RemotePath} is directory, RunMode determined as SyncDirs", options.Source.RemotePath);
                    return RunMode.SyncDirs;
                }
            }
            
            // if no source defined, and destination defined, already checked if localpathisfile, so mode must be UploadDir
            if (sourceIsNull && !destinationIsNull)
            {
                _logger.LogDebug("Only Destination defined, Destination.RemotePath: {RemotePath} is directory, RunMode determined as UploadDir", options.Destination.RemotePath);
                return RunMode.UploadDir;
            }
            // Otherwise, all that's left is to check source
            else
            {
                if (options.Source.RemotePathIsFile)
                {
                    _logger.LogDebug("Only Source defined, Source.RemotePath: {RemotePath} is file, RunMode determined as DownloadFile", options.Source.RemotePath);
                    options.LocalPath = options.LocalPath.EnsurePathIsFile(options.Source.RemotePath);
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
