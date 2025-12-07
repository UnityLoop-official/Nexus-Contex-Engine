import * as vscode from 'vscode';
import { DaemonClient } from './daemonClient';
import { ChatPanel } from './chatPanel';
import { LLMClient } from './llmClient';

/**
 * Provider for the Nexus Sidebar Chat View
 */
export class SidebarProvider implements vscode.WebviewViewProvider {
    private _view?: vscode.WebviewView;
    private readonly extensionUri: vscode.Uri;
    private readonly daemonClient: DaemonClient;
    private llmClient: LLMClient | null = null;

    constructor(extensionUri: vscode.Uri, daemonClient: DaemonClient) {
        this.extensionUri = extensionUri;
        this.daemonClient = daemonClient;
        this.initializeLLMClient();
    }

    private initializeLLMClient() {
        const config = vscode.workspace.getConfiguration('nexus');
        const apiKey = config.get<string>('openaiApiKey', '');

        if (!apiKey || apiKey.trim().length === 0) {
            console.warn('[Nexus Sidebar] OpenAI API key not configured');
            return;
        }

        this.llmClient = new LLMClient(apiKey);
        const model = config.get<string>('llmModel', 'gpt-5.1');
        this.llmClient.setModel(model);
    }

    public resolveWebviewView(
        webviewView: vscode.WebviewView,
        context: vscode.WebviewViewResolveContext,
        _token: vscode.CancellationToken,
    ) {
        this._view = webviewView;

        webviewView.webview.options = {
            // Allow scripts in the webview
            enableScripts: true,
            localResourceRoots: [
                this.extensionUri
            ]
        };

        // Set the HTML content
        webviewView.webview.html = ChatPanel.getHtmlContent(webviewView.webview, this.extensionUri);

        // Handle messages from the webview
        webviewView.webview.onDidReceiveMessage(async (message) => {
            await this.handleMessage(message);
        });
    }

    private async handleMessage(message: any) {
        if (message.type === 'sendMessage') {
            await this.handleSendMessage(message.text);
        }
    }

    private sendToWebview(message: any) {
        if (this._view) {
            this._view.webview.postMessage(message);
        }
    }

    // DUPLICATED LOGIC FROM ChatPanel - ideally this should be shared properly
    // For now, duplicate to get it working quickly without major refactor
    private async handleSendMessage(userMessage: string) {
        try {
            if (!this.llmClient) {
                this.sendToWebview({
                    type: 'error',
                    message: '⚠️ OpenAI API key not configured.'
                });
                return;
            }

            const workspaceFolders = vscode.workspace.workspaceFolders;
            if (!workspaceFolders || workspaceFolders.length === 0) {
                this.sendToWebview({
                    type: 'error',
                    message: '⚠️ No workspace folder open.'
                });
                return;
            }

            const workspacePath = workspaceFolders[0].uri.fsPath;
            const activeEditor = vscode.window.activeTextEditor;
            const activeFile = activeEditor ? activeEditor.document.fileName : '';

            this.sendToWebview({
                type: 'info',
                message: '🔄 Step 1/2: Compiling code context from workspace...'
            });

            const contextResponse = await this.daemonClient.compileContext(
                'Assistant',
                workspacePath,
                activeFile ? [activeFile] : []
            );

            this.sendToWebview({
                type: 'info',
                message: `✓ Context compiled: ${contextResponse.targets.length} nodes`
            });

            const config = vscode.workspace.getConfiguration('nexus');
            const enableStreaming = config.get<boolean>('enableStreaming', true);

            this.sendToWebview({
                type: 'info',
                message: '🤖 Step 2/2: Asking ChatGPT...'
            });

            if (enableStreaming) {
                let streamMessageId: string | null = null;
                await this.llmClient.chat(
                    userMessage,
                    contextResponse.bytecode,
                    (chunk: string) => {
                        this.sendToWebview({
                            type: 'llmChunk',
                            messageId: streamMessageId,
                            chunk: chunk
                        });
                        if (!streamMessageId) {
                            streamMessageId = Date.now().toString();
                        }
                    }
                );
                this.sendToWebview({
                    type: 'llmComplete',
                    messageId: streamMessageId
                });
            } else {
                const llmResponse = await this.llmClient.chat(
                    userMessage,
                    contextResponse.bytecode
                );
                this.sendToWebview({
                    type: 'llmResponse',
                    message: llmResponse
                });
            }

        } catch (error: any) {
            this.sendToWebview({
                type: 'error',
                message: `Error: ${error.message}`
            });
        }
    }
}
