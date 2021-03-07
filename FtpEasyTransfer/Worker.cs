using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FtpEasyTransfer.Options;
using FtpEasyTransfer.Helpers;

namespace FtpEasyTransfer
{
    public class Worker : BackgroundService
    {
        private readonly List<TransferSettingsOptions> _options;
        private readonly IFtpWorker _ftpWorker;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;

        public Worker(ILogger<Worker> logger, IConfiguration config, List<TransferSettingsOptions> options,
            IFtpWorker ftpWorker, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _config = config;
            _options = options;
            _ftpWorker = ftpWorker;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ValidateSettings();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Poll Frequency (ms): {frequency}", _config.GetValue<int>("PollFrequency"));

                foreach (var option in _options)
                {
                    await _ftpWorker.RunAsync(option);
                }
                await Task.Delay(_config.GetValue<int>("PollFrequency"), stoppingToken);
            }
        }

        private void ValidateSettings()
        {
            foreach (var item in _options)
            {
                if (string.IsNullOrWhiteSpace(item.LocalPath))
                {
                    _logger.LogCritical("LocalDirectory not set. Check appsettings.json");
                    _hostApplicationLifetime.StopApplication();
                };

                List<string> normalisedExtensions = new List<string>();

                if (item.Source is not null)
                {
                    foreach (var fileType in item.Source.FileTypesToDownload)
                    {
                        normalisedExtensions.Add(fileType.Normalise());
                    }

                    item.Source.FileTypesToDownload = normalisedExtensions;
                }

                foreach (var ext in item.ChangeExtensions)
                {
                    if (string.IsNullOrWhiteSpace(ext.Source) || string.IsNullOrWhiteSpace(ext.Target))
                    {
                        _logger.LogCritical("Invalid or empty extension source/target in \"ChangeExtensions\". Check appsettings.json");
                        _hostApplicationLifetime.StopApplication();
                    }
                }
            }
        }
    }
}
