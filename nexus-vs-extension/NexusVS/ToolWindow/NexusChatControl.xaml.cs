using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using NexusVS.Services;

namespace NexusVS.ToolWindow
{
    public partial class NexusChatControl : UserControl
    {
        private readonly DaemonClient _client;
        private readonly VsContextProvider _contextProvider;

        public NexusChatControl()
        {
            InitializeComponent();
            _client = new DaemonClient();
            _contextProvider = new VsContextProvider();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var message = InputText.Text;
            if (string.IsNullOrWhiteSpace(message)) return;

            AppendText($"Me: {message}");
            InputText.Clear();
            SendButton.IsEnabled = false;

            try
            {
                // 1. Gather Context
                var solutionPath = _contextProvider.GetSolutionPath();
                var activeFile = _contextProvider.GetActiveFile();

                // Validate we have a solution loaded
                if (string.IsNullOrEmpty(solutionPath))
                {
                    AppendText("⚠️ Error: No solution is currently open. Please open a solution first.");
                    return;
                }

                // 2. Call Daemon to compile context
                AppendText("System: Compiling context...");

                var context = await _client.CompileContextAsync("Refactor", solutionPath, new[] { activeFile });

                AppendText($"✓ Nexus Daemon: Context compiled ({context.Length} chars).");
                AppendText("Preview: " + context.Substring(0, Math.Min(context.Length, 200)) + "...");
            }
            catch (HttpRequestException httpEx)
            {
                // Network/connection errors - daemon not reachable
                AppendText("⚠️ Connection Error: Cannot reach Nexus Daemon.");
                AppendText("   Please ensure the 'Nexus Context Daemon' is running.");
                AppendText("   Run command: 'dotnet run' in nexus-daemon/src/Nexus.Server");
                AppendText($"   Expected URL: {DaemonClient.BaseUrl}"); 
                // Note: DaemonClient.BaseUrl would need to be public or we just hardcode 5050 here for message consistency
                
                // Log detailed error for debugging
                System.Diagnostics.Debug.WriteLine($"[NexusVS] HTTP Error: {httpEx}");
            }
            catch (InvalidOperationException invalidEx)
            {
                // Invalid operation - usually configuration or state issues
                AppendText("⚠️ Configuration Error: Invalid operation.");
                AppendText($"   Details: {invalidEx.Message}");

                System.Diagnostics.Debug.WriteLine($"[NexusVS] Invalid Operation: {invalidEx}");
            }
            catch (Exception ex)
            {
                // Catch-all for unexpected errors
                AppendText("⚠️ Unexpected Error: Something went wrong.");
                AppendText($"   Error type: {ex.GetType().Name}");
                AppendText($"   Message: {ex.Message}");

                // Full stack trace to debug output for developers
                System.Diagnostics.Debug.WriteLine($"[NexusVS] Unexpected Error: {ex}");
            }
            finally
            {
                SendButton.IsEnabled = true;
            }
        }

        private void AppendText(string text)
        {
            OutputText.Text += $"{text}\n\n";
            OutputText.ScrollToEnd();
        }
    }
}
