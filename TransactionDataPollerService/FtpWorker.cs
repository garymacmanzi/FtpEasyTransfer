using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FtpEasyTransfer.Options;
using System.Net;
using FluentFTP.Rules;
using System.Threading;
using System.Diagnostics;
using System.IO;

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

            switch (DetermineRunMode())
            {
                case RunMode.DownloadDir:
                    await RunDownloadDirAsync();
                    break;
                case RunMode.DownloadFile:
                    await RunDownloadFileAsync();
                    break;
                case RunMode.UploadDir:
                    await RunUploadDirAsync();
                    break;
                case RunMode.UploadFile:
                    await RunUploadFileAsync();
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

        private Task RunDownloadDirAsync()
        {
            throw new NotImplementedException();
        }

        private Task RunDownloadFileAsync()
        {
            throw new NotImplementedException();
        }

        private Task RunUploadDirAsync()
        {
            throw new NotImplementedException();
        }

        private async Task RunUploadFileAsync()
        {
            if (_options.Destination != null && !string.IsNullOrWhiteSpace(_options.Destination.Server))
            {
                try
                {
                    await UploadFileToDestinationAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception in RunUploadFileAsync: {Message}", ex.Message);
                }
            }
        }

        private async Task RunSyncDirsAsync()
        {
            if (_options.Source != null || !string.IsNullOrWhiteSpace(_options.Source.Server))
            {
                try
                {
                    await DownloadDirectoryFromSourceAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception in DownloadFromSource: {Message}", ex.Message);
                }
            }
            else
            {
                _logger.LogDebug("No source configured.");
            }

            foreach (var opt in _options.ChangeExtensions)
            {
                ChangeFileExtensions(opt);
            }

            if (_options.Destination != null || !string.IsNullOrWhiteSpace(_options.Destination.Server))
            {
                try
                {
                    await UploadDirectoryToDestinationAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception in UploadToDestination: {Message}", ex.Message);
                }
            }
            else
            {
                _logger.LogDebug("No destination configured.");
            }
        }

        private Task RunSyncFileAsync()
        {
            throw new NotImplementedException();
        }

        private async Task<List<FtpResult>> DownloadDirectoryFromSourceAsync()
        {
            var token = new CancellationToken();


            using (var ftp = new FtpClient(_options.Source.Server, _options.Source.Port, _options.Source.User, _options.Source.Password))
            {
                ftp.OnLogEvent += Log;

                await ftp.ConnectAsync(token);

                var rules = new List<FtpRule>
                {
                    new FtpFileExtensionRule(true, _options.Source.FileTypesToDownload)
                };

                var results = await ftp.DownloadDirectoryAsync(_options.LocalPath, _options.Source.RemotePath, FtpFolderSyncMode.Update,
                    FtpLocalExists.Skip, FtpVerify.None, rules);

                if (_options.Source.DeleteOnceDownloaded)
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

        private void ChangeFileExtensions(ChangeExtensionsOptions options)
        {
            foreach (var file in Directory.GetFiles(_localDirectory, $"*.{options.Source}"))
            {
                var newFileName = @$"{_localDirectory}\{Path.GetFileNameWithoutExtension(file)}.{options.Target}";
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

        private async Task<FtpStatus> UploadFileToDestinationAsync()
        {
            var token = new CancellationToken();

            using (var ftp = new FtpClient(_options.Destination.Server, _options.Destination.Port, _options.Destination.User, _options.Destination.Password))
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

                if (_options.Destination.DeleteOnceUploaded)
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

            using (var ftp = new FtpClient(_options.Destination.Server, _options.Destination.Port, _options.Destination.User, _options.Destination.Password))
            {
                ftp.OnLogEvent += Log;

                await ftp.ConnectAsync(token);

                var results = await ftp.UploadDirectoryAsync(_options.LocalPath, _options.Destination.RemotePath, FtpFolderSyncMode.Update,
                    FtpRemoteExists.Skip, FtpVerify.None);

                if (_options.Destination.DeleteOnceUploaded)
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

        private RunMode DetermineRunMode()
        {
            if (_options.LocalPathIsFile)
            {
                _logger.LogDebug("Local Path: {LocalPath} is file, RunMode determined as UploadFile", _options.LocalPath);
                return RunMode.UploadFile;
            }
            else if (_options.Source is not null && _options.Destination is not null)
            {
                if (_options.Source.RemotePathIsFile)
                {
                    _logger.LogDebug("Source & Destination defined, Source.RemotePath: {RemotePath} is file, RunMode determined as SyncFile", _options.Source.RemotePath);
                    return RunMode.SyncFile;
                }
                else
                {
                    _logger.LogDebug("Source & Destination defined, Source.RemotePath: {RemotePath} is directory, RunMode determined as SyncDirs", _options.Source.RemotePath);
                    return RunMode.SyncDirs;
                }
            }
            else if (_options.Source is null && _options.Destination is not null)
            {
                if (_options.Destination.RemotePathIsFile)
                {
                    _logger.LogDebug("Only Destination defined, Destination.RemotePath: {RemotePath} is file, RunMode determined as UploadFile", _options.Destination.RemotePath);
                    return RunMode.UploadFile;
                }
                else
                {
                    _logger.LogDebug("Only Destination defined, Destination.RemotePath: {RemotePath} is directory, RunMode determined as UploadDir", _options.Destination.RemotePath);
                    return RunMode.UploadDir;
                }
            }
            else
            {
                if (_options.Source.RemotePathIsFile)
                {
                    _logger.LogDebug("Only Source defined, Source.RemotePath: {RemotePath} is file, RunMode determined as DownloadFile", _options.Source.RemotePath);
                    return RunMode.DownloadFile;
                }
                else
                {
                    _logger.LogDebug("Only Source defined, Source.RemotePath: {RemotePath} is directory, RunMode determined as DownloadDir", _options.Source.RemotePath);
                    return RunMode.DownloadDir;
                }
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

        private enum RunMode
        {
            DownloadDir,
            DownloadFile,
            UploadDir,
            UploadFile,
            SyncDirs,
            SyncFile
        }
    }
}
