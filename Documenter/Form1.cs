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
        // Allowed file extensions for analysis
        private readonly HashSet<string> _validExtensions = new() { ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".php", ".go", ".html", ".css", ".sql" };

        // Folders to ignore during scanning
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "bin", "obj", "properties", "debug", "lib", "vendor" };

        private string _selectedBasePath = string.Empty;
        private string _currentLogFile = string.Empty;

        public Form1()
        {
            InitializeComponent();

            // --- CRITICAL FIX: Connect Events Manually ---
            btnStart.Click += BtnStart_Click;
            btnBrowse.Click += BtnBrowse_Click;
            this.Load += Form1_Load;

            // Set default path to Desktop
            _selectedBasePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (lblSelectedPath != null) lblSelectedPath.Text = _selectedBasePath;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (lblStatus != null) lblStatus.Text = "Ready to generate docs.";
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
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

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string repoUrl = txtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                MessageBox.Show("Please enter a valid Git Repository URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Disable UI
            btnStart.Enabled = false;
            btnBrowse.Enabled = false;
            txtUrl.Enabled = false;

            // Setup Paths
            string projectName = GetProjectNameFromUrl(repoUrl);
            string projectRoot = Path.Combine(_selectedBasePath, projectName);
            string codeFolder = Path.Combine(projectRoot, "SourceCode");
            string docsFolder = Path.Combine(projectRoot, "Documentation");
            string htmlPath = Path.Combine(docsFolder, "Documentation.html");
            string pdfPath = Path.Combine(docsFolder, "Documentation.pdf");

            try
            {
                // 1. Prepare Directories
                if (Directory.Exists(codeFolder)) GitService.DeleteDirectory(codeFolder);
                Directory.CreateDirectory(codeFolder);
                Directory.CreateDirectory(docsFolder);
                _currentLogFile = Path.Combine(docsFolder, "log.txt");

                // 2. Clone Repository
                Log("⬇️ Cloning Repository...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, codeFolder));

                // 3. Index for RAG (AI Memory)
                Log("🧠 Indexing Codebase...");
                RagService.IndexProject(codeFolder);

                // 4. Initialize HTML Builder
                var htmlBuilder = new HtmlService();
                var summaryForAi = new StringBuilder(); // For Diagrams/Readme
                var erData = new StringBuilder();       // For Database Schema

                // Get all valid code files
                var files = Directory.GetFiles(codeFolder, "*.*", SearchOption.AllDirectories)
                                     .Where(IsCodeFile)
                                     .ToList();

                // --- STEP 5: GENERATE FOLDER TREE (First Page) ---
                Log("🌳 Generating Folder Structure...");
                string treeStructure = GenerateDirectoryTree(codeFolder);
                htmlBuilder.InjectProjectStructure(treeStructure);

                // Setup Progress Bar
                int totalFiles = files.Count;
                if (progressBar1 != null)
                {
                    progressBar1.Minimum = 0;
                    progressBar1.Maximum = totalFiles;
                    progressBar1.Value = 0;
                }

                // --- STEP 6: ANALYZE FILES LOOP ---
                for (int i = 0; i < totalFiles; i++)
                {
                    string file = files[i];
                    string fileName = Path.GetFileName(file);

                    // Update UI
                    string progressMsg = $"Analyzing ({i + 1}/{totalFiles}): {fileName}";
                    Log(progressMsg);
                    if (lblStatus != null) lblStatus.Text = progressMsg;
                    if (progressBar1 != null) progressBar1.Value = i + 1;

                    // Allow UI to refresh
                    await Task.Delay(10);

                    // Read Code
                    string code = await File.ReadAllTextAsync(file);

                    // Skip tiny files
                    if (code.Length < 10) continue;

                    // AI Analysis
                    string context = RagService.GetContext(code);
                    string analysis = await AiAgent.AnalyzeCode(fileName, code, context);
                    htmlBuilder.AddMarkdown(analysis);

                    // Collect data for high-level summaries
                    // Truncate to avoid token limits
                    string snippet = string.Join("\n", code.Split('\n').Take(20));
                    summaryForAi.AppendLine($"File: {fileName}\n{snippet}\n");

                    // Detect Database Logic for ERD
                    string lowerName = fileName.ToLower();
                    if (lowerName.Contains("dal") || lowerName.Contains("model") ||
                        lowerName.Contains("entity") || lowerName.Contains("context") ||
                        lowerName.Contains("dto") || code.Contains("CREATE TABLE") || code.Contains("class"))
                    {
                        erData.AppendLine($"--- {fileName} ---\n{code}\n");
                    }
                }

                // --- STEP 7: GENERATE DIAGRAMS ---
                Log("📊 Generating Architecture Diagram...");
                string architectureDiagram = await AiAgent.GenerateDiagram(summaryForAi.ToString());
                htmlBuilder.InjectDiagram(architectureDiagram);

                // --- STEP 8: GENERATE ERD (If DB found) ---
                if (erData.Length > 50)
                {
                    Log("🗄️ Database Logic Detected. Generating ER Diagram...");
                    string erDiagram = await AiAgent.GenerateDatabaseSchema(erData.ToString());
                    htmlBuilder.InjectDatabaseSchema(erDiagram);
                }
                else
                {
                    Log("ℹ️ No significant database code found. Skipping ERD.");
                }

                // --- STEP 9: GENERATE README ---
                Log("📘 Generating README & How-To...");
                string readmeMd = await AiAgent.GenerateReadme(summaryForAi.ToString(), repoUrl);

                // Save actual README.md file too
                await File.WriteAllTextAsync(Path.Combine(docsFolder, "README.md"), readmeMd);

                // Inject into PDF
                htmlBuilder.InjectReadme(readmeMd);

                // --- STEP 10: RENDER PDF ---
                Log("📄 Rendering PDF (Please wait)...");
                string finalHtml = htmlBuilder.GetHtml();

                // Save HTML for debugging
                await File.WriteAllTextAsync(htmlPath, finalHtml);

                // Convert to PDF
                await PdfService.ConvertHtmlToPdf(finalHtml, pdfPath);

                // Success!
                Log("🚀 Success! Documentation Generated.");
                if (lblStatus != null) lblStatus.Text = "Completed Successfully!";

                // Open the PDF
                try
                {
                    Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
                }
                catch { Log("⚠️ PDF created but could not open automatically."); }
            }
            catch (Exception ex)
            {
                Log($"❌ Critical Error: {ex.Message}");
                MessageBox.Show($"An error occurred:\n{ex.Message}\n\nSee log.txt for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable UI
                btnStart.Enabled = true;
                btnBrowse.Enabled = true;
                txtUrl.Enabled = true;
                if (progressBar1 != null) progressBar1.Value = 0;
            }
        }

        private string GenerateDirectoryTree(string root)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Path.GetFileName(root) + "/");
            try
            {
                foreach (var f in Directory.GetFiles(root).Where(IsCodeFile))
                    sb.AppendLine("├── " + Path.GetFileName(f));

                foreach (var d in Directory.GetDirectories(root).Where(d => !_ignoredFolders.Contains(Path.GetFileName(d).ToLower())))
                {
                    sb.AppendLine("├── " + Path.GetFileName(d) + "/");
                    foreach (var f in Directory.GetFiles(d).Where(IsCodeFile))
                        sb.AppendLine("│   ├── " + Path.GetFileName(f));
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[Tree Generation Error: {ex.Message}]");
            }
            return sb.ToString();
        }

        private bool IsCodeFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (!_validExtensions.Contains(ext)) return false;

            // Check full path for ignored folders to be safe
            string lowerPath = path.ToLower();
            foreach (var bad in _ignoredFolders)
                if (lowerPath.Contains(Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar))
                    return false;

            return true;
        }

        private void Log(string msg)
        {
            // Thread-safe logging
            if (lstLog.InvokeRequired)
            {
                lstLog.Invoke(new Action<string>(Log), msg);
            }
            else
            {
                string entry = $"{DateTime.Now.ToShortTimeString()}: {msg}";
                lstLog.Items.Add(entry);
                lstLog.TopIndex = lstLog.Items.Count - 1;

                try
                {
                    if (!string.IsNullOrEmpty(_currentLogFile))
                        File.AppendAllText(_currentLogFile, entry + Environment.NewLine);
                }
                catch { /* Ignore file lock errors during logging */ }
            }
        }

        private string GetProjectNameFromUrl(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return "ProjectDocs";

                var uri = new Uri(url);
                string segment = uri.Segments.Last();
                return segment.Trim('/').Replace(".git", "");
            }
            catch
            {
                return "ProjectDocs_" + DateTime.Now.Ticks;
            }
        }
    }
}