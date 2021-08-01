using AgentFire.Lifetime.Modules;
using System.Threading.Tasks;

namespace STH
{
    public static class Starter
    {
        internal static ModuleManager ModuleManager { get; } = new ModuleManager();

        public static Task GoAsync()
        {
            return ModuleManager.Start();
        }
        public static Task ShutdownAsync()
        {
            return ModuleManager.Shutdown();
        }
    }
}
