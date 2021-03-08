using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using FtpEasyTransfer.Options;
using Serilog;
using Microsoft.Extensions.Hosting.WindowsServices;

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
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    services.Configure<List<TransferSettingsOptions>>(configuration.GetSection("TransferOptions"));
                    // List<TransferSettingsOptions> options = configuration.GetSection("TransferOptions").Get<List<TransferSettingsOptions>>();
                    // services.AddSingleton(options);
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
