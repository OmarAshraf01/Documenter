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
        private readonly HashSet<string> _validExtensions = new() { ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".c", ".h", ".go", ".rb", ".php", ".swift", ".kt" };
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "bin", "obj", ".idea", ".vscode", "properties" };
        private string _selectedBasePath = string.Empty;

        public Form1()
        {
            InitializeComponent();
            btnStart.Click += BtnStart_Click;
            btnBrowse.Click += BtnBrowse_Click;
            _selectedBasePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            lblSelectedPath.Text = _selectedBasePath;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _selectedBasePath = fbd.SelectedPath;
                    lblSelectedPath.Text = _selectedBasePath;
                }
            }
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string repoUrl = txtUrl.Text;
            if (string.IsNullOrWhiteSpace(repoUrl)) { Log("❌ Enter URL."); return; }

            btnStart.Enabled = false;
            btnBrowse.Enabled = false;

            string projectName = GetProjectNameFromUrl(repoUrl);
            string projectRoot = Path.Combine(_selectedBasePath, projectName);
            string codeFolder = Path.Combine(projectRoot, "SourceCode");
            string docsFolder = Path.Combine(projectRoot, "Documentation");
            string htmlPath = Path.Combine(docsFolder, "Documentation.html");
            string pdfPath = Path.Combine(docsFolder, "Documentation.pdf");

            try
            {
                // 1. PREPARE FOLDERS
                if (Directory.Exists(codeFolder)) GitService.DeleteDirectory(codeFolder);
                Directory.CreateDirectory(codeFolder);
                Directory.CreateDirectory(docsFolder);

                // 2. CLONE & INDEX
                Log($"⬇️ Cloning into: {codeFolder}...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, codeFolder));

                Log("🧠 Indexing Project...");
                RagService.IndexProject(codeFolder);

                // 3. GENERATE VISUAL TREE
                string projectTree = GenerateDirectoryTree(codeFolder);

                // 4. ANALYZE FILES
                var htmlBuilder = new HtmlService();
                var summaryForReadme = new StringBuilder();
                var files = Directory.GetFiles(codeFolder, "*.*", SearchOption.AllDirectories).Where(IsCodeFile).ToList();

                Log($"Found {files.Count} files. Starting Analysis...");

                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    string code = await File.ReadAllTextAsync(file);
                    if (code.Length < 10) continue;

                    Log($"Analyzing {name}...");

                    string context = RagService.GetContext(code);
                    string analysis = await AiAgent.AnalyzeCode(name, code, context);

                    // Add to detailed docs (we append this later)
                    // For now, we store it or just let HtmlBuilder hold onto the object logic
                    // Actually, let's append directly to builder for details, 
                    // BUT we need the README at the top.

                    // Hack: We will add the detailed analysis to a separate StringBuilder first
                    // so we can insert the Readme before it.
                }

                // RE-ARCHITECTING THE ORDER:
                // 1. Tree (Already Added)
                htmlBuilder.AddProjectStructure(projectTree);

                // 2. Prepare Summaries for AI
                foreach (var file in files)
                {
                    string code = await File.ReadAllTextAsync(file);
                    string name = Path.GetFileName(file);
                    // Just take first 500 chars for summary to save time/tokens
                    string snippet = code.Length > 500 ? code.Substring(0, 500) : code;
                    summaryForReadme.AppendLine($"File: {name}\nSnippet: {snippet}\n");
                }

                // 3. GENERATE DIAGRAM
                Log("📊 Generating Architecture Diagram...");
                string mermaidCode = await AiAgent.GenerateDiagram(summaryForReadme.ToString());
                htmlBuilder.AddDiagram(mermaidCode);

                // 4. GENERATE PROFESSIONAL README
                Log("📘 Writing Deployment Guide & README...");
                string readme = await AiAgent.GenerateReadme(summaryForReadme.ToString(), repoUrl);
                htmlBuilder.AddReadme(readme);

                // 5. ADD DETAILED FILE DOCS (Now we add them to HTML)
                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    string code = await File.ReadAllTextAsync(file);
                    if (code.Length < 10) continue;
                    string context = RagService.GetContext(code);
                    string analysis = await AiAgent.AnalyzeCode(name, code, context);
                    htmlBuilder.AddMarkdown(analysis);
                }

                // 6. SAVE & CONVERT
                string finalHtml = htmlBuilder.GetHtml();
                await File.WriteAllTextAsync(htmlPath, finalHtml);
                Log($"✅ HTML Saved.");

                Log("📄 Rendering PDF...");
                await PdfService.ConvertHtmlToPdf(finalHtml, pdfPath);

                Log("🚀 Opening PDF...");
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });

                MessageBox.Show($"Documentation Ready!\nLocation: {docsFolder}", "Success");
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

        // --- BETTER TREE GENERATION ---
        private string GenerateDirectoryTree(string rootPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Path.GetFileName(rootPath) + "/");
            PrintTree(rootPath, "", sb);
            return sb.ToString();
        }

        private void PrintTree(string dirPath, string prefix, StringBuilder sb)
        {
            var dirs = Directory.GetDirectories(dirPath)
                .Where(d => !_ignoredFolders.Contains(Path.GetFileName(d))).ToArray();
            var files = Directory.GetFiles(dirPath).Where(IsCodeFile).ToArray();

            for (int i = 0; i < dirs.Length; i++)
            {
                bool isLast = (i == dirs.Length - 1) && (files.Length == 0);
                sb.AppendLine(prefix + (isLast ? "└── " : "├── ") + Path.GetFileName(dirs[i]) + "/");
                PrintTree(dirs[i], prefix + (isLast ? "    " : "│   "), sb);
            }

            for (int i = 0; i < files.Length; i++)
            {
                bool isLast = (i == files.Length - 1);
                sb.AppendLine(prefix + (isLast ? "└── " : "├── ") + Path.GetFileName(files[i]));
            }
        }

        private bool IsCodeFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (!_validExtensions.Contains(ext)) return false;
            foreach (var bad in _ignoredFolders)
                if (path.Contains(Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar)) return false;
            return true;
        }

        private void Log(string msg)
        {
            if (lstLog.InvokeRequired) lstLog.Invoke(new Action<string>(Log), msg);
            else { lstLog.Items.Add($"{DateTime.Now.ToShortTimeString()}: {msg}"); lstLog.TopIndex = lstLog.Items.Count - 1; }
        }

        private string GetProjectNameFromUrl(string url)
        {
            try { return new Uri(url).Segments.Last().Trim('/').Replace(".git", ""); }
            catch { return "ProjectDocs"; }
        }
    }
}