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
        private readonly HashSet<string> _validExtensions = new() { ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".c", ".h", ".go", ".rb", ".php" };
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "bin", "obj", "properties", ".idea" };
        private string _selectedBasePath = string.Empty;
        private string _currentLogFile = string.Empty;

        public Form1()
        {
            InitializeComponent();
            btnStart.Click -= BtnStart_Click; btnStart.Click += BtnStart_Click;
            btnBrowse.Click -= BtnBrowse_Click; btnBrowse.Click += BtnBrowse_Click;
            this.Load -= Form1_Load; this.Load += Form1_Load;

            _selectedBasePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (lblSelectedPath != null) lblSelectedPath.Text = _selectedBasePath;
        }

        private void Form1_Load(object sender, EventArgs e) { if (lblStatus != null) lblStatus.Text = "Ready."; }

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
            string repoUrl = txtUrl.Text;
            if (string.IsNullOrWhiteSpace(repoUrl)) { Log("❌ Error: Enter a URL."); return; }

            btnStart.Enabled = false; btnBrowse.Enabled = false;
            if (progressBar1 != null) progressBar1.Value = 0;

            string projectName = GetProjectNameFromUrl(repoUrl);
            string projectRoot = Path.Combine(_selectedBasePath, projectName);
            string codeFolder = Path.Combine(projectRoot, "SourceCode");
            string docsFolder = Path.Combine(projectRoot, "Documentation");
            string htmlPath = Path.Combine(docsFolder, "Documentation.html");
            string pdfPath = Path.Combine(docsFolder, "Documentation.pdf");

            try
            {
                if (Directory.Exists(codeFolder)) GitService.DeleteDirectory(codeFolder);
                Directory.CreateDirectory(codeFolder);
                Directory.CreateDirectory(docsFolder);
                _currentLogFile = Path.Combine(docsFolder, "log.txt");

                Log("⬇️ Cloning...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, codeFolder));
                Log("🧠 Indexing...");
                RagService.IndexProject(codeFolder);

                var htmlBuilder = new HtmlService();
                var summaryForAi = new StringBuilder();
                var dalSummary = new StringBuilder(); // Collects SQL/DAL info

                var files = Directory.GetFiles(codeFolder, "*.*", SearchOption.AllDirectories).Where(IsCodeFile).ToList();

                // 1. INJECT TREE
                htmlBuilder.InjectProjectStructure(GenerateDirectoryTree(codeFolder));

                if (progressBar1 != null) progressBar1.Maximum = files.Count;
                int processed = 0;

                foreach (var file in files)
                {
                    processed++;
                    string name = Path.GetFileName(file);
                    if (progressBar1 != null) progressBar1.Value = processed;
                    if (lblStatus != null) lblStatus.Text = $"Analyzing: {name}";
                    Log($"Analyzing: {name}...");

                    string code = await File.ReadAllTextAsync(file);
                    if (code.Length > 10)
                    {
                        string context = RagService.GetContext(code);
                        string analysis = await AiAgent.AnalyzeCode(name, code, context);
                        htmlBuilder.AddMarkdown(analysis);

                        string snippet = string.Join("\n", code.Split('\n').Take(15));
                        summaryForAi.AppendLine($"File: {name}\n{snippet}\n");

                        // Collect DB Logic for Schema
                        if (name.ToLower().Contains("dal") || code.ToLower().Contains("select"))
                        {
                            dalSummary.AppendLine($"--- {name} ---\n{code}\n");
                        }
                    }
                }

                // 2. DIAGRAMS
                Log("📊 Architecture Diagram...");
                htmlBuilder.InjectDiagram(await AiAgent.GenerateDiagram(summaryForAi.ToString()));

                // 3. DATABASE SCHEMA
                if (dalSummary.Length > 0)
                {
                    Log("🗄️ Database Schema...");
                    htmlBuilder.InjectDatabaseSchema(await AiAgent.GenerateDatabaseSchema(dalSummary.ToString()));
                }

                // 4. README
                Log("📘 Readme...");
                htmlBuilder.InjectReadme(await AiAgent.GenerateReadme(summaryForAi.ToString(), repoUrl));

                // 5. SAVE & PDF
                Log("📄 Rendering PDF...");
                string html = htmlBuilder.GetHtml();
                await File.WriteAllTextAsync(htmlPath, html);
                await PdfService.ConvertHtmlToPdf(html, pdfPath);

                Log("🚀 Done!");
                if (lblStatus != null) lblStatus.Text = "Complete!";
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnStart.Enabled = true; btnBrowse.Enabled = true;
            }
        }

        private string GenerateDirectoryTree(string root)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Path.GetFileName(root) + "/");
            try
            {
                foreach (var f in Directory.GetFiles(root).Where(IsCodeFile)) sb.AppendLine("├── " + Path.GetFileName(f));
                foreach (var d in Directory.GetDirectories(root).Where(d => !_ignoredFolders.Contains(Path.GetFileName(d))))
                {
                    sb.AppendLine("├── " + Path.GetFileName(d) + "/");
                    foreach (var f in Directory.GetFiles(d).Where(IsCodeFile)) sb.AppendLine("│   ├── " + Path.GetFileName(f));
                }
            }
            catch { }
            return sb.ToString();
        }

        private bool IsCodeFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (!_validExtensions.Contains(ext)) return false;
            foreach (var bad in _ignoredFolders) if (path.Contains(bad)) return false;
            return true;
        }

        private void Log(string msg)
        {
            if (lstLog.InvokeRequired) lstLog.Invoke(new Action<string>(Log), msg);
            else
            {
                lstLog.Items.Add($"{DateTime.Now.ToShortTimeString()}: {msg}");
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