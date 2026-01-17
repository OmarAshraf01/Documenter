using System;
using System.Collections.Generic;
using System.Diagnostics; // Needed to open PDF
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
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "dist", "build", "venv", "__pycache__", ".idea", ".vscode" };

        public Form1()
        {
            InitializeComponent();
            btnStart.Click += BtnStart_Click;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string repoUrl = txtUrl.Text;
            if (string.IsNullOrWhiteSpace(repoUrl)) { Log("❌ Enter URL."); return; }

            btnStart.Enabled = false;

            // --- 1. SETUP PATHS ---
            string projectName = GetProjectNameFromUrl(repoUrl);
            string tempFolder = Path.GetTempPath();
            string clonesRoot = Path.Combine(tempFolder, "Doc_Clones");
            string targetFolder = Path.Combine(clonesRoot, projectName);

            // Output files inside the CLONED REPO folder (as requested)
            string htmlPath = Path.Combine(targetFolder, "Documentation.html");
            string pdfPath = Path.Combine(targetFolder, "Documentation.pdf");

            try
            {
                // --- 2. CLONE & INDEX ---
                if (Directory.Exists(targetFolder)) GitService.DeleteDirectory(targetFolder);
                Directory.CreateDirectory(clonesRoot);

                Log($"⬇️ Cloning {projectName}...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, targetFolder));
                Log("🧠 Indexing project structure...");
                RagService.IndexProject(targetFolder);

                // --- 3. GENERATE PROJECT STRUCTURE (Tree) ---
                Log("🌳 Generating File Tree...");
                string projectTree = GenerateDirectoryTree(targetFolder);

                // --- 4. ANALYZE FILES ---
                var htmlBuilder = new HtmlService();
                // Add Tree to HTML immediately
                htmlBuilder.AddProjectStructure(projectTree);

                var summaryForReadme = new StringBuilder();
                var files = Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories)
                                     .Where(IsCodeFile).ToList();

                Log($"Found {files.Count} files. Starting Qwen Analysis...");

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string code = await File.ReadAllTextAsync(file);

                    if (code.Length < 10) continue;

                    Log($"Analysing {fileName}...");

                    string context = RagService.GetContext(code);
                    string analysis = await AiAgent.AnalyzeCode(fileName, code, context);

                    // Add Detailed Doc
                    htmlBuilder.AddMarkdown(analysis);

                    // Collect snippet for Readme
                    summaryForReadme.AppendLine($"File: {fileName}\nSummary: {string.Join(" ", analysis.Split('\n').Take(3))}");
                }

                // --- 5. GENERATE PROFESSIONAL README ---
                Log("📘 Writing 'How-To' Guide (README)...");
                string readme = await AiAgent.GenerateReadme(summaryForReadme.ToString());
                // Note: In HtmlService, we modified AddReadme to append it AFTER the tree but BEFORE the details.
                // However, since we appended the Tree first, we just need to insert the Readme now.
                // Actually, let's just append it. The HTML Service will handle order.
                htmlBuilder.AddReadme(readme);

                // --- 6. SAVE & CONVERT ---
                string finalHtml = htmlBuilder.GetHtml();
                await File.WriteAllTextAsync(htmlPath, finalHtml);
                Log($"✅ HTML saved inside repo.");

                Log("📄 Converting to PDF...");
                await PdfService.ConvertHtmlToPdf(finalHtml, pdfPath);
                Log($"✅ PDF saved: {pdfPath}");

                // --- 7. OPEN PDF AUTOMATICALLY ---
                Log("🚀 Opening PDF...");
                OpenPdf(pdfPath);

                MessageBox.Show($"Success!\nFiles saved in:\n{targetFolder}", "Done");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }

        // --- HELPER: Generate Tree Structure ---
        private string GenerateDirectoryTree(string rootPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Path.GetFileName(rootPath) + "/");
            GenerateTreeRecursive(rootPath, "", sb);
            return sb.ToString();
        }

        private void GenerateTreeRecursive(string dir, string indent, StringBuilder sb)
        {
            try
            {
                var dirs = Directory.GetDirectories(dir)
                    .Where(d => !_ignoredFolders.Contains(Path.GetFileName(d)))
                    .ToArray();
                var files = Directory.GetFiles(dir)
                    .Where(IsCodeFile)
                    .ToArray();

                for (int i = 0; i < dirs.Length; i++)
                {
                    bool isLastDir = (i == dirs.Length - 1) && (files.Length == 0);
                    sb.AppendLine($"{indent}{(isLastDir ? "└── " : "├── ")}{Path.GetFileName(dirs[i])}/");
                    GenerateTreeRecursive(dirs[i], indent + (isLastDir ? "    " : "│   "), sb);
                }

                for (int i = 0; i < files.Length; i++)
                {
                    bool isLastFile = (i == files.Length - 1);
                    sb.AppendLine($"{indent}{(isLastFile ? "└── " : "├── ")}{Path.GetFileName(files[i])}");
                }
            }
            catch { /* Ignore permission errors */ }
        }

        // --- HELPER: Open PDF ---
        private void OpenPdf(string path)
        {
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo(path) { UseShellExecute = true }
                }.Start();
            }
            catch (Exception ex) { Log("Could not open PDF automatically: " + ex.Message); }
        }

        private string GetProjectNameFromUrl(string url)
        {
            try { return new Uri(url).Segments.Last().Trim('/').Replace(".git", ""); }
            catch { return "ProjectDocs"; }
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
    }
}