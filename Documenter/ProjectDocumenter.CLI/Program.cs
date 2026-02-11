using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectDocumenter.Core.Interfaces;
using ProjectDocumenter.Models.Configuration;
using ProjectDocumenter.Services.AI;
using ProjectDocumenter.Services.Analysis;
using ProjectDocumenter.Services.Caching;
using ProjectDocumenter.Services.Export;
using ProjectDocumenter.Services.Infrastructure;
using ProjectDocumenter.Services.Orchestration;
using ProjectDocumenter.Services.Rag;
using ProjectDocumenter.Services.Repository;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ProjectDocumenter.CLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("🤖 ProjectDocumenter CLI v2.0 - Modular Edition");
            Console.WriteLine("================================================\n");

            if (args.Length == 0)
            {
                ShowUsage();
                return 1;
            }

            var command = args[0].ToLower();

            try
            {
                return command switch
                {
                    "analyze" => await AnalyzeCommand(args),
                    "help" => ShowUsage(),
                    _ => ShowUsage()
                };
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static async Task<int> AnalyzeCommand(string[] args)
        {
            string? url = null;
            string? path = null;
            string? output = null;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--url" && i + 1 < args.Length)
                    url = args[++i];
                else if (args[i] == "--path" && i + 1 < args.Length)
                    path = args[++i];
                else if (args[i] == "--output" && i + 1 < args.Length)
                    output = args[++i];
            }

            if (string.IsNullOrEmpty(url) && string.IsNullOrEmpty(path))
            {
                Console.WriteLine("❌ Error: Either --url or --path is required");
                return 1;
            }

            output ??= Path.Combine(Environment.CurrentDirectory, "Documentation");

            Console.WriteLine($"📂 Output Directory: {output}\n");

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            // Ensure Docker is running
            var dockerManager = provider.GetRequiredService<DockerManager>();
            if (!await dockerManager.EnsureDockerRunningAsync())
            {
                Console.WriteLine("❌ Docker is not running. Please start Docker Desktop and try again.");
                return 1;
            }

            // Ensure Ollama container is running
            await dockerManager.EnsureContainerRunningAsync(
                "ai-server",
                "ollama/ollama",
                new[] { "run", "-d", "-v", "ollama:/root/.ollama", "-p", "11435:11434", "--name", "ai-server", "ollama/ollama" },
                default);

            Console.WriteLine("✅ Docker and AI container ready\n");

            // Create repository
            ISourceRepository repository;
            var logger = provider.GetRequiredService<ILogger<GitRepository>>();
            var localLogger = provider.GetRequiredService<ILogger<LocalFolderRepository>>();

            if (!string.IsNullOrEmpty(url))
            {
                Console.WriteLine($"🌐 Cloning repository: {url}");
                repository = new GitRepository(url, logger, shallowClone: true);
            }
            else
            {
                Console.WriteLine($"📁 Analyzing local folder: {path}");
                repository = new LocalFolderRepository(path!, localLogger);
            }

            // Create orchestrator
            var orchestrator = provider.GetRequiredService<DocumentationOrchestrator>();

            // Generate documentation
            var progress = new Progress<ProjectDocumenter.Models.AnalysisProgress>(p =>
            {
                Console.WriteLine($"⚙️  [{p.CurrentPhase}] {p.CurrentFile} ({p.ProcessedFiles}/{p.TotalFiles})");
            });

            var pdfPath = await orchestrator.GenerateDocumentationAsync(output, progress, default);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✅ Documentation generated successfully!");
            Console.WriteLine($"📄 PDF: {pdfPath}");
            Console.ResetColor();

            return 0;
        }

        static void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            var settings = new AppSettings
            {
                AiProvider = new AiProviderSettings
                {
                    Type = "Ollama",
                    Endpoint = "http://localhost:11435",
                    Model = "qwen2.5-coder:1.5b",
                    MaxConcurrentRequests = 3,
                    TimeoutSeconds = 300
                },
                Performance = new PerformanceSettings
                {
                    MaxParallelFileAnalysis = 4,
                    ChunkSizeBytes = 8192
                },
                Caching = new CachingSettings
                {
                    EnableCaching = true,
                    CacheDirectory = ".cache"
                }
            };

            // Logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddConsole();
            });

            // Core Services
            services.AddSingleton(settings.AiProvider);
            services.AddSingleton(settings.Caching);
            services.AddSingleton<IAiProvider, OllamaProvider>();
            services.AddSingleton<IRagService, EnhancedRagService>();
            services.AddSingleton<ICacheService>(sp =>
                new FileHashCache(
                    settings.Caching.CacheDirectory,
                    sp.GetRequiredService<ILogger<FileHashCache>>()));

            services.AddSingleton<ICodeAnalyzer>(sp =>
                new StreamingCodeAnalyzer(
                    sp.GetRequiredService<IAiProvider>(),
                    sp.GetRequiredService<IRagService>(),
                    sp.GetRequiredService<ICacheService>(),
                    sp.GetRequiredService<ILogger<StreamingCodeAnalyzer>>(),
                    settings.Performance.MaxParallelFileAnalysis));

            services.AddSingleton<IDocumentGenerator, PdfGenerator>();
            services.AddSingleton<DockerManager>();
            services.AddSingleton<DocumentationOrchestrator>();
        }

        static int ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  documenter analyze --url <github-url> --output <output-dir>");
            Console.WriteLine("  documenter analyze --path <local-path> --output <output-dir>");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  documenter analyze --url https://github.com/user/repo --output ./docs");
            Console.WriteLine("  documenter analyze --path ./MyProject --output ./MyProject-docs");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  --url       GitHub repository URL to analyze");
            Console.WriteLine("  --path      Local folder path to analyze");
            Console.WriteLine("  --output    Output directory for documentation (default: ./Documentation)");
            return 0;
        }
    }
}
