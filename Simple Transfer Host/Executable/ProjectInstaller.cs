using AgentFire.Lifetime.ConsoleServiceInstaller;
using STH.Modules;
using System.ComponentModel;

namespace STH
{
    [RunInstaller(true)]
    public sealed partial class ProjectInstaller : SmartNetworkServiceInstaller
    {
        public const string ServiceName = "Simple Transfer Host";

        public ProjectInstaller() : base(ServiceName, WebServerModule.HttpBinding) { }
    }
}
