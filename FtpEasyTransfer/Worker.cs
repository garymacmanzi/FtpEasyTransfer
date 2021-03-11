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
using Microsoft.Extensions.Options;

namespace FtpEasyTransfer
{
    public class Worker : BackgroundService
    {
        private readonly List<TransferSettingsOptions> _options;
        private readonly IFtpWorker _ftpWorker;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IRunModeRetriever _modeRetriever;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;

        public Worker(ILogger<Worker> logger, IConfiguration config, IOptions<List<TransferSettingsOptions>> options,
            IFtpWorker ftpWorker, IHostApplicationLifetime hostApplicationLifetime, IRunModeRetriever modeRetriever)
        {
            _logger = logger;
            _config = config;
            _options = options.Value;
            _ftpWorker = ftpWorker;
            _hostApplicationLifetime = hostApplicationLifetime;
            _modeRetriever = modeRetriever;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service at: {time}", DateTimeOffset.Now);
            ValidateSettings();
            _logger.LogInformation("Settings validated");
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Poll Frequency (ms): {frequency}", _config.GetValue<int>("PollFrequency"));

                _logger.LogInformation("Running through options: {Count}, LocalPath of 0: {LocalPath}", _options.Count, _options[0].LocalPath);
                foreach (var option in _options)
                {
                    await _ftpWorker.RunAsync(option);
                }
                await Task.Delay(_config.GetValue<int>("PollFrequency"), stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopped at: {time}", DateTimeOffset.Now);

            return base.StopAsync(cancellationToken);
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
    }
}
