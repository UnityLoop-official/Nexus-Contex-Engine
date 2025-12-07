# 🚀 Nexus AI - Quick Start Guide

Get your AI-powered code assistant running in **3 steps**!

---

## ⚡ Quick Setup (5 minutes)

### **Step 1: Start the Daemon**

```bash
cd nexus-daemon/src/Nexus.Server
dotnet run
```

✅ **Verify**: Open http://localhost:5050/swagger in browser

---

### **Step 2: Configure API Key**

1. Open VS Code
2. Open folder `nexus-vscode` (File → Open Folder)
3. Press **F5** to launch Extension Development Host
4. In the new VS Code window:
   - `Ctrl+Shift+P` → "Preferences: Open Settings (UI)"
   - Search for: **"nexus"**
   - Find **"Nexus: OpenAI API Key"**
   - Paste your OpenAI API key (get one at https://platform.openai.com/api-keys)

**Security Note**: This key is stored securely in VS Code settings. Never commit it to git!

---

### **Step 3: Open Chat & Test**

1. Open a workspace (e.g., the `nexus-daemon` folder)
2. `Ctrl+Shift+P` → **"Nexus: Open Chat Panel"**
3. Try these example questions:

```
What services exist in this codebase?
```

```
Explain how the context compilation works
```

```
Suggest improvements to the CodeIndexer performance
```

---

## 🎯 What Just Happened?

When you send a message:

1. **Nexus Daemon** indexes your workspace (cached for 5 min)
2. **Context Compiler** generates DSL with:
   - Architectural rules
   - Code nodes (functions, classes)
   - File structure
3. **ChatGPT 4o** receives this context + your question
4. **AI responds** with code-aware answers

---

## 🔧 Configuration

Access settings: `Ctrl+,` → search "nexus"

| Setting | Default | Description |
|---------|---------|-------------|
| **OpenAI API Key** | (empty) | Your ChatGPT API key |
| **LLM Model** | `gpt-4o` | Model: gpt-4o, gpt-4o-mini, etc. |
| **Enable Streaming** | `true` | Real-time response streaming |
| **Daemon URL** | `http://localhost:5050` | Backend daemon address |

---

## 📖 Example Conversations

### **Architectural Questions**
```
User: What architectural patterns are used in this codebase?

Nexus AI: Based on the context, I can see:
1. Repository Pattern (Rule R01)
2. Dependency Injection (Program.cs)
3. Decorator Pattern (CachedCodeIndexer)
...
```

### **Code Review**
```
User: Review the ContextController for potential issues

Nexus AI: I've analyzed ContextController.cs. Here are my findings:
1. ✅ Good: Uses dependency injection
2. ⚠️ Consider: The target filtering could be optimized...
...
```

### **Refactoring Suggestions**
```
User: How can I improve the CodeIndexer performance?

Nexus AI: Here are 3 optimization strategies:
1. Parallel file processing with Parallel.ForEach...
[code snippet]
...
```

---

## 🐛 Troubleshooting

### **"Cannot reach Nexus Daemon"**
```bash
# Check daemon is running
curl http://localhost:5050/swagger/index.html

# If not, start it:
cd nexus-daemon/src/Nexus.Server
dotnet run
```

### **"OpenAI API key not configured"**
- Go to Settings → Extensions → Nexus
- Set your API key
- Reopen chat panel

### **"Invalid OpenAI API key"**
- Verify key at: https://platform.openai.com/api-keys
- Check you have credits remaining
- Ensure key starts with `sk-proj-` or `sk-`

### **Extension not appearing**
```bash
cd nexus-vscode
npm run compile
```
Then press `F5` again

---

## 💡 Pro Tips

1. **Open relevant files** before asking - Nexus will include them in context
2. **Be specific** - "Refactor UserService" vs "Improve code"
3. **Reference NodeIds** - Use IDs like `FN:123` from previous responses
4. **Use streaming** - Watch AI think in real-time (enabled by default)

---

## 🎓 Advanced Usage

### **Custom Models**
Settings → Nexus → LLM Model:
- `gpt-4o` - Latest flagship (best quality)
- `gpt-4o-mini` - Faster, cheaper
- `gpt-4-turbo` - Previous generation

### **Disable Streaming**
For slower connections:
Settings → Nexus → Enable Streaming → `false`

### **Remote Daemon**
Settings → Nexus → Daemon URL → `http://your-server:5050`

---

## 📚 Next Steps

- Read [project_status.md](project_status.md) for architecture details
- Explore [nexus-vscode/README.md](nexus-vscode/README.md) for extension docs
- Check daemon API at http://localhost:5050/swagger

---

## 🎉 You're Ready!

Start chatting with your AI assistant and explore your codebase like never before!

**Questions?** Check the logs:
- Daemon: Console output where `dotnet run` is running
- Extension: `View → Output → Nexus Context Engine`
