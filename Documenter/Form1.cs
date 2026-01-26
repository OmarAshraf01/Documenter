using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Documenter
{
    public partial class Form1 : Form
    {
        // Added "documentation" to ignored folders to prevent the tool from documenting its own output
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "bin", "obj", "properties", "debug", "lib", "packages", "documentation" };
        private readonly HashSet<string> _validExtensions = new() { ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".sql", ".xml", ".config", ".html", ".css" };

        private string _selectedBasePath = string.Empty;
        private string _currentLogFile = string.Empty;
        private RealTimeView? _realTimeWindow;

        public Form1()
        {
            InitializeComponent();

            // Clear existing events to avoid duplication if designer attached them
            btnBrowse.Click -= BtnBrowse_Click;
            btnStart.Click -= BtnStart_Click;
            txtUrl.TextChanged -= TxtUrl_TextChanged; // New Event
            this.Load -= Form1_Load;

            // Attach Events
            btnBrowse.Click += BtnBrowse_Click;
            btnStart.Click += BtnStart_Click;
            txtUrl.TextChanged += TxtUrl_TextChanged; // New Event
            this.Load += Form1_Load;

            // Default path
            _selectedBasePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (lblSelectedPath != null) lblSelectedPath.Text = _selectedBasePath;

            // Set initial button state
            UpdateUiMode();
        }

        private async void Form1_Load(object? sender, EventArgs e)
        {
            if (lblStatus != null) lblStatus.Text = "Initializing Docker...";
            btnStart.Enabled = false;

            // Initialize Docker
            string result = await Task.Run(() => DockerService.InitializeAsync(Log));
            Log(result);

            if (!result.Contains("❌"))
            {
                btnStart.Enabled = true;
                if (lblStatus != null) lblStatus.Text = "Ready";
            }
        }

        // --- NEW: Handle UI changes based on URL text ---
        private void TxtUrl_TextChanged(object? sender, EventArgs e)
        {
            UpdateUiMode();
        }

        private void UpdateUiMode()
        {
            if (string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                // LOCAL FOLDER MODE
                btnStart.Text = "📄 Document Folder";
                if (lblStatus != null && lblStatus.Text == "Ready") lblStatus.Text = "Mode: Local Folder";
            }
            else
            {
                // GITHUB MODE
                btnStart.Text = "⬇️ Generate from Web";
                if (lblStatus != null && lblStatus.Text == "Ready") lblStatus.Text = "Mode: GitHub Clone";
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                // If in local mode, encourage picking a specific source folder
                if (string.IsNullOrWhiteSpace(txtUrl.Text))
                {
                    fbd.Description = "Select the SOURCE CODE folder to document";
                    fbd.UseDescriptionForTitle = true;
                }
                else
                {
                    fbd.Description = "Select folder to SAVE the repository";
                }

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _selectedBasePath = fbd.SelectedPath;
                    if (lblSelectedPath != null) lblSelectedPath.Text = _selectedBasePath;
                }
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            // --- 1. DETERMINE MODE & PATHS ---
            string repoUrl = txtUrl.Text.Trim();
            bool isGitMode = !string.IsNullOrWhiteSpace(repoUrl);

            string projectName;
            string projectRoot;
            string codeFolder;

            if (isGitMode)
            {
                // GitHub Mode: Clone INTO the selected path
                projectName = GetProjectNameFromUrl(repoUrl);
                projectRoot = Path.Combine(_selectedBasePath, projectName);
                codeFolder = Path.Combine(projectRoot, "SourceCode"); // We create a clean subfolder
            }
            else
            {
                // Local Mode: The selected path IS the project root
                projectRoot = _selectedBasePath;
                projectName = Path.GetFileName(_selectedBasePath);
                codeFolder = _selectedBasePath; // Analyze the selected folder directly
            }

            // Standardize Output Paths
            string docsFolder = Path.Combine(projectRoot, "Documentation");
            string htmlPath = Path.Combine(docsFolder, "Documentation.html");
            string pdfPath = Path.Combine(docsFolder, "Documentation.pdf");

            // --- 2. START PROCESS ---
            btnStart.Enabled = false;
            btnBrowse.Enabled = false;
            txtUrl.Enabled = false;

            _realTimeWindow = new RealTimeView();
            _realTimeWindow.Show();

            try
            {
                Log("🚀 Initializing Background Tasks...");
                Task browserDownloadTask = PdfService.PrepareBrowserAsync(Log);

                // Create Docs Folder (if it doesn't exist)
                Directory.CreateDirectory(docsFolder);
                _currentLogFile = Path.Combine(docsFolder, "log.txt");

                // --- 3. HANDLE CLONING (Only if Git Mode) ---
                if (isGitMode)
                {
                    // Safety check: Clean up previous runs only in Git Mode
                    if (Directory.Exists(codeFolder)) GitService.DeleteDirectory(codeFolder);
                    Directory.CreateDirectory(codeFolder);

                    Log($"⬇️ Cloning {projectName}...");
                    await Task.Run(() => GitService.CloneRepository(repoUrl, codeFolder));
                }
                else
                {
                    Log($"📂 using Local Folder: {codeFolder}");
                }

                // --- 4. START ANALYSIS (Shared Logic) ---
                Log("🧠 Indexing knowledge base...");
                RagService.IndexProject(codeFolder);

                var htmlBuilder = new HtmlService();
                var summaryForAi = new StringBuilder();
                var dbData = new StringBuilder();

                // Get Files (Using logic to ignore binary/system folders)
                var files = Directory.GetFiles(codeFolder, "*.*", SearchOption.AllDirectories)
                                            .Where(IsCodeFile)
                                            .OrderBy(f => f)
                                            .ToList();

                if (files.Count == 0)
                {
                    throw new Exception("No valid code files found in the selected directory!");
                }

                int total = files.Count;
                Log("🌳 Generating Folder Structure...");

                // Pass relative path logic so the tree looks nice
                htmlBuilder.InjectProjectStructure(GenerateTreeRecursively(codeFolder, "", codeFolder));

                if (progressBar1 != null) { progressBar1.Maximum = total; progressBar1.Value = 0; }

                // --- AI LOOP ---
                for (int i = 0; i < total; i++)
                {
                    string file = files[i];
                    string name = Path.GetFileName(file);

                    // Safety: Skip the documentation file we are currently writing to prevent loops
                    if (file.Contains(docsFolder)) continue;

                    string code = await File.ReadAllTextAsync(file);
                    if (string.IsNullOrWhiteSpace(code)) continue;

                    string msg = $"Analyzing ({i + 1}/{total}): {name}";
                    Log(msg);
                    if (lblStatus != null) lblStatus.Text = msg;
                    if (progressBar1 != null) progressBar1.Value = i + 1;

                    string context = RagService.GetContext(code);
                    string analysis = await AiAgent.AnalyzeCode(name, code, context);
                    htmlBuilder.AddMarkdown(analysis);
                    _realTimeWindow.AppendLog(name, analysis);

                    // Build summaries for README generation
                    string snippet = string.Join("\n", code.Split('\n').Take(30));
                    summaryForAi.AppendLine($"File: {name}\nType: {Path.GetExtension(name)}\n{snippet}\n");

                    // Heuristic for Database detection
                    string lower = name.ToLower();
                    if (lower.Contains("dal") || lower.Contains("model") || lower.Contains("entity") ||
                        lower.Contains("dto") || code.Contains("CREATE TABLE") ||
                        code.Contains("DbContext") || code.Contains("DbSet") || code.Contains("INSERT INTO"))
                    {
                        dbData.AppendLine($"--- {name} ---\n{code}\n");
                    }
                }

                // Database Analysis
                if (dbData.Length > 50)
                {
                    Log("🗄️ Analyzing Database Structure...");
                    string dbAnalysis = await AiAgent.AnalyzeDatabaseLogic(dbData.ToString());
                    if (!string.IsNullOrWhiteSpace(dbAnalysis) && !dbAnalysis.Contains("N/A"))
                    {
                        htmlBuilder.InjectDatabaseAnalysis(dbAnalysis);
                        _realTimeWindow.AppendLog("DATABASE ANALYSIS", dbAnalysis);
                    }
                }

                // README Generation
                Log("📘 Generating README...");
                // If local, we don't have a repo URL, pass "Local Project" instead
                string urlForReadme = isGitMode ? repoUrl : "Local Project";
                string readme = await AiAgent.GenerateReadme(summaryForAi.ToString(), urlForReadme);
                htmlBuilder.InjectReadme(readme);
                _realTimeWindow.AppendLog("README", readme);

                // PDF Generation
                Log("📄 Rendering PDF...");
                string html = htmlBuilder.GetHtml();
                await File.WriteAllTextAsync(htmlPath, html);

                if (!browserDownloadTask.IsCompleted)
                {
                    Log("⏳ Waiting for browser download to finish...");
                }
                await browserDownloadTask;

                await PdfService.ConvertHtmlToPdf(html, pdfPath);

                Log("🚀 Done!");
                if (lblStatus != null) lblStatus.Text = "Complete!";
                _realTimeWindow.AppendLog("SYSTEM", $"🎉 Generation Complete. Saved to: {docsFolder}");

                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStart.Enabled = true;
                btnBrowse.Enabled = true;
                txtUrl.Enabled = true;
            }
        }

        // Updated Tree Generation to handle relative paths better
        private string GenerateTreeRecursively(string currentDir, string indent, string rootDir)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var directories = Directory.GetDirectories(currentDir)
                    .Where(d => !_ignoredFolders.Contains(Path.GetFileName(d).ToLower()));

                foreach (var d in directories)
                {
                    sb.AppendLine($"{indent}📁 {Path.GetFileName(d)}/");
                    sb.Append(GenerateTreeRecursively(d, indent + "    ", rootDir));
                }

                var files = Directory.GetFiles(currentDir).Where(IsCodeFile);
                foreach (var f in files)
                {
                    sb.AppendLine($"{indent}📄 {Path.GetFileName(f)}");
                }
            }
            catch { }
            return sb.ToString();
        }

        private bool IsCodeFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (!_validExtensions.Contains(ext)) return false;
            foreach (var bad in _ignoredFolders)
                if (path.ToLower().Contains(Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar)) return false;
            return true;
        }

        private void Log(string msg)
        {
            if (lstLog.InvokeRequired) lstLog.Invoke(new Action<string>(Log), msg);
            else
            {
                lstLog.Items.Add($"{DateTime.Now:HH:mm:ss}: {msg}");
                lstLog.TopIndex = lstLog.Items.Count - 1;
                try { if (!string.IsNullOrEmpty(_currentLogFile)) File.AppendAllText(_currentLogFile, msg + "\n"); } catch { }
            }
        }

        private string GetProjectNameFromUrl(string url)
        {
            try { return new Uri(url).Segments.Last().Trim('/').Replace(".git", ""); }
            catch { return "ProjectDocs"; }
        }
    }
}