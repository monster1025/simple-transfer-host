using AgentFire.Lifetime.Modules;
using Microsoft.Owin.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace STH.Modules
{
    internal sealed partial class WebServerModule : ModuleBase
    {
        public const ushort WebServerPort = 80;
        public const ushort WebServerSslPort = 443;
        private IDisposable _app;

        public static string HttpBinding { get; } = $"https://+:{WebServerSslPort}/simple-transfer-host/";

        protected override async Task StartInternal(CancellationToken token)
        {
            StartOptions options = new StartOptions();

            options.Urls.Add(HttpBinding);

            _app = await Task.Run(() => WebApp.Start<Startup>(options)).ConfigureAwait(false);
        }
        protected override Task StopInternal(CancellationToken token)
        {
            _app.Dispose();
            return Task.CompletedTask;
        }
    }
}
