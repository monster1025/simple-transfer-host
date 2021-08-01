using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleTransferHost.Instance.Services
{
    public sealed class WebServerHostedService : IHostedService
    {
        private IWebHost _host = null!;

        public async Task StartAsync(CancellationToken ct)
        {
            _host = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseRouting();

                    app.UsePathBase("simple-transfer-host");
                    app.UseResponseCompression();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                })
                .UseKestrel((hostBuilder, kestrelOptions) =>
                {
                    kestrelOptions.ListenAnyIP(443);
                })
                .Build();

            await _host.RunAsync(ct);
        }

        public async Task StopAsync(CancellationToken ct)
        {
            await _host.StopAsync(ct);

            _host.Dispose();
        }
    }
}
