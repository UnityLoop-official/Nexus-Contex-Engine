# Nexus Context Engine

An advanced, intelligent context-awareness engine for Visual Studio 2022, designed to bridge the gap between your codebase and LLMs.

## 🚀 Overview

Nexus is a local "Context Daemon" that analyzes your active solution, compiles relevant code context into a specialized DSL (Domain Specific Language), and serves it to a lightweight Visual Studio extension. This allows AI assistants to understand not just the code you are looking at, but the *architectural context*, *rules*, and *relationships* surrounding it.

## 🏗️ Architecture

The system consists of two main components:

### 1. Nexus Daemon (`nexus-daemon`)
A high-performance **.NET 9 Web API** running locally.
- **Responsibilities**:
  - **Indexing**: Parses C# code using Roslyn to extract semantic nodes (Classes, Methods, APIs).
  - **Context Compilation**: Generates "DSL v0" bytecode optimized for LLM consumption.
  - **Security**: Validates all file access to prevent path traversal attacks.
  - **Caching**: Uses in-memory caching to ensure sub-millisecond response times for repeated queries.
- **Tech Stack**: ASP.NET Core 9, Roslyn, Microsoft.Extensions.Caching.Memory.

### 2. NexusVS Extension (`nexus-vs-extension`)
A **Visual Studio 2022 VSIX** extension.
- **Responsibilities**:
  - Provides a chat interface within VS.
  - Captures editor state (Active Solution, Active File, Selection).
  - Communicates with the Daemon via HTTP (Port 5050).
- **Tech Stack**: .NET Framework 4.7.2, WPF (Windows Presentation Foundation).

## ✨ Key Features

- **Smart Context**: Doesn't just dump files. It selects relevant nodes based on your query and architectural rules.
- **Security First**: 
  - Strict path validation preventing unauthorized file access.
  - Configurable "Authorized Root" (Defaults to `C:\` for MVP, safest to run in isolated dev environments).
- **Performance**:
  - **Indexer Caching**: Avoids re-parsing thousands of files on every request.
- **Extensible**:
  - **Rule Provider**: Modular system to inject architectural rules (e.g., "No DB access in Controllers").

## 🛠️ Getting Started

### Prerequisites
- Visual Studio 2022 (v17.0+)
- .NET 9.0 SDK
- .NET Framework 4.7.2 Dev Pack

### Step 1: Start the Daemon
The daemon must be running for the extension to work.

```powershell
cd nexus-daemon/src/Nexus.Server
dotnet run
```
*The daemon will start listening on `http://localhost:5050`.*

### Step 2: Run the Extension
1. Open `nexus-vs-extension/NexusVS.sln` in Visual Studio.
2. Set `NexusVS` as the startup project.
3. Press **F5** to launch the Experimental Instance of Visual Studio.
4. In the new VS window, go to **View -> Other Windows -> Nexus Context Chat**.

### Step 3: Usage
1. Open a C# solution in the Experimental Instance.
2. Open a code file.
3. In the Nexus Chat window, type a message or simply click **Send** to see the compiled context for the current file.

## 🔒 Security
The daemon exposes a local API. By default, it binds to `localhost`.
- **Path Traversal Protection**: Attempts to access files like `../../windows/system32` are blocked and logged.
- **Authorized Root**: Constrained to allowed directory paths.

## 🛣️ Roadmap
- [x] **Phase 1**: Security Hardening & Daemon MVP
- [x] **Phase 2**: Indexer Caching
- [x] **Phase 3**: Smart Target Matching
- [x] **Phase 4**: Rule Provider Pattern
- [x] **Phase 5**: Unit Tests
- [x] **Phase 6**: VS Extension Polish
- [ ] **Phase 7**: Production Release (Signed VSIX, Installer)

---
*Built with ❤️ by the Nexus Team.*
