using Microsoft.Owin.BuilderProperties;
using Ninject;
using Ninject.Web.WebApi;
using Owin;
using STH.Services;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Http;

namespace STH.Modules
{
    internal partial class WebServerModule
    {
        private sealed class Startup
        {
            // This code configures Web API. The Startup class is specified as a type
            // parameter in the WebApp.Start method.
            public void Configuration(IAppBuilder appBuilder)
            {
                UseWebApi(appBuilder);
            }

            private static IKernel CreateKernel()
            {
                var kernel = new StandardKernel();

                kernel.Load<WebApiModule>();
                kernel.Bind<ConnectorService>().ToSelf().InSingletonScope();

                return kernel;
            }

            private static void UseWebApi(IAppBuilder appBuilder)
            {
                // Configure Web API for self-host. 
                HttpConfiguration config = new HttpConfiguration();

                config.MapHttpAttributeRoutes();

                // Enable JSON serialization for outgoing objects.
                config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

                appBuilder.UseNinject(CreateKernel);
                appBuilder.UseNinjectWebApi(config);

                config.EnsureInitialized();

                AppProperties properties = new AppProperties(appBuilder.Properties);
                CancellationToken token = properties.OnAppDisposing;

                if (token != CancellationToken.None)
                {
                    token.Register(() => config.Dispose());
                }
            }
        }
    }
}
