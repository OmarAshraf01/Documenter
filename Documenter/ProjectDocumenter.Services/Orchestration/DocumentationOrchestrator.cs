using Microsoft.Extensions.Logging;
using ProjectDocumenter.Core.Interfaces;
using ProjectDocumenter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Services.Orchestration
{
    /// <summary>
    /// Main orchestrator for documentation generation
    /// </summary>
    public class DocumentationOrchestrator
    {
        private readonly ISourceRepository _repository;
        private readonly ICodeAnalyzer _analyzer;
        private readonly IAiProvider _aiProvider;
        private readonly IDocumentGenerator _pdfGenerator;
        private readonly ILogger<DocumentationOrchestrator> _logger;

        private static readonly string[] IgnoredFolders = { "node_modules", ".git", ".vs", "bin", "obj", "properties", "debug", "lib", "packages", "documentation" };
        private static readonly HashSet<string> ValidExtensions = new() { ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".sql", ".xml", ".config", ".html", ".css", ".go", ".rs" };

        public DocumentationOrchestrator(
            ISourceRepository repository,
            ICodeAnalyzer analyzer,
            IAiProvider aiProvider,
            IDocumentGenerator pdfGenerator,
            ILogger<DocumentationOrchestrator> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GenerateDocumentationAsync(
            string outputDirectory,
            IProgress<AnalysisProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting documentation generation");

            // Fetch source code
            progress?.Report(new AnalysisProgress { CurrentPhase = "Fetching Source", ProcessedFiles = 0, TotalFiles = 0 });
            var sourcePath = await _repository.FetchAsync(Path.Combine(outputDirectory, "Source"), cancellationToken);

            // Build project context
            var projectContext = BuildProjectContext(sourcePath);

            // Analyze code
            var analysisResults = new List<AnalysisResult>();
            var dbData = new StringBuilder();

            await foreach (var result in _analyzer.AnalyzeProjectAsync(projectContext, progress, cancellationToken))
            {
                analysisResults.Add(result);

                // Collect database-related files
                if (IsDatabaseFile(result.FileName, result.MarkdownContent))
                {
                    dbData.AppendLine($"--- {result.FileName} ---\n{result.MarkdownContent}\n");
                }
            }

            // Analyze database
            string dbAnalysis = "";
            if (dbData.Length > 50)
            {
                progress?.Report(new AnalysisProgress { CurrentPhase = "Analyzing Database", ProcessedFiles = 0, TotalFiles = 0 });
                dbAnalysis = await AnalyzeDatabaseAsync(dbData.ToString(), cancellationToken);
            }

            // Generate README
            progress?.Report(new AnalysisProgress { CurrentPhase = "Generating README", ProcessedFiles = 0, TotalFiles = 0 });
            var summary = BuildProjectSummary(analysisResults);
            var readme = await GenerateReadmeAsync(summary, _repository.GetName(), cancellationToken);

            // Build documentation context
            var docContext = new DocumentationContext
            {
                ProjectName = _repository.GetName(),
                RepositoryUrl = _repository.SourceType == "Git" ? projectContext.Configuration.GetValueOrDefault("RepositoryUrl", "") : "Local Project",
                ProjectTree = GenerateProjectTree(sourcePath),
                AnalysisResults = analysisResults,
                DatabaseAnalysis = dbAnalysis,
                ReadmeContent = readme
            };

            // Generate PDF
            var docsOutputDir = Path.Combine(outputDirectory, "Documentation");
            Directory.CreateDirectory(docsOutputDir);

            var pdfPath = Path.Combine(docsOutputDir, "Documentation.pdf");
            await _pdfGenerator.GenerateAsync(docContext, pdfPath, null, cancellationToken);

            _logger.LogInformation("Documentation generation complete: {Path}", pdfPath);
            return pdfPath;
        }

        private ProjectContext BuildProjectContext(string projectPath)
        {
            var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(IsCodeFile)
                .ToList();

            return new ProjectContext
            {
                ProjectName = Path.GetFileName(projectPath),
                ProjectPath = projectPath,
                FilesToAnalyze = files,
                IgnoredFolders = IgnoredFolders.ToList(),
                SupportedExtensions = ValidExtensions.ToList()
            };
        }

        private bool IsCodeFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (!ValidExtensions.Contains(ext)) return false;

            foreach (var folder in IgnoredFolders)
            {
                if (path.ToLower().Contains(Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar))
                    return false;
            }

            return true;
        }

        private bool IsDatabaseFile(string fileName, string content)
        {
            var lower = fileName.ToLower();
            return lower.Contains("dal") || lower.Contains("model") || lower.Contains("entity") ||
                   lower.Contains("dto") || content.Contains("CREATE TABLE") ||
                   content.Contains("DbContext") || content.Contains("DbSet");
        }

        private string GenerateProjectTree(string rootPath)
        {
            var sb = new StringBuilder();
            GenerateTreeRecursive(rootPath, "", rootPath, sb);
            return sb.ToString();
        }

        private void GenerateTreeRecursive(string currentDir, string indent, string rootDir, StringBuilder sb)
        {
            try
            {
                var directories = Directory.GetDirectories(currentDir)
                    .Where(d => !IgnoredFolders.Contains(Path.GetFileName(d).ToLower()));

                foreach (var dir in directories)
                {
                    sb.AppendLine($"{indent}📁 {Path.GetFileName(dir)}/");
                    GenerateTreeRecursive(dir, indent + "    ", rootDir, sb);
                }

                var files = Directory.GetFiles(currentDir).Where(IsCodeFile);
                foreach (var file in files)
                {
                    sb.AppendLine($"{indent}📄 {Path.GetFileName(file)}");
                }
            }
            catch { }
        }

        private string BuildProjectSummary(List<AnalysisResult> results)
        {
            var sb = new StringBuilder();
            foreach (var result in results.Take(20)) // Limit to avoid huge prompts
            {
                sb.AppendLine($"File: {result.FileName}\nSummary: {result.Summary}\n");
            }
            return sb.ToString();
        }

        private async Task<string> AnalyzeDatabaseAsync(string dbData, CancellationToken cancellationToken)
        {
            var prompt = $@"
[ROLE: Senior Backend Developer]
Analyze the provided code snippets (DAL/SQL).
Create a **Markdown Table** summarizing the database entities.

### CODE SNIPPETS
{dbData.Substring(0, Math.Min(dbData.Length, 8000))}

### INSTRUCTIONS
1. Identify Table Names/Entities.
2. Describe what they store.
3. If no database logic is found, return 'N/A'.

### OUTPUT FORMAT
## 🗄️ Database Structure
| Entity / Table | Inferred Fields | Purpose |
|---|---|---|
| [Name] | [Fields] | [Description] |
";

            return await _aiProvider.GenerateAsync(prompt, cancellationToken);
        }

        private async Task<string> GenerateReadmeAsync(string summary, string projectName, CancellationToken cancellationToken)
        {
            var prompt = $@"
[ROLE: Senior Developer]
Write a professional README.md for the project: {projectName}

### PROJECT SUMMARY
{summary.Substring(0, Math.Min(summary.Length, 5000))}

### STRICT OUTPUT RULES
1. Markdown only.
2. NO conversational text.

### FORMAT
# {projectName}
## 📖 Description
[Professional description]
## ✨ Key Features
[Bullet points]
## 🛠️ Tech Stack
[List]
## 🚀 Setup
1. [Step 1]
2. [Step 2]
";

            return await _aiProvider.GenerateAsync(prompt, cancellationToken);
        }
    }
}
