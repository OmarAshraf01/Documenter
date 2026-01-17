using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Documenter
{
    public partial class Form1 : Form
    {
        // 1. Valid Extensions to Analyze
        private readonly HashSet<string> _validExtensions = new()
        {
            ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".c", ".h", ".go", ".rb", ".php", ".swift", ".kt"
        };

        // 2. Folders to Ignore
        private readonly string[] _ignoredFolders =
        {
            "node_modules", ".git", ".vs", "dist", "build", "venv", "__pycache__"
        };

        public Form1()
        {
            InitializeComponent();
            btnStart.Click += BtnStart_Click;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string repoUrl = txtUrl.Text;
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                Log("❌ Please enter a valid Repository URL.");
                return;
            }

            btnStart.Enabled = false;

            // --- SETUP PATHS ---
            string projectName = GetProjectNameFromUrl(repoUrl);
            string tempFolder = Path.GetTempPath();
            string clonesRoot = Path.Combine(tempFolder, "Doc_Clones");
            string targetFolder = Path.Combine(clonesRoot, projectName);

            // Output paths on Desktop
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string htmlPath = Path.Combine(desktopPath, $"{projectName}_Docs.html");
            string pdfPath = Path.Combine(desktopPath, $"{projectName}_Docs.pdf");

            try
            {
                // 1. CLEANUP & CLONE
                if (Directory.Exists(targetFolder)) GitService.DeleteDirectory(targetFolder);
                Directory.CreateDirectory(clonesRoot);

                Log($"⬇️ Cloning {projectName}...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, targetFolder));

                // 2. INDEX FOR RAG (Context)
                Log("🧠 Indexing project structure...");
                RagService.IndexProject(targetFolder);

                // 3. INITIALIZE HTML BUILDER
                var htmlBuilder = new HtmlService();
                var summaryForReadme = new StringBuilder();

                // Get all code files (filtering out Debug/Release artifacts)
                var files = Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories)
                                     .Where(IsCodeFile)
                                     .ToList();

                Log($"Found {files.Count} files. Starting Analysis...");

                // 4. ANALYZE FILES LOOP
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string code = await File.ReadAllTextAsync(file);

                    // Skip tiny files
                    if (code.Length < 10) continue;

                    Log($"Analysing {fileName}...");

                    // A. Get RAG Context (Related files)
                    string context = RagService.GetContext(code);

                    // B. Call AI (Qwen 1.5B)
                    string analysis = await AiAgent.AnalyzeCode(fileName, code, context);

                    // C. Add to HTML Report
                    htmlBuilder.AddMarkdown(analysis);

                    // D. Collect snippet for README generation (First 3 lines of analysis)
                    string snippet = string.Join(" ", analysis.Split('\n').Take(3));
                    summaryForReadme.AppendLine($"File: {fileName} - {snippet}");
                }

                // 5. GENERATE PROFESSIONAL README
                Log("📘 Generating User Guide / README...");
                string readme = await AiAgent.GenerateReadme(summaryForReadme.ToString());

                // Add README to the very top of the HTML
                htmlBuilder.AddReadme(readme);

                // 6. SAVE HTML (Intermediary Step)
                string finalHtml = htmlBuilder.GetHtml();
                await File.WriteAllTextAsync(htmlPath, finalHtml);
                Log($"✅ HTML saved to Desktop.");

                // 7. CONVERT TO PDF (Puppeteer/Chrome)
                Log("📄 Converting HTML to PDF (this ensures perfect tables)...");
                await PdfService.ConvertHtmlToPdf(finalHtml, pdfPath);

                Log("🎉 Done!");
                MessageBox.Show($"Documentation Generated!\n\n📄 PDF: {pdfPath}\n🌐 HTML: {htmlPath}", "Success");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
                MessageBox.Show($"Critical Error: {ex.Message}", "Error");
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }

        // Helper: Extract project name from URL (e.g., "github.com/user/MyRepo.git" -> "MyRepo")
        private string GetProjectNameFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "UnknownProject";
            try
            {
                var uri = new Uri(url);
                return uri.Segments.Last().Trim('/').Replace(".git", "");
            }
            catch
            {
                return "ProjectDocs";
            }
        }

        // Helper: Check if file is valid code
        private bool IsCodeFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (!_validExtensions.Contains(ext)) return false;

            foreach (var bad in _ignoredFolders)
            {
                if (path.Contains(Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar))
                    return false;
            }
            return true;
        }

        // Helper: Log to ListBox
        private void Log(string msg)
        {
            // InvokeRequired check handles threading if Log is called from async Tasks
            if (lstLog.InvokeRequired)
            {
                lstLog.Invoke(new Action<string>(Log), msg);
            }
            else
            {
                lstLog.Items.Add($"{DateTime.Now.ToShortTimeString()}: {msg}");
                lstLog.TopIndex = lstLog.Items.Count - 1;
            }
        }
    }
}