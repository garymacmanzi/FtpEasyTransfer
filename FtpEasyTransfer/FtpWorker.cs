using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FtpEasyTransfer.Options;
using FluentFTP.Rules;
using System.Threading;
using System.IO;
using FtpEasyTransfer.Helpers;

namespace FtpEasyTransfer
{
    public class FtpWorker : IFtpWorker
    {
        private readonly ILogger<FtpWorker> _logger;
        private TransferSettingsOptions _options;
        private string _localDirectory;

        public FtpWorker(ILogger<FtpWorker> logger)
        {
            _logger = logger;
        }

        public async Task RunAsync(TransferSettingsOptions options)
        {
            _options = options;
            _localDirectory = _options.LocalPath;

            if (!_options.LocalPathIsFile)
            {
                Directory.CreateDirectory(_options.LocalPath);
            }

            try
            {
                switch (options.RunMode)
                {
                    case RunMode.DownloadDir:
                        await DownloadDirectoryFromSourceAsync();
                        break;
                    case RunMode.DownloadFile:
                        await DownloadFileFromSourceAsync();
                        break;
                    case RunMode.UploadDir:
                        await UploadDirectoryToDestinationAsync();
                        break;
                    case RunMode.UploadFile:
                        await UploadFileToDestinationAsync();
                        break;
                    case RunMode.SyncDirs:
                        await RunSyncDirsAsync();
                        break;
                    case RunMode.SyncFile:
                        await RunSyncFileAsync();
                        break;
                    default:
                        break;
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
            }

        }

        private async Task RunSyncDirsAsync()
        {
            await DownloadDirectoryFromSourceAsync();

            ChangeFileExtensions(_options.ChangeExtensions);

            await UploadDirectoryToDestinationAsync();

        }

        private async Task RunSyncFileAsync()
        {
            await DownloadFileFromSourceAsync();

            ChangeFileExtensions(_options.ChangeExtensions);

            await UploadFileToDestinationAsync();
        }

        private async Task<List<FtpResult>> DownloadDirectoryFromSourceAsync()
        {
            var token = new CancellationToken();


            using (var ftp = CreateFtpClient(_options.Source))
            {
                ftp.OnLogEvent += Log;

                await ftp.ConnectAsync(token);

                var rules = new List<FtpRule>
                {
                    new FtpFileExtensionRule(true, _options.Source.FileTypesToTransfer)
                };

                var results = await ftp.DownloadDirectoryAsync(_options.LocalPath, _options.Source.RemotePath, FtpFolderSyncMode.Update,
                    FtpLocalExists.Skip, FtpVerify.None, rules);

                if (_options.Source.DeleteOnceTransferred)
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

        private async Task<FtpStatus> DownloadFileFromSourceAsync()
        {
            var token = new CancellationToken();

            using (var ftp = CreateFtpClient(_options.Source))
            {
                ftp.OnLogEvent += Log;

                await ftp.ConnectAsync(token);

                var overwriteExisting = _options.Source.OverwriteExisting ? FtpLocalExists.Overwrite : FtpLocalExists.Skip;

                string localPath = _options.Destination.RemotePath;

                if (!_options.LocalPathIsFile)
                {
                    var fileName = Path.GetFileName(_options.Source.RemotePath);
                    localPath = $"{_options.LocalPath}/{fileName}";
                }

                var result = await ftp.DownloadFileAsync(localPath, _options.Source.RemotePath, overwriteExisting);

                if (_options.Source.DeleteOnceTransferred)
                {
                    if (result.IsSuccess())
                    {
                        try
                        {
                            await ftp.DeleteFileAsync(_options.Source.RemotePath, token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error deleting {RemotePath}: {Message}", _options.Source.RemotePath, ex.Message);
                        }
                    }
                }

                return result;
            }
        }

        private void ChangeFileExtensions(List<ChangeExtensionsOptions> options)
        {
            foreach (var item in options)
            {
                foreach (var file in Directory.GetFiles(_localDirectory, $"*.{item.Source}"))
                {
                    var newFileName = @$"{_localDirectory}\{Path.GetFileNameWithoutExtension(file)}.{item.Target}";
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
            
        }

        private async Task<FtpStatus> UploadFileToDestinationAsync()
        {
            var token = new CancellationToken();

            using (var ftp = CreateFtpClient(_options.Destination))
            {
                ftp.OnLogEvent += Log;

                await ftp.ConnectAsync(token);

                var overwriteExisting = _options.Destination.OverwriteExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;

                string remotePath = _options.Destination.RemotePath;

                if (!_options.Destination.RemotePathIsFile)
                {
                    var fileName = Path.GetFileName(_options.LocalPath);
                    remotePath = $"{_options.Destination.RemotePath}/{fileName}";
                }

                var result = await ftp.UploadFileAsync(_options.LocalPath, remotePath, overwriteExisting);

                if (_options.Destination.DeleteOnceTransferred)
                {
                    if (result.IsSuccess())
                    {
                        try
                        {
                            if (_options.LocalPathIsFile)
                            {
                                File.Delete(_options.LocalPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error deleting {LocalPath}: {Message}", _options.LocalPath, ex.Message);
                        }
                    }
                }

                return result;
            };
        }

        private async Task<List<FtpResult>> UploadDirectoryToDestinationAsync()
        {
            var token = new CancellationToken();

            using (var ftp = CreateFtpClient(_options.Destination))
            {
                ftp.OnLogEvent += Log;

                var rules = new List<FtpRule>
                {
                    new FtpFileExtensionRule(true, _options.Destination.FileTypesToTransfer)
                };

                var overwriteExisting = _options.Destination.OverwriteExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;

                await ftp.ConnectAsync(token);

                var results = await ftp.UploadDirectoryAsync(_options.LocalPath, _options.Destination.RemotePath, FtpFolderSyncMode.Update,
                    overwriteExisting, FtpVerify.None, rules);

                await ValidateResults(results, ftp, _options.Destination.DeleteOnceTransferred);

                return results;
            }
        }

        private async Task ValidateResults(List<FtpResult> results, FtpClient ftp, bool deleteIfSuccess)
        {
            foreach (var result in results)
            {
                if (result.IsSuccess)
                {
                    if (deleteIfSuccess)
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
                }
                else
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
