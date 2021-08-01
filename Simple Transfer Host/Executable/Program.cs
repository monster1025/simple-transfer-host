using AgentFire.Lifetime.ConsoleApp;
using AgentFire.Lifetime.ConsoleServiceInstaller;
using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace STH.Executable
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                Console.WindowWidth = Math.Min(Console.LargestWindowWidth, Console.WindowWidth + 50);
                Console.WindowHeight = Math.Min(Console.LargestWindowHeight, Console.WindowHeight + 20);

                RunParameters p = new RunParameters
                {
                    AllowContinue = true,
                    SayGoodbye = true,
                };

                if (Controller.Run(ProjectInstaller.ServiceName, p, args.SingleOrDefault()))
                {
                    await CApp.LifetimeAsync(ProjectInstaller.ServiceName, Starter.GoAsync, Starter.ShutdownAsync).ConfigureAwait(false);
                }
            }
            else
            {
                ServiceHelper.SetLocalAsCurrentDirectory();

                using (WinService service = new WinService())
                {
                    ServiceBase.Run(service);
                }
            }
        }
    }
}
