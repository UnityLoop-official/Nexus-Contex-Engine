import * as vscode from 'vscode';
import { DaemonClient } from './daemonClient';
import { ChatPanel } from './chatPanel';
import { SidebarProvider } from './sidebarProvider';

let daemonClient: DaemonClient;

/**
 * Extension activation entry point
 */
export function activate(context: vscode.ExtensionContext) {
    console.log('[Nexus] Extension activating...');

    // Get daemon URL from settings
    const config = vscode.workspace.getConfiguration('nexus');
    const daemonUrl = config.get<string>('daemonUrl', 'http://localhost:5050');

    // Initialize daemon client
    daemonClient = new DaemonClient(daemonUrl);

    // Register command: Open Chat Panel
    const openChatCommand = vscode.commands.registerCommand('nexus.openChat', () => {
        ChatPanel.createOrShow(context.extensionUri, daemonClient);
    });

    // Register View Provider for Sidebar
    const sidebarProvider = new SidebarProvider(context.extensionUri, daemonClient);
    context.subscriptions.push(
        vscode.window.registerWebviewViewProvider('nexus.chatView', sidebarProvider)
    );

    // Register command: Compile Context for Selection
    const compileContextCommand = vscode.commands.registerCommand('nexus.compileContext', async () => {
        await compileContextForSelection();
    });

    // Add to subscriptions
    context.subscriptions.push(openChatCommand, compileContextCommand);

    // Check daemon connectivity on startup
    checkDaemonConnectivity();

    // Show welcome message
    vscode.window.showInformationMessage(
        '🚀 Nexus Context Engine activated! Use "Nexus: Open Chat Panel" to get started.'
    );

    console.log('[Nexus] Extension activated successfully');
}

/**
 * Compile context for current selection/file
 */
async function compileContextForSelection() {
    try {
        const editor = vscode.window.activeTextEditor;
        if (!editor) {
            vscode.window.showWarningMessage('No active editor. Please open a file first.');
            return;
        }

        const workspaceFolders = vscode.workspace.workspaceFolders;
        if (!workspaceFolders || workspaceFolders.length === 0) {
            vscode.window.showWarningMessage('No workspace folder open.');
            return;
        }

        const workspacePath = workspaceFolders[0].uri.fsPath;
        const activeFile = editor.document.fileName;

        // Show progress
        await vscode.window.withProgress(
            {
                location: vscode.ProgressLocation.Notification,
                title: 'Nexus: Compiling context...',
                cancellable: false
            },
            async (progress) => {
                progress.report({ increment: 0 });

                const response = await daemonClient.compileContext(
                    'Analyze',
                    workspacePath,
                    [activeFile]
                );

                progress.report({ increment: 100 });

                // Show result in output channel
                const outputChannel = vscode.window.createOutputChannel('Nexus Context');
                outputChannel.clear();
                outputChannel.appendLine('=== NEXUS CONTEXT OUTPUT ===');
                outputChannel.appendLine(`Summary: ${response.summary}`);
                outputChannel.appendLine(`Targets: ${response.targets.join(', ')}`);
                outputChannel.appendLine('');
                outputChannel.appendLine('=== DSL BYTECODE ===');
                outputChannel.appendLine(response.bytecode);
                outputChannel.show();

                vscode.window.showInformationMessage(
                    `✓ Context compiled: ${response.targets.length} nodes found`
                );
            }
        );
    } catch (error: any) {
        handleError(error);
    }
}

/**
 * Check if daemon is reachable
 */
async function checkDaemonConnectivity() {
    try {
        const isOnline = await daemonClient.ping();
        if (!isOnline) {
            vscode.window.showWarningMessage(
                `⚠️ Nexus Daemon not reachable at ${daemonClient.getBaseUrl()}. ` +
                'Please start it with: cd nexus-daemon/src/Nexus.Server && dotnet run',
                'Open Settings'
            ).then(selection => {
                if (selection === 'Open Settings') {
                    vscode.commands.executeCommand('workbench.action.openSettings', 'nexus.daemonUrl');
                }
            });
        } else {
            console.log('[Nexus] Daemon connectivity verified');
        }
    } catch (error) {
        console.warn('[Nexus] Daemon connectivity check failed:', error);
    }
}

/**
 * Handle errors with user-friendly messages
 */
function handleError(error: any) {
    let message = 'Nexus Error: ';

    if (error.code === 'ECONNREFUSED' || error.code === 'ETIMEDOUT') {
        message += `Cannot reach daemon at ${daemonClient.getBaseUrl()}. ` +
            'Ensure it is running with: dotnet run';
    } else if (error.response) {
        message += `HTTP ${error.response.status}: ${JSON.stringify(error.response.data)}`;
    } else {
        message += error.message || 'Unknown error occurred';
    }

    vscode.window.showErrorMessage(message);
    console.error('[Nexus] Error:', error);
}

/**
 * Extension deactivation
 */
export function deactivate() {
    console.log('[Nexus] Extension deactivated');
}
