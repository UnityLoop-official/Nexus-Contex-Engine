using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NexusVS.ToolWindow;
using Task = System.Threading.Tasks.Task;

namespace NexusVS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("12345678-1234-1234-1234-123456789012")] // Matches the ID in vsixmanifest roughly (needs sync if strict)
    [ProvideToolWindow(typeof(NexusChatToolWindow))]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class NexusVSPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            // Initialization code here
        }

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (toolWindowType == typeof(NexusChatToolWindow).GUID)
            {
                return this;
            }

            return base.GetAsyncToolWindowFactory(toolWindowType);
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            if (toolWindowType == typeof(NexusChatToolWindow))
            {
                return "Nexus Dev Chat";
            }

            return base.GetToolWindowTitle(toolWindowType, id);
        }
    }
}
