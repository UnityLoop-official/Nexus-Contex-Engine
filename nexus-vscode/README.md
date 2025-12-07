# Nexus Context Engine - VS Code Extension

🤖 **AI-powered code assistant** with ChatGPT 5.1 integration and automatic context extraction.

## Features

- 🧠 **ChatGPT 5.1 Integration**: Full codebase context sent to latest AI model
- 💬 **Interactive Chat Panel**: Real-time streaming responses
- 🎯 **Smart Context Compilation**: Extracts architectural rules, code nodes, and dependencies
- ⚡ **Performance**: Cached indexing with 5-minute TTL
- 🔒 **Secure**: Path traversal protection and encrypted API key storage
- 📊 **Multi-Model Support**: ChatGPT 5.1, GPT-4o, GPT-4 Turbo, and more

## Prerequisites

1. **.NET 9 SDK** - Required to run the Nexus Daemon
2. **OpenAI API Key** - Get one at https://platform.openai.com/api-keys
3. **Nexus Daemon** - Backend service that indexes and compiles code context

## Quick Start

### 1. Start the Nexus Daemon

```bash
cd nexus-daemon/src/Nexus.Server
dotnet run
```

✅ Verify at: http://localhost:5050/swagger

### 2. Configure OpenAI API Key

1. Open this folder (`nexus-vscode`) in VS Code
2. Press `F5` to launch Extension Development Host
3. In the new window:
   - `Ctrl+Shift+P` → "Preferences: Open Settings"
   - Search for: **"nexus openai"**
   - Paste your API key

### 3. Start Chatting!

1. Open a workspace folder (e.g., `nexus-daemon`)
2. `Ctrl+Shift+P` → **"Nexus: Open Chat Panel"**
3. Ask questions like:
   - "What services exist in this codebase?"
   - "Explain how the CodeIndexer works"
   - "Suggest improvements to performance"

## Configuration

Access settings via `File → Preferences → Settings → Extensions → Nexus Context Engine`

| Setting | Default | Description |
|---------|---------|-------------|
| **OpenAI API Key** | (empty) | Your ChatGPT API key (required) |
| **LLM Model** | `gpt-5.1` | Model: gpt-5.1, gpt-4o, gpt-4o-mini, gpt-4-turbo, gpt-3.5-turbo |
| **Enable Streaming** | `true` | Real-time streaming responses |
| **Daemon URL** | `http://localhost:5050` | Nexus Daemon backend address |
| **Auto Start** | `false` | Auto-start daemon on VS Code launch |

## How It Works

### Conversation Flow

```
User Question
    ↓
[1/2] Compile Context
    → Nexus Daemon indexes workspace
    → Generates DSL with @RULE, @NODE, @DECISION
    ↓
[2/2] Ask ChatGPT 5.1
    → Full context + question sent to AI
    → Streaming response displayed
    ↓
AI Answer (Context-Aware)
```

### Architecture

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│             │  HTTP   │              │  HTTP   │             │
│  VS Code    ├────────►│   Nexus      │◄────────┤  OpenAI     │
│  Extension  │         │   Daemon     │         │  ChatGPT    │
│             │         │   (5050)     │         │   5.1       │
└─────────────┘         └──────────────┘         └─────────────┘
      │                       │                         │
  TypeScript              .NET 9                  Streaming
  LLMClient            CodeIndexer                Responses
  ChatPanel           DSL Compiler
```

### Components

- **extension.ts**: Command registration, activation
- **llmClient.ts**: OpenAI API client with ChatGPT 5.1
- **daemonClient.ts**: HTTP client for Nexus Daemon
- **chatPanel.ts**: Webview panel with streaming UI

## Example Conversations

### Ask About Architecture
```
You: What architectural patterns are used?

AI: Based on the DSL context, I see:
1. Repository Pattern (Rule R01)
2. Dependency Injection (Program.cs)
3. Decorator Pattern (CachedCodeIndexer)
...
```

### Request Code Review
```
You: Review ContextController for issues

AI: Analyzing ContextController.cs (Node: CTRL:42)
✅ Good: Dependency injection
✅ Good: Async/await usage
⚠️  Consider: Extract FilterNodesByTargets to separate service
...
```

### Get Refactoring Suggestions
```
You: How to improve CodeIndexer performance?

AI: 3 optimization strategies:
1. Parallel file processing with Task.WhenAll
2. Incremental indexing with file watchers
3. Pre-compiled Roslyn syntax trees

Here's code for parallel processing:
[code snippet]
```

## DSL v0 Format

The daemon compiles your code into this structured format for ChatGPT:

```
=== RULES ===
@RULE R01 PRI:0.9
    "Services must not access DB directly. Use repositories."

=== NODES ===
@NODE FN:123 T:SRV P:Services/UserService.cs L:10-25
    TAGS: public, async
    DEP: REPO:45
    SUM: "GetUserById"

=== DECISIONS ===
@DECISION D001
    RULE: R01
    CONTEXT: "Architecture validation"
    SCOPE: FN:123
```

## Development

### Build
```bash
npm install
npm run compile
```

### Watch Mode
```bash
npm run watch
```

### Debug
1. Open `nexus-vscode` folder in VS Code
2. Press `F5`
3. Extension Development Host launches

### Package
```bash
npm install -g @vscode/vsce
vsce package
code --install-extension nexus-context-engine-0.1.0.vsix
```

## Troubleshooting

### "Cannot reach Nexus Daemon"
```bash
# Check daemon status
curl http://localhost:5050/swagger/index.html

# Start if not running
cd nexus-daemon/src/Nexus.Server
dotnet run
```

### "OpenAI API key not configured"
1. Settings → Extensions → Nexus Context Engine
2. Set **OpenAI API Key**
3. Reopen chat panel

### "Invalid OpenAI API key"
- Verify at: https://platform.openai.com/api-keys
- Ensure you have credits remaining
- Key should start with `sk-proj-` or `sk-`

### Extension not loading
```bash
cd nexus-vscode
npm run compile
```
Then reload: `Developer: Reload Window`

### Streaming not working
- Check Settings → **Enable Streaming** is `true`
- Try disabling and re-enabling
- Check console: `View → Output → Nexus Context Engine`

## Advanced Usage

### Custom Models
Switch models in settings:
- `gpt-5.1` - Latest GPT-5.1 (default, recommended)
- `gpt-4o` - GPT-4 Optimized
- `gpt-4o-mini` - Faster, cheaper
- `gpt-4-turbo` - Previous generation

### Remote Daemon
For team setups:
```
Settings → Nexus → Daemon URL → http://your-server:5050
```

### Disable Streaming
For slower connections:
```
Settings → Nexus → Enable Streaming → false
```

## Security

✅ **API Key Storage**: Encrypted in VS Code settings
✅ **Never Committed**: Automatically excluded from git
✅ **Path Validation**: Daemon prevents directory traversal
✅ **Secure Transport**: HTTPS for OpenAI API

**Important**: Never commit your API key to version control!

## Performance Tips

1. **Open relevant files** before asking - included in context
2. **Be specific** - "Refactor UserService.cs" vs "Improve code"
3. **Use streaming** - See AI think in real-time
4. **Cache hits** - Repeated queries on same workspace use cache

## Links

- [Quick Start Guide](../QUICKSTART.md)
- [Nexus Daemon](../nexus-daemon)
- [Project Status](../project_status.md)
- [OpenAI API Docs](https://platform.openai.com/docs)

## License

MIT

---

**Built with ❤️ using ChatGPT 5.1, .NET 9, and TypeScript**
