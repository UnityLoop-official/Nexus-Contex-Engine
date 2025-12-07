"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.ChatPanel = void 0;
const vscode = __importStar(require("vscode"));
const llmClient_1 = require("./llmClient");
/**
 * Manages the chat webview panel with LLM integration
 */
class ChatPanel {
    constructor(panel, extensionUri, daemonClient) {
        this.disposables = [];
        this.llmClient = null;
        this.panel = panel;
        this.extensionUri = extensionUri;
        this.daemonClient = daemonClient;
        // Initialize LLM client if API key is configured
        this.initializeLLMClient();
        // Set the webview's HTML content
        this.panel.webview.html = ChatPanel.getHtmlContent(this.panel.webview, extensionUri);
        // Handle messages from the webview
        this.panel.webview.onDidReceiveMessage(message => this.handleMessage(message), null, this.disposables);
        // Handle panel disposal
        this.panel.onDidDispose(() => this.dispose(), null, this.disposables);
    }
    initializeLLMClient() {
        const config = vscode.workspace.getConfiguration('nexus');
        const apiKey = config.get('openaiApiKey', '');
        if (!apiKey || apiKey.trim().length === 0) {
            vscode.window.showWarningMessage('⚠️ Nexus: OpenAI API key not configured. Please set it in settings.', 'Open Settings').then(selection => {
                if (selection === 'Open Settings') {
                    vscode.commands.executeCommand('workbench.action.openSettings', 'nexus.openaiApiKey');
                }
            });
            return;
        }
        this.llmClient = new llmClient_1.LLMClient(apiKey);
        const model = config.get('llmModel', 'gpt-5.1');
        this.llmClient.setModel(model);
    }
    static createOrShow(extensionUri, daemonClient) {
        const column = vscode.window.activeTextEditor
            ? vscode.window.activeTextEditor.viewColumn
            : undefined;
        // If we already have a panel, show it
        if (ChatPanel.currentPanel) {
            ChatPanel.currentPanel.panel.reveal(column);
            // Reinitialize LLM client in case settings changed
            ChatPanel.currentPanel.initializeLLMClient();
            return;
        }
        // Otherwise, create a new panel
        const panel = vscode.window.createWebviewPanel('nexusChat', 'Nexus AI Chat', column || vscode.ViewColumn.One, {
            enableScripts: true,
            retainContextWhenHidden: true,
            localResourceRoots: [vscode.Uri.joinPath(extensionUri, 'resources')]
        });
        ChatPanel.currentPanel = new ChatPanel(panel, extensionUri, daemonClient);
    }
    async handleMessage(message) {
        switch (message.type) {
            case 'sendMessage':
                await this.handleSendMessage(message.text);
                break;
        }
    }
    async handleSendMessage(userMessage) {
        try {
            // Check if LLM is configured
            if (!this.llmClient) {
                this.sendToWebview({
                    type: 'error',
                    message: '⚠️ OpenAI API key not configured.\n\nPlease set your API key in:\nSettings → Extensions → Nexus Context Engine → OpenAI API Key'
                });
                return;
            }
            // Get workspace folder
            const workspaceFolders = vscode.workspace.workspaceFolders;
            if (!workspaceFolders || workspaceFolders.length === 0) {
                this.sendToWebview({
                    type: 'error',
                    message: '⚠️ No workspace folder open. Please open a folder first.'
                });
                return;
            }
            const workspacePath = workspaceFolders[0].uri.fsPath;
            // Get active file (if any)
            const activeEditor = vscode.window.activeTextEditor;
            const activeFile = activeEditor ? activeEditor.document.fileName : '';
            // Step 1: Compile context from daemon
            this.sendToWebview({
                type: 'info',
                message: '🔄 Step 1/2: Compiling code context from workspace...'
            });
            const contextResponse = await this.daemonClient.compileContext('Assistant', workspacePath, activeFile ? [activeFile] : []);
            this.sendToWebview({
                type: 'info',
                message: `✓ Context compiled: ${contextResponse.targets.length} nodes, ${contextResponse.bytecode.length} chars`
            });
            // Step 2: Send to LLM with streaming
            const config = vscode.workspace.getConfiguration('nexus');
            const enableStreaming = config.get('enableStreaming', true);
            this.sendToWebview({
                type: 'info',
                message: '🤖 Step 2/2: Asking ChatGPT...'
            });
            if (enableStreaming) {
                // Streaming response
                let streamMessageId = null;
                await this.llmClient.chat(userMessage, contextResponse.bytecode, (chunk) => {
                    // Send chunk to webview for real-time display
                    this.sendToWebview({
                        type: 'llmChunk',
                        messageId: streamMessageId,
                        chunk: chunk
                    });
                    // Store message ID for subsequent chunks
                    if (!streamMessageId) {
                        streamMessageId = Date.now().toString();
                    }
                });
                // Mark streaming complete
                this.sendToWebview({
                    type: 'llmComplete',
                    messageId: streamMessageId
                });
            }
            else {
                // Non-streaming response
                const llmResponse = await this.llmClient.chat(userMessage, contextResponse.bytecode);
                this.sendToWebview({
                    type: 'llmResponse',
                    message: llmResponse
                });
            }
        }
        catch (error) {
            let errorMessage = '⚠️ Error: ';
            if (error.code === 'ECONNREFUSED' || error.code === 'ETIMEDOUT') {
                errorMessage += 'Cannot reach Nexus Daemon.\n';
                errorMessage += `   Please ensure daemon is running at ${this.daemonClient.getBaseUrl()}\n`;
                errorMessage += '   Run: cd nexus-daemon/src/Nexus.Server && dotnet run';
            }
            else if (error.status === 401) {
                errorMessage += 'Invalid OpenAI API key.\n';
                errorMessage += '   Please check your API key in settings.';
            }
            else if (error.status === 429) {
                errorMessage += 'Rate limit exceeded or quota reached.\n';
                errorMessage += '   Please check your OpenAI account.';
            }
            else if (error.response) {
                errorMessage += `HTTP ${error.response.status}: ${error.response.data}`;
            }
            else {
                errorMessage += error.message || 'Unknown error';
            }
            this.sendToWebview({
                type: 'error',
                message: errorMessage
            });
            // Log to console for debugging
            console.error('[Nexus] Error:', error);
        }
    }
    sendToWebview(message) {
        this.panel.webview.postMessage(message);
    }
    static getHtmlContent(webview, extensionUri) {
        return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Nexus AI Chat</title>
    <style>
        body {
            font-family: var(--vscode-font-family);
            color: var(--vscode-foreground);
            background-color: var(--vscode-editor-background);
            padding: 0;
            margin: 0;
            display: flex;
            flex-direction: column;
            height: 100vh;
        }
        #chat-container {
            flex: 1;
            overflow-y: auto;
            padding: 16px;
            display: flex;
            flex-direction: column;
            gap: 12px;
        }
        .message {
            padding: 12px;
            border-radius: 4px;
            max-width: 90%;
            word-wrap: break-word;
            white-space: pre-wrap;
            line-height: 1.6;
        }
        .message.user {
            background-color: var(--vscode-input-background);
            align-self: flex-end;
            border: 1px solid var(--vscode-input-border);
        }
        .message.assistant {
            background-color: var(--vscode-editor-inactiveSelectionBackground);
            align-self: flex-start;
            border-left: 3px solid var(--vscode-button-background);
        }
        .message.system {
            background-color: var(--vscode-editor-inactiveSelectionBackground);
            align-self: flex-start;
            font-size: 0.9em;
            opacity: 0.9;
        }
        .message.error {
            background-color: var(--vscode-inputValidation-errorBackground);
            border: 1px solid var(--vscode-inputValidation-errorBorder);
            align-self: flex-start;
        }
        .message.info {
            background-color: var(--vscode-inputValidation-infoBackground);
            border: 1px solid var(--vscode-inputValidation-infoBorder);
            align-self: flex-start;
            font-size: 0.9em;
        }
        #input-container {
            display: flex;
            padding: 16px;
            gap: 8px;
            border-top: 1px solid var(--vscode-panel-border);
            background-color: var(--vscode-sideBar-background);
        }
        #message-input {
            flex: 1;
            padding: 8px;
            background-color: var(--vscode-input-background);
            color: var(--vscode-input-foreground);
            border: 1px solid var(--vscode-input-border);
            border-radius: 4px;
            font-family: var(--vscode-font-family);
        }
        #send-button {
            padding: 8px 16px;
            background-color: var(--vscode-button-background);
            color: var(--vscode-button-foreground);
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-family: var(--vscode-font-family);
        }
        #send-button:hover {
            background-color: var(--vscode-button-hoverBackground);
        }
        #send-button:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }
        .typing-indicator {
            display: inline-block;
            animation: blink 1.4s infinite;
        }
        @keyframes blink {
            0%, 50%, 100% { opacity: 1; }
            25%, 75% { opacity: 0.3; }
        }
    </style>
</head>
<body>
    <div id="chat-container"></div>
    <div id="input-container">
        <input type="text" id="message-input" placeholder="Ask me anything about your code..." />
        <button id="send-button">Send</button>
    </div>

    <script>
        const vscode = acquireVsCodeApi();
        const chatContainer = document.getElementById('chat-container');
        const messageInput = document.getElementById('message-input');
        const sendButton = document.getElementById('send-button');
        let streamingMessages = {};

        function addMessage(type, content) {
            const messageDiv = document.createElement('div');
            messageDiv.className = \`message \${type}\`;
            messageDiv.textContent = content;
            chatContainer.appendChild(messageDiv);
            chatContainer.scrollTop = chatContainer.scrollHeight;
            return messageDiv;
        }

        function sendMessage() {
            const text = messageInput.value.trim();
            if (!text) return;

            // Add user message to chat
            addMessage('user', text);

            // Send to extension
            vscode.postMessage({
                type: 'sendMessage',
                text: text
            });

            // Clear input and disable
            messageInput.value = '';
            sendButton.disabled = true;
        }

        sendButton.addEventListener('click', sendMessage);
        messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        // Handle messages from extension
        window.addEventListener('message', event => {
            const message = event.data;

            switch (message.type) {
                case 'error':
                    addMessage('error', message.message);
                    sendButton.disabled = false;
                    break;

                case 'info':
                    addMessage('info', message.message);
                    break;

                case 'llmChunk':
                    // Streaming chunk received
                    const messageId = message.messageId || 'stream';
                    if (!streamingMessages[messageId]) {
                        streamingMessages[messageId] = addMessage('assistant', '');
                    }
                    streamingMessages[messageId].textContent += message.chunk;
                    chatContainer.scrollTop = chatContainer.scrollHeight;
                    break;

                case 'llmComplete':
                    // Streaming complete
                    delete streamingMessages[message.messageId || 'stream'];
                    sendButton.disabled = false;
                    break;

                case 'llmResponse':
                    // Non-streaming response
                    addMessage('assistant', message.message);
                    sendButton.disabled = false;
                    break;
            }
        });

        // Welcome message
        addMessage('system', '🤖 Nexus AI Assistant\\n\\nPowered by ChatGPT with full codebase context.\\nAsk questions, request refactoring, or explore your architecture!');
    </script>
</body>
</html>`;
    }
    dispose() {
        ChatPanel.currentPanel = undefined;
        this.panel.dispose();
        while (this.disposables.length) {
            const disposable = this.disposables.pop();
            if (disposable) {
                disposable.dispose();
            }
        }
    }
}
exports.ChatPanel = ChatPanel;
//# sourceMappingURL=chatPanel.js.map