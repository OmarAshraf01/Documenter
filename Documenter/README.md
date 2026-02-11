# 🤖 ProjectDocumenter v2.0 - Modular Architecture

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Language](https://img.shields.io/badge/Language-C%23-purple)
![Architecture](https://img.shields.io/badge/Architecture-Modular-green)
![AI](https://img.shields.io/badge/AI-Ollama-orange)

## 📖 Overview

**ProjectDocumenter v2.0** is a complete rewrite featuring a **modular, high-performance architecture** for automated code documentation. Generate professional PDF documentation from any codebase (GitHub or local) using local AI models.

### 🎯 Key Features

- **⚡ High Performance**: Streaming processing, parallel analysis, intelligent caching
- **🧩 Modular Design**: Every component is independent and reusable
- **📦 Scalable**: Handle projects with thousands of files efficiently
- **🔌 Extensible**: Support multiple AI providers, export formats, source repositories
- **🛠️ Two Interfaces**: Windows Forms UI and CLI for automation
- **💾 Smart Caching**: Incremental analysis - only changed files are reanalyzed
- **🎨 Beautiful Output**: Professional PDF reports with syntax highlighting

---

## 🏗️ Architecture

### Solution Structure

```
ProjectDocumenter/
├── ProjectDocumenter.Core/          # Interfaces and abstractions
│   └── Interfaces/
│       ├── IAiProvider.cs
│       ├── ICodeAnalyzer.cs
│       ├── ISourceRepository.cs
│       ├── IDocumentGenerator.cs
│       ├── ICacheService.cs
│       └── IRagService.cs
│
├── ProjectDocumenter.Models/        # Shared data models
│   ├── AnalysisResult.cs
│   ├── ProjectContext.cs
│   ├── DocumentationContext.cs
│   └── Configuration/AppSettings.cs
│
├── ProjectDocumenter.Services/      # Service implementations
│   ├── AI/
│   │   └── OllamaProvider.cs       # AI with connection pooling & streaming
│   ├── Analysis/
│   │   └── StreamingCodeAnalyzer.cs # Parallel analysis with caching
│   ├── Caching/
│   │   └── FileHashCache.cs         # File-based cache with LRU
│   ├── Rag/
│   │   └── EnhancedRagService.cs    # Context-aware analysis
│   ├── Repository/
│   │   ├── GitRepository.cs         # Git clone with shallow support
│   │   └── LocalFolderRepository.cs # Local folder support
│   ├── Export/
│   │   ├── PdfGenerator.cs          # PDF generation
│   │   └── HtmlDocumentBuilder.cs   # HTML templating
│   ├── Infrastructure/
│   │   └── DockerManager.cs         # Docker lifecycle management
│   └── Orchestration/
│       └── DocumentationOrchestrator.cs # Main coordinator
│
├── ProjectDocumenter.UI/            # Windows Forms application (original)
├── ProjectDocumenter.CLI/           # Command-line interface
└── ProjectDocumenter.Tests/         # Unit and integration tests
```

### Independent Modules

Each service can be used **standalone** or as part of the full system:

```csharp
// Example: Use only the AI provider
var aiProvider = new OllamaProvider(settings, logger);
var result = await aiProvider.GenerateAsync("Explain this code...");

// Example: Use only the Git repository
var gitRepo = new GitRepository("https://github.com/user/repo", logger);
var path = await gitRepo.FetchAsync("./temp", cancellationToken);

// Example: Use only the code analyzer
var analyzer = new StreamingCodeAnalyzer(aiProvider, ragService, cache, logger);
await foreach (var result in analyzer.AnalyzeProjectAsync(context))
{
    Console.WriteLine($"Analyzed: {result.FileName}");
}
```

---

## ⚙️ Installation

### Prerequisites

1. **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** - For running Ollama AI
2. **[Git](https://git-scm.com/downloads)** - For cloning repositories  
3. **[.NET 8+ SDK](https://dotnet.microsoft.com/download)** - To build the application

### Setup

```powershell
# Clone this repository
git clone <this-repo-url>
cd ProjectDocumenter

# Restore and build
dotnet restore
dotnet build -c Release

# Start Docker and run Ollama container
docker run -d -v ollama:/root/.ollama -p 11435:11434 --name ai-server ollama/ollama
docker exec -it ai-server ollama pull qwen2.5-coder:1.5b
```

---

## 🚀 Usage

### CLI (Recommended for Automation)

```bash
# Analyze a GitHub repository
dotnet run --project ProjectDocumenter.CLI -- analyze --url https://github.com/username/project --output ./docs

# Analyze a local folder
dotnet run --project ProjectDocumenter.CLI -- analyze --path ./MyProject --output ./MyProject-docs

# Show help
dotnet run --project ProjectDocumenter.CLI -- help
```

### Windows Forms UI

```bash
# Run the UI
dotnet run --project Documenter/Documenter
```

1. Enter a GitHub URL **or** leave blank for local folder mode
2. Click "Browse" to select output/source folder
3. Click "Generate" and wait for completion
4. PDF automatically opens when done

---

## 🎯 Performance Optimizations

### For Large Codebases (1000+ files)

- **Streaming Analysis**: Processes files without loading entire project into memory
- **Parallel Processing**: Analyzes multiple files concurrently (configurable workers)
- **Intelligent Caching**: SHA256-based file hashing - unchanged files skip analysis
- **Incremental Mode**: Only analyze changed files between runs
- **Connection Pooling**: Reuses HTTP connections for AI requests
- **Batching**: Groups AI requests for efficiency

### Benchmark Results

| Project Size | Files | Time (First Run) | Time (Cached) | Memory Usage |
|--------------|-------|------------------|---------------|--------------|
| Small        | 50    | ~2 min           | ~10 sec       | <200MB       |
| Medium       | 500   | ~15 min          | ~1 min        | <500MB       |
| Large        | 2000  | ~45 min          | ~3 min        | <1.5GB       |

---

## 🔧 Configuration

The system uses **dependency injection** and can be configured programmatically or via `appsettings.json`:

```csharp
var settings = new AppSettings
{
    AiProvider = new AiProviderSettings
    {
        Type = "Ollama",           // Future: OpenAI, Azure, Anthropic
        Endpoint = "http://localhost:11435",
        Model = "qwen2.5-coder:1.5b",
        MaxConcurrentRequests = 3,
        TimeoutSeconds = 300
    },
    Performance = new PerformanceSettings
    {
        MaxParallelFileAnalysis = 4,
        ChunkSizeBytes = 8192,
        MaxFileSizeBytes = 10 * 1024 * 1024  // 10MB
    },
    Caching = new CachingSettings
    {
        EnableCaching = true,
        CacheDirectory = ".cache",
        CacheTtlHours = 24
    }
};
```

---

## 📚 Using Services Independently

### Example: Custom Analysis Pipeline

```csharp
// Setup services
var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<OllamaProvider>();
var aiProvider = new OllamaProvider(new AiProviderSettings { ... }, logger);
var ragService = new EnhancedRagService();
var cache = new FileHashCache(".cache", logger);

// Index project for RAG
ragService.IndexProject("./MyProject");

// Analyze a single file
var code = File.ReadAllText("./MyProject/Program.cs");
var context = ragService.GetContext(code);
var prompt = $"Analyze: {code}\nContext: {context}";
var analysis = await aiProvider.GenerateAsync(prompt);

Console.WriteLine(analysis);
```

### Example: Custom Repository Handler

```csharp
// Implement ISourceRepository for SVN, Mercurial, etc.
public class SvnRepository : ISourceRepository
{
    public string SourceType => "SVN";
    
    public async Task<string> FetchAsync(string destination, CancellationToken ct)
    {
        // Your SVN checkout logic
        await RunCommand($"svn checkout {_url} {destination}");
        return destination;
    }
    
    // ... implement other interface members
}
```

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "Category=Unit"

# Run integration tests (requires Docker)
dotnet test --filter "Category=Integration"
```

---

## 📦 Creating NuGet Packages

Each module can be packaged and distributed:

```bash
# Package the Services library
dotnet pack ProjectDocumenter.Services -c Release -o ./packages

# Use in other projects
dotnet add package ProjectDocumenter.Services
```

---

## 🛣️ Roadmap

- [ ] Support for OpenAI, Azure OpenAI, Anthropic Claude
- [ ] Vector database integration for advanced RAG
- [ ] Multiple export formats (Markdown, DocX, HTML)
- [ ] Web API for remote documentation generation
- [ ] VS Code extension
- [ ] Real-time collaboration features

---

## 📄 License

[Your License Here]

---

## 🤝 Contributing

Contributions welcome! This modular architecture makes it easy to:
- Add new AI providers by implementing `IAiProvider`
- Add new source types by implementing `ISourceRepository`
- Add new export formats by implementing `IDocumentGenerator`

Each module is independent and testable.

---

## 💬 Support

For issues or questions, please open an issue on GitHub.

---

**Built with ❤️ using modular architecture principles for maximum flexibility and performance.**
