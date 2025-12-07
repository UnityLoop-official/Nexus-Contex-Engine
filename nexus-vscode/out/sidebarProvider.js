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
exports.SidebarProvider = void 0;
const vscode = __importStar(require("vscode"));
const chatPanel_1 = require("./chatPanel");
const llmClient_1 = require("./llmClient");
/**
 * Provider for the Nexus Sidebar Chat View
 */
class SidebarProvider {
    constructor(extensionUri, daemonClient) {
        this.llmClient = null;
        this.extensionUri = extensionUri;
        this.daemonClient = daemonClient;
        this.initializeLLMClient();
    }
    initializeLLMClient() {
        const config = vscode.workspace.getConfiguration('nexus');
        const apiKey = config.get('openaiApiKey', '');
        if (!apiKey || apiKey.trim().length === 0) {
            console.warn('[Nexus Sidebar] OpenAI API key not configured');
            return;
        }
        this.llmClient = new llmClient_1.LLMClient(apiKey);
        const model = config.get('llmModel', 'gpt-5.1');
        this.llmClient.setModel(model);
    }
    resolveWebviewView(webviewView, context, _token) {
        this._view = webviewView;
        webviewView.webview.options = {
            // Allow scripts in the webview
            enableScripts: true,
            localResourceRoots: [
                this.extensionUri
            ]
        };
        // Set the HTML content
        webviewView.webview.html = chatPanel_1.ChatPanel.getHtmlContent(webviewView.webview, this.extensionUri);
        // Handle messages from the webview
        webviewView.webview.onDidReceiveMessage(async (message) => {
            await this.handleMessage(message);
        });
    }
    async handleMessage(message) {
        if (message.type === 'sendMessage') {
            await this.handleSendMessage(message.text);
        }
    }
    sendToWebview(message) {
        if (this._view) {
            this._view.webview.postMessage(message);
        }
    }
    // DUPLICATED LOGIC FROM ChatPanel - ideally this should be shared properly
    // For now, duplicate to get it working quickly without major refactor
    async handleSendMessage(userMessage) {
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
            const contextResponse = await this.daemonClient.compileContext('Assistant', workspacePath, activeFile ? [activeFile] : []);
            this.sendToWebview({
                type: 'info',
                message: `✓ Context compiled: ${contextResponse.targets.length} nodes`
            });
            const config = vscode.workspace.getConfiguration('nexus');
            const enableStreaming = config.get('enableStreaming', true);
            this.sendToWebview({
                type: 'info',
                message: '🤖 Step 2/2: Asking ChatGPT...'
            });
            if (enableStreaming) {
                let streamMessageId = null;
                await this.llmClient.chat(userMessage, contextResponse.bytecode, (chunk) => {
                    this.sendToWebview({
                        type: 'llmChunk',
                        messageId: streamMessageId,
                        chunk: chunk
                    });
                    if (!streamMessageId) {
                        streamMessageId = Date.now().toString();
                    }
                });
                this.sendToWebview({
                    type: 'llmComplete',
                    messageId: streamMessageId
                });
            }
            else {
                const llmResponse = await this.llmClient.chat(userMessage, contextResponse.bytecode);
                this.sendToWebview({
                    type: 'llmResponse',
                    message: llmResponse
                });
            }
        }
        catch (error) {
            this.sendToWebview({
                type: 'error',
                message: `Error: ${error.message}`
            });
        }
    }
}
exports.SidebarProvider = SidebarProvider;
//# sourceMappingURL=sidebarProvider.js.map