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
                if (ex.InnerException is not null)
                {
                    _logger.LogError("Inner exception: {InnerException}", ex.InnerException);
                }
            }

        }

        private async Task RunSyncDirsAsync()
        {
            List<FtpResult> transferResults = await DownloadDirectoryFromSourceAsync();

            ChangeFileExtensions(_options.ChangeExtensions, transferResults);

            await UploadDirectoryToDestinationAsync();

        }

        private async Task RunSyncFileAsync()
        {
            bool isSuccess = await DownloadFileFromSourceAsync();

            if (isSuccess)
            {
                ChangeFileExtensions(_options.ChangeExtensions, _options.LocalPath);
            }
            
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

                var overwriteExisting = _options.Source.OverwriteExisting ? FtpLocalExists.Overwrite : FtpLocalExists.Skip;

                var results = await ftp.DownloadDirectoryAsync(_options.LocalPath, _options.Source.RemotePath, FtpFolderSyncMode.Update,
                    overwriteExisting, FtpVerify.None, rules);

                await ValidateFtpResultList(results, ftp, _options.Source.DeleteOnceTransferred);

                return results;
            }
        }

        private async Task<bool> DownloadFileFromSourceAsync()
        {
            var token = new CancellationToken();

            using (var ftp = CreateFtpClient(_options.Source))
            {
                ftp.OnLogEvent += Log;
                await ftp.ConnectAsync(token);

                var overwriteExisting = _options.Source.OverwriteExisting ? FtpLocalExists.Overwrite : FtpLocalExists.Skip;

                var result = await ftp.DownloadFileAsync(_options.LocalPath, _options.Source.RemotePath, overwriteExisting);

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

                return result.IsSuccess();
            }
        }

        private void ChangeFileExtensions(List<ChangeExtensionsOptions> options, List<FtpResult> results)
        {
            foreach (var file in results.Where(x => x.IsSuccess))
            {
                ChangeFileExtensions(options, file.LocalPath);
            }            
        }

        private void ChangeFileExtensions(List<ChangeExtensionsOptions> options, string localPath)
        {
            foreach (var item in options)
            {
                var targetFileName = @$"{_localDirectory}\{Path.GetFileNameWithoutExtension(localPath)}.{item.Target}";
                try
                {
                    File.Move(localPath, targetFileName, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Moving file {file} failed: {Message}", localPath, ex.Message);
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

                var result = await ftp.UploadFileAsync(_options.LocalPath, _options.Destination.RemotePath, overwriteExisting);

                if (_options.Destination.DeleteOnceTransferred)
                {
                    if (result.IsSuccess())
                    {
                        try
                        {
                            File.Delete(_options.LocalPath);
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

                await ValidateFtpResultList(results, ftp, _options.Destination.DeleteOnceTransferred);

                return results;
            }
        }

        private async Task ValidateFtpResultList(List<FtpResult> results, FtpClient ftp, bool deleteIfSuccess)
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
                else if (result.IsFailed)
                {
                    _logger.LogWarning("Transfer of {Name} failed, reason: {Exception}", result.Name, result.Exception);
                }
            }
        }

        private string EnsurePathIsFile(string toBeChecked, string targetPath)
        {
            string output = toBeChecked;

            if (!string.IsNullOrEmpty(Path.GetExtension(toBeChecked)))
            {
                var fileName = Path.GetFileName(targetPath);
                output = $"{toBeChecked}/{fileName}";
            }

            return output;
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
