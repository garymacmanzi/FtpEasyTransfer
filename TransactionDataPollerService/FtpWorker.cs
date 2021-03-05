using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionDataPollerService.Models;
using System.Net;
using FluentFTP.Rules;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace TransactionDataPollerService
{
    public class FtpWorker : IFtpWorker
    {
        private readonly ILogger<FtpWorker> _logger;
        private string _localDirectory;

        public FtpWorker(ILogger<FtpWorker> logger)
        {
            _logger = logger;
        }

        public async Task RunAsync(TransferSettingsOptions options)
        {
            _localDirectory = $"{options.LocalDirectory}\\{options.Destination.Name}";

            Directory.CreateDirectory(_localDirectory);

            _logger.LogDebug("Start transfer for: {Name}", options.Destination.Name);

            try
            {
                await DownloadFromSourceAsync(options);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in DownloadFromSource: {Message}", ex.Message);
            }

            RenamePFiles(options);

            try
            {
                await UploadToDestinationAsync(options);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception in UploadToDestination: {Message}", ex.Message);
            }
            
        }

        private async Task<List<FtpResult>> DownloadFromSourceAsync(TransferSettingsOptions options)
        {
            var token = new CancellationToken();


            using (var ftp = new FtpClient(options.Source.Server, options.Source.Port, options.Source.User, options.Source.Password))
            {
                ftp.OnLogEvent += Log;

                await ftp.ConnectAsync(token);

                var rules = new List<FtpRule>
                {
                    new FtpFileExtensionRule(true, options.Source.FileTypesToDownload)
                };

                var results = await ftp.DownloadDirectoryAsync(_localDirectory, options.Source.DirectoryPath, FtpFolderSyncMode.Update,
                    FtpLocalExists.Skip, FtpVerify.None, rules);

                if (options.Source.DeleteOnceDownloaded)
                {
                    foreach (var download in results)
                    {
                        if (download.IsSuccess && download.Type == FtpFileSystemObjectType.File)
                        {
                            await ftp.DeleteFileAsync(download.RemotePath);
                        }
                    }
                }

                foreach (var download in results)
                {
                    if (download.IsFailed)
                    {
                        _logger.LogWarning("Download of {Name} failed: {Exception}", download.Name, download.Exception);
                    }
                }

                return results;
            }
        }

        private void RenamePFiles(TransferSettingsOptions options)
        {
            foreach (var file in Directory.GetFiles(_localDirectory, "*.PRF"))
            {
                var newFileName = @$"{_localDirectory}\{Path.GetFileNameWithoutExtension(file)}.PDF";
                try
                {
                    File.Move(file, newFileName, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Moving file {file} failed: {Message}", file, ex.Message);
                }
                
            }
        }

        private async Task<List<FtpResult>> UploadToDestinationAsync(TransferSettingsOptions options)
        {
            var token = new CancellationToken();

            using (var ftp = new FtpClient(options.Destination.Server, options.Destination.Port, options.Destination.User, options.Destination.Password))
            {
                ftp.OnLogEvent += Log;

                await ftp.ConnectAsync(token);

                var results = await ftp.UploadDirectoryAsync(_localDirectory, options.Destination.DirectoryPath, FtpFolderSyncMode.Update,
                    FtpRemoteExists.Skip, FtpVerify.None);

                if (options.Destination.DeleteOnceUploaded)
                {
                    foreach (var upload in results)
                    {
                        if (upload.IsSuccess)
                        {
                            try
                            {
                                File.Delete(upload.LocalPath);
                                _logger.LogInformation("File deleted: {LocalPath}", upload.LocalPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("Error deleting file {LocalPath}: {Message}", upload.LocalPath, ex.Message);
                            }
                            
                        }
                    }
                }

                foreach (var upload in results)
                {
                    if (upload.IsFailed)
                    {
                        _logger.LogWarning("Upload of {LocalPath} failed: {Exception}", upload.LocalPath, upload.Exception);
                    }
                }

                return results;
            }
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
