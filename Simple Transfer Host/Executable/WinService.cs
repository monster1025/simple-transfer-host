using Microsoft.VisualStudio.Threading;
using System.ServiceProcess;

namespace STH.Executable
{
    partial class WinService : ServiceBase
    {
        public WinService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            using (JoinableTaskContext ctx = new JoinableTaskContext())
            {
                new JoinableTaskFactory(ctx).Run(Starter.GoAsync);
            }
        }
        protected override void OnStop()
        {
            using (JoinableTaskContext ctx = new JoinableTaskContext())
            {
                new JoinableTaskFactory(ctx).Run(Starter.ShutdownAsync);
            }
        }
    }
}
