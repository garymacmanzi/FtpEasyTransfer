using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FtpEasyTransfer.Options;
using FluentFTP;
using Serilog;

namespace FtpEasyTransfer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    List<TransferSettingsOptions> options = configuration.GetSection("TransferOptions").Get<List<TransferSettingsOptions>>();
                    services.AddSingleton(options);
                    services.AddTransient<IFtpWorker, FtpWorker>();

                    services.AddHostedService<Worker>();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    IConfiguration config = hostContext.Configuration;
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(config)
                        .CreateLogger();

                    logging.AddSerilog();
                });

            return hostBuilder;
        }
    }
}
