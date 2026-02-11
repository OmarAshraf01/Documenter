using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectDocumenter.Core.Interfaces;
using ProjectDocumenter.Models;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Documenter
{
    public partial class Form1 : Form
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly DocumentationOrchestrator _orchestrator;
        private readonly DockerManager _dockerManager;
        private CancellationTokenSource? _cancellationTokenSource;

        private string _selectedFolder = string.Empty;
        private bool _isProcessing = false;

        public Form1()
        {
            InitializeComponent();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Get required services
            _orchestrator = _serviceProvider.GetRequiredService<DocumentationOrchestrator>();
            _dockerManager = _serviceProvider.GetRequiredService<DockerManager>();

            // Clear and reattach events
            btnBrowse.Click -= BtnBrowse_Click;
            btnStart.Click -= BtnStart_Click;
            txtUrl.TextChanged -= TxtUrl_TextChanged;
            this.Load -= Form1_Load;
            this.FormClosing -= Form1_FormClosing;

            btnBrowse.Click += BtnBrowse_Click;
            btnStart.Click += BtnStart_Click;
            txtUrl.TextChanged += TxtUrl_TextChanged;
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;

            // Default folder
            _selectedFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            lblSelectedPath.Text = _selectedFolder;

            // Add modern button hover effects
            AddButtonHoverEffects();
        }

        private void ConfigureServices(IServiceCollection services)
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

        private void AddButtonHoverEffects()
        {
            // Hover effect for Start button
            btnStart.MouseEnter += (s, e) => {
                if (btnStart.Enabled)
                    btnStart.BackColor = Color.FromArgb(39, 174, 96);
            };
            btnStart.MouseLeave += (s, e) => btnStart.BackColor = Color.FromArgb(46, 204, 113);

            // Hover effect for Browse button
            btnBrowse.MouseEnter += (s, e) => btnBrowse.BackColor = Color.FromArgb(41, 128, 185);
            btnBrowse.MouseLeave += (s, e) => btnBrowse.BackColor = Color.FromArgb(52, 152, 219);
        }

        private async void Form1_Load(object? sender, EventArgs e)
        {
            UpdateStatus("Initializing Docker...");
            btnStart.Enabled = false;
            progressBar1.Visible = true;

            try
            {
                var isDockerRunning = await _dockerManager.EnsureDockerRunningAsync();
                if (!isDockerRunning)
                {
                    Log("❌ Docker is not running. Please start Docker Desktop.");
                    UpdateStatus("Docker initialization failed");
                    progressBar1.Visible = false;
                    MessageBox.Show("Docker Desktop is not running. Please start Docker Desktop and restart the application.",
                        "Docker Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Ensure Ollama container is running
                await _dockerManager.EnsureContainerRunningAsync(
                    "ai-server",
                    "ollama/ollama",
                    new[] { "run", "-d", "-v", "ollama:/root/.ollama", "-p", "11435:11434", "--name", "ai-server", "ollama/ollama" },
                    default);

                Log("✅ Docker and AI container ready");
                UpdateStatus("Ready");
                btnStart.Enabled = true;
                UpdateButtonText();
            }
            catch (Exception ex)
            {
                Log($"❌ Initialization error: {ex.Message}");
                UpdateStatus("Initialization failed");
                MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar1.Visible = false;
            }
        }

        private void TxtUrl_TextChanged(object? sender, EventArgs e)
        {
            UpdateButtonText();
        }

        private void UpdateButtonText()
        {
            var isGitMode = !string.IsNullOrWhiteSpace(txtUrl.Text);
            btnStart.Text = isGitMode ? "🚀 Clone & Document" : "🚀 Generate Documentation";

            if (lblStatus.Text == "Ready")
            {
                UpdateStatus(isGitMode ? "Mode: GitHub Repository" : "Mode: Local Folder");
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            var isGitMode = !string.IsNullOrWhiteSpace(txtUrl.Text);

            if (isGitMode)
            {
                fbd.Description = "📁 Select folder to save the cloned repository and documentation";
            }
            else
            {
                fbd.Description = "📁 Select the SOURCE CODE folder you want to document";
            }

            fbd.UseDescriptionForTitle = true;
            fbd.SelectedPath = _selectedFolder;

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                _selectedFolder = fbd.SelectedPath;
                lblSelectedPath.Text = _selectedFolder;
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (_isProcessing)
            {
                // Cancel operation
                _cancellationTokenSource?.Cancel();
                return;
            }

            var isGitMode = !string.IsNullOrWhiteSpace(txtUrl.Text);

            try
            {
                _isProcessing = true;
                _cancellationTokenSource = new CancellationTokenSource();
                btnStart.Text = "⏹️ Cancel";
                btnStart.BackColor = Color.FromArgb(231, 76, 60);
                btnBrowse.Enabled = false;
                txtUrl.Enabled = false;
                progressBar1.Visible = true;
                lstLog.Items.Clear();

                ISourceRepository repository;
                string outputFolder;

                if (isGitMode)
                {
                    // GitHub Mode
                    var repoUrl = txtUrl.Text.Trim();
                    Log($"🌐 Cloning repository: {repoUrl}");
                    UpdateStatus("Cloning repository...");

                    var logger = _serviceProvider.GetRequiredService<ILogger<GitRepository>>();
                    repository = new GitRepository(repoUrl, logger, shallowClone: true);

                    var projectName = Path.GetFileName(repoUrl.TrimEnd('/').Replace(".git", ""));
                    outputFolder = Path.Combine(_selectedFolder, projectName, "Documentation");
                }
                else
                {
                    // Local Mode
                    if (!Directory.Exists(_selectedFolder))
                    {
                        MessageBox.Show("Selected folder does not exist. Please select a valid folder.",
                            "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    Log($"📁 Analyzing local folder: {_selectedFolder}");
                    UpdateStatus("Analyzing folder...");

                    var logger = _serviceProvider.GetRequiredService<ILogger<LocalFolderRepository>>();
                    repository = new LocalFolderRepository(_selectedFolder, logger);

                    var projectName = new DirectoryInfo(_selectedFolder).Name;
                    outputFolder = Path.Combine(_selectedFolder, "Documentation");
                }

                // Generate documentation with progress reporting
                var progress = new Progress<AnalysisProgress>(p =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(() => UpdateProgress(p));
                    }
                    else
                    {
                        UpdateProgress(p);
                    }
                });

                UpdateStatus("Generating documentation...");
                var pdfPath = await _orchestrator.GenerateDocumentationAsync(
                    outputFolder,
                    progress,
                    _cancellationTokenSource.Token);

                // Success
                Log($"✅ Documentation generated successfully!");
                Log($"📄 PDF: {pdfPath}");
                UpdateStatus("Completed successfully");
                progressBar1.Visible = false;

                var result = MessageBox.Show(
                    $"Documentation generated successfully!\n\nLocation: {pdfPath}\n\nWould you like to open the documentation folder?",
                    "Success",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", outputFolder);
                }
            }
            catch (OperationCanceledException)
            {
                Log("⚠️ Operation cancelled by user");
                UpdateStatus("Cancelled");
                progressBar1.Visible = false;
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
                UpdateStatus("Failed");
                progressBar1.Visible = false;
                MessageBox.Show($"An error occurred:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                btnStart.Text = isGitMode ? "🚀 Clone & Document" : "🚀 Generate Documentation";
                btnStart.BackColor = Color.FromArgb(46, 204, 113);
                btnBrowse.Enabled = true;
                txtUrl.Enabled = true;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void UpdateProgress(AnalysisProgress progress)
        {
            var message = $"⚙️  [{progress.CurrentPhase}] {progress.CurrentFile} ({progress.ProcessedFiles}/{progress.TotalFiles})";
            Log(message);
            UpdateStatus($"{progress.CurrentPhase}: {progress.ProcessedFiles}/{progress.TotalFiles} files");
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => Log(message));
                return;
            }

            lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            lstLog.TopIndex = lstLog.Items.Count - 1; // Auto-scroll
        }

        private void UpdateStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateStatus(status));
                return;
            }

            lblStatus.Text = status;
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _serviceProvider?.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _serviceProvider?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}