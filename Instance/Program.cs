using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using SimpleTransferHost.Instance.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Instance
{
    class Program
    {
        private static void ConfigureHostConfiguration(IConfigurationBuilder cfg, IReadOnlyCollection<string> args)
        {
            cfg.SetBasePath(Directory.GetCurrentDirectory());
            cfg.AddEnvironmentVariables("host-config:");
            cfg.AddJsonFile("hostsettings.json", optional: false);
            cfg.AddCommandLine(args.ToArray());
        }
        private static void ConfigureAppConfiguration(IConfigurationBuilder cfg, IReadOnlyCollection<string> args, string applicationName, string environmentName)
        {
            cfg.SetBasePath(Directory.GetCurrentDirectory());
            cfg.AddJsonFile("appsettings.json", optional: false);
            cfg.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
            cfg.AddCommandLine(args.ToArray());
            cfg.AddEnvironmentVariables(prefix: $"{applicationName}:");
        }

        static async Task Main(string[] args)
        {
            #region Config

            ConfigurationBuilder configHostBuilder = new();
            ConfigureHostConfiguration(configHostBuilder, args);
            IConfigurationRoot hostConfig = configHostBuilder.Build();
            ConfigurationBuilder configAppBuilder = new();

            string environment = hostConfig.GetValue<string>("environment");
            string applicationName = hostConfig.GetValue<string>("applicationName");
            ConfigureAppConfiguration(configAppBuilder, args, environment, applicationName);

            IConfiguration _configuration = configAppBuilder.Build();

            #endregion
            #region Logger Factory

            var loggerFactory = new LoggerFactory();

            LogFactory nLogFactory = LogManager.Setup(builder =>
            {
                builder.LoadConfigurationFromSection(_configuration, "Logging:NLog");
            });

            var provider = new NLogLoggerProvider(new NLogProviderOptions(), nLogFactory);
            provider.Options.IncludeScopes = true;
            loggerFactory.AddProvider(provider);

            #endregion

            using IHost host = new HostBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureHostConfiguration(cfg => ConfigureHostConfiguration(cfg, args))
                .ConfigureAppConfiguration((hostContext, cfg) =>
                {
                    ConfigureAppConfiguration(cfg, args, hostContext.HostingEnvironment.ApplicationName, hostContext.HostingEnvironment.EnvironmentName);
                })
                .ConfigureContainer<ContainerBuilder>((hostContext, containerBuilder) =>
                {
                    containerBuilder
                        .RegisterInstance(loggerFactory)
                        .SingleInstance()
                        .ExternallyOwned();

                    containerBuilder
                        .RegisterInstance(hostContext.Configuration)
                        .SingleInstance();

                    containerBuilder.RegisterType<ConnectorService>().AsSelf().SingleInstance();
                    containerBuilder.RegisterType<WebServerHostedService>().As<IHostedService>().SingleInstance();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}
