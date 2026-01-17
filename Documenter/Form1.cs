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
        private readonly HashSet<string> _validExtensions = new() { ".cs", ".py", ".java", ".js", ".cpp" };
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs" };

        // Store selected path
        private string _selectedBasePath = string.Empty;

        public Form1()
        {
            InitializeComponent();
            btnStart.Click += BtnStart_Click;

            // Assuming you added a button named 'btnBrowse'
            btnBrowse.Click += BtnBrowse_Click;

            // Set default path to Desktop just in case
            _selectedBasePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            lblSelectedPath.Text = _selectedBasePath; // Assuming you have a label to show path
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

            // --- 1. FOLDER LOGIC ---
            string projectName = GetProjectNameFromUrl(repoUrl);

            // Create a specific folder for this Repo inside the selected path
            string projectRoot = Path.Combine(_selectedBasePath, projectName);
            string codeFolder = Path.Combine(projectRoot, "SourceCode");
            string docsFolder = Path.Combine(projectRoot, "Documentation");

            string htmlPath = Path.Combine(docsFolder, "Documentation.html");
            string pdfPath = Path.Combine(docsFolder, "Documentation.pdf");

            try
            {
                // Setup Directories
                if (Directory.Exists(codeFolder)) GitService.DeleteDirectory(codeFolder);
                Directory.CreateDirectory(codeFolder);
                Directory.CreateDirectory(docsFolder);

                // --- 2. CLONE ---
                Log($"⬇️ Cloning into: {codeFolder}...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, codeFolder));

                Log("🧠 Indexing...");
                RagService.IndexProject(codeFolder);

                // --- 3. TREE VIEW ---
                string projectTree = GenerateDirectoryTree(codeFolder);
                var htmlBuilder = new HtmlService();

                // --- 4. ANALYZE ---
                var summaryForAi = new StringBuilder();
                var files = Directory.GetFiles(codeFolder, "*.*", SearchOption.AllDirectories).Where(IsCodeFile).ToList();

                Log($"Found {files.Count} files. Starting Analysis...");

                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    string code = await File.ReadAllTextAsync(file);
                    if (code.Length < 10) continue;

                    Log($"Processing {name}...");

                    string context = RagService.GetContext(code);
                    string analysis = await AiAgent.AnalyzeCode(name, code, context);

                    htmlBuilder.AddMarkdown(analysis);

                    // Collect first few lines for the Diagram/Readme generator
                    string snippet = string.Join(" ", analysis.Split('\n').Take(5));
                    summaryForAi.AppendLine($"File: {name} Summary: {snippet}");
                }

                // --- 5. GENERATE DIAGRAM (New!) ---
                Log("📊 Generating Architecture Diagram...");
                string mermaidCode = await AiAgent.GenerateDiagram(summaryForAi.ToString());
                htmlBuilder.AddDiagram(mermaidCode);

                // --- 6. GENERATE README ---
                Log("📘 Generating README...");
                string readme = await AiAgent.GenerateReadme(summaryForAi.ToString());
                htmlBuilder.AddMarkdown(readme); // Append Readme

                // Add Tree View at the very start
                htmlBuilder.AddProjectStructure(projectTree);

                // --- 7. SAVE ---
                string finalHtml = htmlBuilder.GetHtml();
                await File.WriteAllTextAsync(htmlPath, finalHtml);

                Log("📄 Rendering PDF (Waiting for Diagrams)...");
                // Wait a bit for Mermaid JS to render inside Puppeteer
                await PdfService.ConvertHtmlToPdf(finalHtml, pdfPath);

                Log("✅ Done!");
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

        // ... (Keep IsCodeFile, Log, GenerateDirectoryTree, GetProjectNameFromUrl helpers from previous code) ...
        // Ensure you copy those helpers here!

        private string GenerateDirectoryTree(string rootPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Path.GetFileName(rootPath) + "/");
            // Simple recursive tree generation (Same as before)
            // ...
            return sb.ToString();
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

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}