using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FtpEasyTransfer.Options;
using FluentFTP.Rules;
using System.Threading;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using FtpEasyTransfer.Helpers;
using Microsoft.Extensions.Configuration;

namespace FtpEasyTransfer
{
    public class FtpWorker : BackgroundService
    {
        private readonly ILogger<FtpWorker> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IRunModeRetriever _modeRetriever;
        private readonly IConfiguration _config;
        private List<TransferSettingsOptions> _options;
        private int _pollFrequency;

        public FtpWorker(ILogger<FtpWorker> logger, IOptions<List<TransferSettingsOptions>> options,
            IHostApplicationLifetime hostApplicationLifetime, IRunModeRetriever modeRetriever,
            IConfiguration config)
        {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
            _modeRetriever = modeRetriever;
            _config = config;
            _options = options.Value;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            ValidateSettings();
            _logger.LogInformation("Starting service at: {time}", DateTimeOffset.Now);
            

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Poll Frequency (ms): {frequency}", _pollFrequency);

                foreach (var transfer in _options)
                {
                    await RunTransferAsync(transfer);
                }

                await Task.Delay(_pollFrequency, stoppingToken);
            }
        }

        private void ValidateSettings()
        {
            try
            {
                _pollFrequency = _config.GetValue<int>("PollFrequency");
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Unable to retrieve poll frequency: {Message}", ex.Message);
                _hostApplicationLifetime.StopApplication();
            }
            

            foreach (var item in _options)
            {
                if (string.IsNullOrWhiteSpace(item.LocalPath))
                {
                    _logger.LogCritical("LocalDirectory not set. Check appsettings.json");
                    _hostApplicationLifetime.StopApplication();
                };

                List<string> normalisedExtensions = new();

                if (item.Source is not null)
                {
                    foreach (var fileType in item.Source.FileTypesToTransfer)
                    {
                        normalisedExtensions.Add(fileType.Normalise());
                    }

                    item.Source.FileTypesToTransfer = normalisedExtensions;
                }

                foreach (var ext in item.ChangeExtensions)
                {
                    if (string.IsNullOrWhiteSpace(ext.Source) || string.IsNullOrWhiteSpace(ext.Target))
                    {
                        _logger.LogCritical("Invalid or empty extension source/target in \"ChangeExtensions\". Check appsettings.json");
                        _hostApplicationLifetime.StopApplication();
                    }
                }

                item.RunMode = _modeRetriever.RetrieveRunMode(item);
            }
        }

        public async Task RunTransferAsync(TransferSettingsOptions transferOptions)
        {
            if (!transferOptions.LocalPathIsFile)
            {
                Directory.CreateDirectory(transferOptions.LocalPath);
            }

            try
            {
                switch (transferOptions.RunMode)
                {
                    case RunMode.DownloadDir:
                        await DownloadDirectoryFromSourceAsync(transferOptions);
                        break;
                    case RunMode.DownloadFile:
                        await DownloadFileFromSourceAsync(transferOptions);
                        break;
                    case RunMode.UploadDir:
                        await UploadDirectoryToDestinationAsync(transferOptions);
                        break;
                    case RunMode.UploadFile:
                        await UploadFileToDestinationAsync(transferOptions);
                        break;
                    case RunMode.SyncDirs:
                        await RunSyncDirsAsync(transferOptions);
                        break;
                    case RunMode.SyncFile:
                        await RunSyncFileAsync(transferOptions);
                        break;
                    default:
                        break;
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                _logger.LogDebug("Stack Trace: {StackTrace}", ex.StackTrace);
                if (ex.InnerException is not null)
                {
                    _logger.LogDebug("Inner exception: {InnerException}", ex.InnerException);
                }
            }

        }

        private async Task RunSyncDirsAsync(TransferSettingsOptions transfer)
        {
            List<FtpResult> transferResults = await DownloadDirectoryFromSourceAsync(transfer);

            ChangeFileExtensions(transfer, transferResults);

            await UploadDirectoryToDestinationAsync(transfer);

        }

        private async Task RunSyncFileAsync(TransferSettingsOptions transfer)
        {
            bool isSuccess = await DownloadFileFromSourceAsync(transfer);

            if (isSuccess)
            {
                ChangeFileExtensions(transfer, transfer.LocalPath);
            }
            
            await UploadFileToDestinationAsync(transfer);
        }

        private async Task<List<FtpResult>> DownloadDirectoryFromSourceAsync(TransferSettingsOptions transfer)
        {
            var token = new CancellationToken();

            using (var ftp = CreateFtpClient(transfer.Source))
            {
                ftp.OnLogEvent += Log;
                await ftp.ConnectAsync(token);

                var rules = new List<FtpRule>
                {
                    new FtpFileExtensionRule(true, transfer.Source.FileTypesToTransfer)
                };

                var overwriteExisting = transfer.Source.OverwriteExisting ? FtpLocalExists.Overwrite : FtpLocalExists.Skip;

                var results = await ftp.DownloadDirectoryAsync(transfer.LocalPath, transfer.Source.RemotePath, FtpFolderSyncMode.Update,
                    overwriteExisting, FtpVerify.None, rules);

                await ValidateFtpResultList(results, ftp, transfer.Source.DeleteOnceTransferred);

                return results;
            }
        }

        private async Task<bool> DownloadFileFromSourceAsync(TransferSettingsOptions transfer)
        {
            var token = new CancellationToken();

            using (var ftp = CreateFtpClient(transfer.Source))
            {
                ftp.OnLogEvent += Log;
                await ftp.ConnectAsync(token);

                var overwriteExisting = transfer.Source.OverwriteExisting ? FtpLocalExists.Overwrite : FtpLocalExists.Skip;

                var result = await ftp.DownloadFileAsync(transfer.LocalPath, transfer.Source.RemotePath, overwriteExisting);

                if (transfer.Source.DeleteOnceTransferred)
                {
                    if (result.IsSuccess())
                    {
                        try
                        {
                            await ftp.DeleteFileAsync(transfer.Source.RemotePath, token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error deleting {RemotePath}: {Message}", transfer.Source.RemotePath, ex.Message);
                        }
                    }
                }

                return result.IsSuccess();
            }
        }

        private void ChangeFileExtensions(TransferSettingsOptions transfer, List<FtpResult> results)
        {
            foreach (var file in results.Where(x => x.IsSuccess))
            {
                ChangeFileExtensions(transfer, file.LocalPath);
            }            
        }

        private void ChangeFileExtensions(TransferSettingsOptions transfer, string localFilePath)
        {
            foreach (var item in transfer.ChangeExtensions)
            {
                var targetFileName = @$"{transfer.LocalPath}\{Path.GetFileNameWithoutExtension(localFilePath)}.{item.Target}";
                try
                {
                    File.Move(localFilePath, targetFileName, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Moving file {file} failed: {Message}", localFilePath, ex.Message);
                }
            }
        }

        private async Task<FtpStatus> UploadFileToDestinationAsync(TransferSettingsOptions transfer)
        {
            var token = new CancellationToken();

            using (var ftp = CreateFtpClient(transfer.Destination))
            {
                ftp.OnLogEvent += Log;

                await ftp.ConnectAsync(token);

                var overwriteExisting = transfer.Destination.OverwriteExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;

                var result = await ftp.UploadFileAsync(transfer.LocalPath, transfer.Destination.RemotePath, overwriteExisting);

                if (transfer.Destination.DeleteOnceTransferred)
                {
                    if (result.IsSuccess())
                    {
                        try
                        {
                            File.Delete(transfer.LocalPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error deleting {LocalPath}: {Message}", transfer.LocalPath, ex.Message);
                        }
                    }
                }

                return result;
            };
        }

        private async Task<List<FtpResult>> UploadDirectoryToDestinationAsync(TransferSettingsOptions transfer)
        {
            var token = new CancellationToken();

            using (var ftp = CreateFtpClient(transfer.Destination))
            {
                ftp.OnLogEvent += Log;

                var rules = new List<FtpRule>
                {
                    new FtpFileExtensionRule(true, transfer.Destination.FileTypesToTransfer)
                };

                var overwriteExisting = transfer.Destination.OverwriteExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;

                await ftp.ConnectAsync(token);

                var results = await ftp.UploadDirectoryAsync(transfer.LocalPath, transfer.Destination.RemotePath, FtpFolderSyncMode.Update,
                    overwriteExisting, FtpVerify.None, rules);

                await ValidateFtpResultList(results, ftp, transfer.Destination.DeleteOnceTransferred);

                return results;
            }
        }

        private async Task ValidateFtpResultList(List<FtpResult> results, FtpClient ftp, bool deleteIfSuccess)
        {
            foreach (var result in results)
            {
                if (result.IsSuccess && deleteIfSuccess)
                {
                    if (result.IsDownload && result.Type == FtpFileSystemObjectType.File)
                    {
                        await ftp.DeleteFileAsync(result.RemotePath);
                    }
                    else if (result.Type == FtpFileSystemObjectType.File)
                    {
                        File.Delete(result.LocalPath);
                    }
                }
                else if (result.IsFailed)
                {
                    _logger.LogWarning("Transfer of {Name} failed, reason: {Exception}", result.Name, result.Exception);
                }
            }
        }

        private static FtpClient CreateFtpClient(TransferDetails details)
        {
            return new FtpClient(details.Server, details.Port, details.User, details.Password);
        }

        

        private void Log(FtpTraceLevel traceLevel, string content)
        {
            switch (traceLevel)
            {
                case FtpTraceLevel.Verbose:
                    _logger.LogDebug(content);
                    break;
                case FtpTraceLevel.Info:
                    _logger.LogInformation(content);
                    break;
                case FtpTraceLevel.Warn:
                    _logger.LogWarning(content);
                    break;
                case FtpTraceLevel.Error:
                    _logger.LogError(content);
                    break;
                default:
                    break;
            }
        }
    }
}
