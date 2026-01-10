using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Documenter
{
    public partial class Form1 : Form
    {
        private readonly HashSet<string> _validExtensions = new() { ".cs", ".py", ".java", ".js", ".cpp" };
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "bin", "obj" };

        public Form1()
        {
            InitializeComponent();
            btnStart.Click += BtnStart_Click;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string repoUrl = txtUrl.Text;
            if (string.IsNullOrWhiteSpace(repoUrl)) { Log("❌ Enter a URL."); return; }

            btnStart.Enabled = false;
            string exeFolder = Application.StartupPath;
            string projectName = new Uri(repoUrl).Segments.Last().Trim('/').Replace(".git", "");
            string clonesRoot = Path.Combine(exeFolder, "Cloned_Repos");
            string targetFolder = Path.Combine(clonesRoot, projectName);
            string pdfPath = Path.Combine(exeFolder, $"{projectName}_Docs.pdf");

            string reportContent = $"# Documentation for {projectName}\n\n";

            try
            {
                // 1. CLONE
                if (Directory.Exists(targetFolder)) GitService.DeleteDirectory(targetFolder);
                Directory.CreateDirectory(clonesRoot);
                Log($"⬇️ Cloning {projectName}...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, targetFolder));

                // 2. INDEX FILES (RAG) - CRITICAL STEP
                Log("🧠 Indexing project structure...");
                RagService.IndexProject(targetFolder);

                // 3. ANALYZE FILES
                var allFiles = Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories)
                                        .Where(IsCodeFile).ToList();

                Log($"Found {allFiles.Count} files. Starting Qwen 2.5 Analysis...");

                int counter = 1;
                foreach (var file in allFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string code = await File.ReadAllTextAsync(file);

                    if (code.Length < 50) continue;

                    Log($"[{counter}/{allFiles.Count}] Analyzing {fileName}...");

                    // 4. GET CONTEXT (RAG)
                    string context = RagService.GetContext(code);

                    // 5. CALL AI (Docker)
                    string analysis = await AiAgent.AnalyzeCode(fileName, code, context);

                    if (analysis.Contains("Error"))
                        Log($"⚠️ {fileName}: {analysis}");
                    else
                    {
                        reportContent += analysis + "\n\n";
                        Log($"✅ {fileName} Done.");
                    }
                    counter++;
                }

                // 6. SAVE PDF
                Log("📄 Generating PDF...");
                PdfReport.Generate(pdfPath, reportContent);
                MessageBox.Show($"Saved to:\n{pdfPath}", "Success");
            }
            catch (Exception ex)
            {
                Log($"❌ Critical Error: {ex.Message}");
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnStart.Enabled = true;
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
            lstLog.Items.Add($"{DateTime.Now.ToShortTimeString()}: {msg}");
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }
    }
}