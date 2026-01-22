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
        // 1. Class-Level Variables
        private readonly HashSet<string> _validExtensions = new() { ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".sql", ".xml", ".config", ".html", ".css" };
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "bin", "obj", "properties", "debug", "lib", "packages" };

        private string _selectedBasePath = string.Empty;
        private string _currentLogFile = string.Empty;
        private RealTimeView _realTimeWindow;

        public Form1()
        {
            InitializeComponent();

            // Fix double-click event subscription bugs
            btnBrowse.Click -= BtnBrowse_Click;
            btnStart.Click -= BtnStart_Click;
            this.Load -= Form1_Load;

            btnBrowse.Click += BtnBrowse_Click;
            btnStart.Click += BtnStart_Click;
            this.Load += Form1_Load;

            _selectedBasePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (lblSelectedPath != null) lblSelectedPath.Text = _selectedBasePath;
        }

        private async void Form1_Load(object? sender, EventArgs e)
        {
            if (lblStatus != null) lblStatus.Text = "Initializing...";

            // Disable start button while loading Docker
            btnStart.Enabled = false;

            // Call the Docker Service and pass the Log function so users see progress
            string result = await Task.Run(() => DockerService.InitializeAsync(Log));

            Log(result);
            if (lblStatus != null) lblStatus.Text = result.Contains("❌") ? "Error" : "Ready";

            // Re-enable button only if Docker started successfully
            if (!result.Contains("❌"))
            {
                btnStart.Enabled = true;
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _selectedBasePath = fbd.SelectedPath;
                    if (lblSelectedPath != null) lblSelectedPath.Text = _selectedBasePath;
                }
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            // --- SCOPE FIX: Define these at the very top of the method ---
            string repoUrl = txtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(repoUrl)) { MessageBox.Show("Enter URL"); return; }

            // Define paths immediately so they are visible in 'finally' or lower blocks if needed
            string projectName = GetProjectNameFromUrl(repoUrl);
            string projectRoot = Path.Combine(_selectedBasePath, projectName);
            string codeFolder = Path.Combine(projectRoot, "SourceCode");
            string docsFolder = Path.Combine(projectRoot, "Documentation");
            string htmlPath = Path.Combine(docsFolder, "Documentation.html");
            string pdfPath = Path.Combine(docsFolder, "Documentation.pdf");

            btnStart.Enabled = false;
            btnBrowse.Enabled = false;

            // Launch Real-Time Window
            _realTimeWindow = new RealTimeView();
            _realTimeWindow.Show();

            try
            {
                // Directory Setup
                if (Directory.Exists(codeFolder)) GitService.DeleteDirectory(codeFolder);
                Directory.CreateDirectory(codeFolder);
                Directory.CreateDirectory(docsFolder);
                _currentLogFile = Path.Combine(docsFolder, "log.txt");

                Log("⬇️ Cloning repository...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, codeFolder));

                Log("🧠 Indexing knowledge base...");
                RagService.IndexProject(codeFolder);

                var htmlBuilder = new HtmlService();
                var summaryForAi = new StringBuilder();
                var dbData = new StringBuilder();

                // Get Files
                var files = Directory.GetFiles(codeFolder, "*.*", SearchOption.AllDirectories)
                                        .Where(IsCodeFile)
                                        .OrderBy(f => f)
                                        .ToList();

                // --- SCOPE FIX: Define 'total' before using it ---
                int total = files.Count;

                Log("🌳 Generating Folder Structure...");
                htmlBuilder.InjectProjectStructure(GenerateTreeRecursively(codeFolder, ""));

                if (progressBar1 != null) { progressBar1.Maximum = total; progressBar1.Value = 0; }

                // --- MAIN LOOP ---
                for (int i = 0; i < total; i++)
                {
                    string file = files[i];
                    string name = Path.GetFileName(file);

                    // --- SCOPE FIX: Define 'code' inside the loop, before checking it ---
                    string code = await File.ReadAllTextAsync(file);

                    if (string.IsNullOrWhiteSpace(code)) continue;

                    string msg = $"Analyzing ({i + 1}/{total}): {name}";
                    Log(msg);
                    if (lblStatus != null) lblStatus.Text = msg;
                    if (progressBar1 != null) progressBar1.Value = i + 1;

                    // 1. Analyze Code
                    string context = RagService.GetContext(code);
                    string analysis = await AiAgent.AnalyzeCode(name, code, context);
                    htmlBuilder.AddMarkdown(analysis);
                    _realTimeWindow.AppendLog(name, analysis);

                    // 2. Build Summary for README
                    string snippet = string.Join("\n", code.Split('\n').Take(30));
                    summaryForAi.AppendLine($"File: {name}\nType: {Path.GetExtension(name)}\n{snippet}\n");

                    // 3. Database Collection (Checks 'code' variable)
                    string lower = name.ToLower();
                    if (lower.Contains("dal") || lower.Contains("model") || lower.Contains("entity") ||
                        lower.Contains("dto") || code.Contains("CREATE TABLE") ||
                        code.Contains("DbContext") || code.Contains("DbSet") || code.Contains("INSERT INTO"))
                    {
                        dbData.AppendLine($"--- {name} ---\n{code}\n");
                    }
                }

                // --- POST-LOOP GENERATION ---

                // Database Analysis (Text Table)
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

                // README Generation (Uses 'repoUrl' which is defined at the top)
                Log("📘 Generating README...");
                string readme = await AiAgent.GenerateReadme(summaryForAi.ToString(), repoUrl);
                htmlBuilder.InjectReadme(readme);
                _realTimeWindow.AppendLog("README", readme);

                // PDF Generation (Uses 'pdfPath' which is defined at the top)
                Log("📄 Rendering PDF...");
                string html = htmlBuilder.GetHtml();
                await File.WriteAllTextAsync(htmlPath, html);
                await PdfService.ConvertHtmlToPdf(html, pdfPath);

                Log("🚀 Done!");
                if (lblStatus != null) lblStatus.Text = "Complete!";

                _realTimeWindow.AppendLog("SYSTEM", "🎉 Generation Complete. Opening PDF...");

                // Open PDF
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnStart.Enabled = true;
                btnBrowse.Enabled = true;
            }
        }

        private string GenerateTreeRecursively(string dir, string indent)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var directories = Directory.GetDirectories(dir)
                    .Where(d => !_ignoredFolders.Contains(Path.GetFileName(d).ToLower()));

                foreach (var d in directories)
                {
                    sb.AppendLine($"{indent}📁 {Path.GetFileName(d)}/");
                    sb.Append(GenerateTreeRecursively(d, indent + "    "));
                }

                var files = Directory.GetFiles(dir).Where(IsCodeFile);
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