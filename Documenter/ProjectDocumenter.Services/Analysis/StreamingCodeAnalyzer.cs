using Microsoft.Extensions.Logging;
using ProjectDocumenter.Core.Interfaces;
using ProjectDocumenter.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Services.Analysis
{
    /// <summary>
    /// Memory-efficient streaming code analyzer with parallel processing
    /// </summary>
    public class StreamingCodeAnalyzer : ICodeAnalyzer
    {
        private readonly IAiProvider _aiProvider;
        private readonly IRagService _ragService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<StreamingCodeAnalyzer> _logger;
        private readonly int _maxParallelism;
        private readonly int _maxFileSize;

        public StreamingCodeAnalyzer(
            IAiProvider aiProvider,
            IRagService ragService,
            ICacheService cacheService,
            ILogger<StreamingCodeAnalyzer> logger,
            int maxParallelism = 4,
            int maxFileSize = 10 * 1024 * 1024) // 10MB
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxParallelism = maxParallelism;
            _maxFileSize = maxFileSize;
        }

        public async Task<AnalysisResult> AnalyzeFileAsync(string filePath, string content, ProjectContext context, CancellationToken cancellationToken = default)
        {
            var fileName = Path.GetFileName(filePath);
            var fileHash = ComputeHash(content);

            // Check cache first
            var cacheKey = $"analysis:{fileHash}"; 
            var cachedResult = await _cacheService.GetAsync<AnalysisResult>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for {FileName}", fileName);
                return cachedResult;
            }

            // Truncate large files
            if (content.Length > _maxFileSize)
            {
                _logger.LogWarning("File {FileName} exceeds max size, truncating", fileName);
                content = content.Substring(0, _maxFileSize) + "\n\n... [File truncated due to size]";
            }

            // Get context from RAG
            var ragContext = _ragService.GetContext(content);

            // Build prompt
            var prompt = BuildAnalysisPrompt(fileName, content, ragContext);

            // Call AI
            var response = await _aiProvider.GenerateAsync(prompt, cancellationToken);

            var result = new AnalysisResult
            {
                FileName = fileName,
                FilePath = filePath,
                FileHash = fileHash,
                MarkdownContent = response,
                Summary = ExtractSummary(response),
                AnalyzedAt = DateTime.UtcNow
            };

            // Cache the result
            await _cacheService.SetAsync(cacheKey, result, cancellationToken);

            return result;
        }

        public async IAsyncEnumerable<AnalysisResult> AnalyzeProjectAsync(
            ProjectContext context,
            IProgress<AnalysisProgress>? progress = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var files = context.FilesToAnalyze;
            var totalFiles = files.Count;
            var processedFiles = 0;

            // Index project for RAG
            _ragService.IndexProject(context.ProjectPath);

            // Process files with throttling
            var semaphore = new SemaphoreSlim(_maxParallelism);
            var results = new BlockingCollection<(int Index, AnalysisResult Result)>();

            var processingTask = Task.Run(async () =>
            {
                var tasks = files.Select(async (filePath, index) =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                        if (string.IsNullOrWhiteSpace(content)) return;

                        progress?.Report(new AnalysisProgress
                        {
                            TotalFiles = totalFiles,
                            ProcessedFiles = Interlocked.Increment(ref processedFiles),
                            CurrentFile = Path.GetFileName(filePath),
                            CurrentPhase = "Analyzing"
                        });

                        var result = await AnalyzeFileAsync(filePath, content, context, cancellationToken);
                        results.Add((index, result));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error analyzing {FilePath}", filePath);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
                results.CompleteAdding();
            }, cancellationToken);

            // Stream results in order
            var buffer = new Dictionary<int, AnalysisResult>();
            var nextIndex = 0;

            foreach (var (index, result) in results.GetConsumingEnumerable(cancellationToken))
            {
                buffer[index] = result;

                while (buffer.ContainsKey(nextIndex))
                {
                    yield return buffer[nextIndex];
                    buffer.Remove(nextIndex);
                    nextIndex++;
                }
            }

            await processingTask;
        }

        public async IAsyncEnumerable<AnalysisResult> AnalyzeIncrementalAsync(
            ProjectContext context,
            IProgress<AnalysisProgress>? progress = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Filter to only changed files
            var changedFiles = new List<string>();

            foreach (var filePath in context.FilesToAnalyze)
            {
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var fileHash = ComputeHash(content);
                var cacheKey = $"analysis:{fileHash}";

                if (!await _cacheService.ExistsAsync(cacheKey, cancellationToken))
                {
                    changedFiles.Add(filePath);
                }
            }

            _logger.LogInformation("Incremental analysis: {Changed}/{Total} files changed",
                changedFiles.Count, context.FilesToAnalyze.Count);

            // Analyze only changed files
            var incrementalContext = new ProjectContext
            {
                ProjectName = context.ProjectName,
                ProjectPath = context.ProjectPath,
                FilesToAnalyze = changedFiles,
                IgnoredFolders = context.IgnoredFolders,
                SupportedExtensions = context.SupportedExtensions
            };

            await foreach (var result in AnalyzeProjectAsync(incrementalContext, progress, cancellationToken))
            {
                yield return result;
            }
        }

        private static string ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static string BuildAnalysisPrompt(string fileName, string code, string context)
        {
            if (code.Length > 8000)
            {
                code = code.Substring(0, 8000) + "...[truncated]";
            }

            return $@"
[ROLE: Technical Writer]
Analyze this file: '{fileName}'.

### CODE
{code}

### CONTEXT
{context}

### STRICT OUTPUT RULES
1. Markdown only.
2. NO conversational text.
3. NO closing remarks.

### FORMAT
## 📄 {fileName}
**Type:** [Class/Interface/Module]
### 📘 Summary
[One sentence summary]
### 🛠️ Key Components
| Name | Type | Description |
|---|---|---|
| [Name] | [Type] | [Description] |
";
        }

        private static string ExtractSummary(string markdown)
        {
            // Extract first sentence from Summary section
            var lines = markdown.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("###") && line.Contains("Summary"))
                {
                    var idx = Array.IndexOf(lines, line);
                    if (idx + 1 < lines.Length)
                    {
                        return lines[idx + 1].Trim();
                    }
                }
            }
            return "No summary available";
        }
    }
}
