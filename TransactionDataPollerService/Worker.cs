using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TransactionDataPollerService.Models;

namespace TransactionDataPollerService
{
    public class Worker : BackgroundService
    {
        private readonly List<TransferSettingsOptions> _options;
        private readonly IFtpWorker _ftpWorker;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;

        public Worker(ILogger<Worker> logger, IConfiguration config, List<TransferSettingsOptions> options,
            IFtpWorker ftpWorker)
        {
            _logger = logger;
            _config = config;
            _options = options;
            _ftpWorker = ftpWorker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Poll Frequency (ms): {frequency}", _config.GetValue<int>("PollFrequency"));

                foreach (var option in _options)
                {
                    _logger.LogInformation("Destination server: {destination}", option.Destination.Server);
                    _logger.LogInformation("Source server: {source}", option.Source.Server);
                    await _ftpWorker.RunAsync(option);
                }
                await Task.Delay(_config.GetValue<int>("PollFrequency"), stoppingToken);
            }
        }
    }
}
