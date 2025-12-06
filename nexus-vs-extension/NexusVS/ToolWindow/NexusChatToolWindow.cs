using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace NexusVS.ToolWindow
{
    [Guid("e567104b-7f15-4632-88e8-b9716f4ad948")]
    public class NexusChatToolWindow : ToolWindowPane
    {
        public NexusChatToolWindow() : base(null)
        {
            this.Caption = "Nexus Dev Chat";
            this.Content = new NexusChatControl();
        }
    }
}
