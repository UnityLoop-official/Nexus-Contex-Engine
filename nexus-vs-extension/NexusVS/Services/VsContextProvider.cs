using System;
using System.IO;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

namespace NexusVS.Services
{
    public class VsContextProvider
    {
        private DTE2 _dte;

        public VsContextProvider()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
        }

        public string GetSolutionPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_dte != null && _dte.Solution != null && _dte.Solution.IsOpen)
            {
                return Path.GetDirectoryName(_dte.Solution.FullName);
            }
            return string.Empty;
        }

        public string GetActiveFile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_dte != null && _dte.ActiveDocument != null)
            {
                return _dte.ActiveDocument.FullName; // Or .Name for just filename
            }
            return string.Empty;
        }

        public string GetSelectedText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_dte != null && _dte.ActiveDocument != null)
            {
                var selection = _dte.ActiveDocument.Selection as TextSelection;
                if (selection != null)
                {
                    return selection.Text;
                }
            }
            return string.Empty;
        }
    }
}
