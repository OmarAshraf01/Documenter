using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Documenter
{
    public partial class Form1 : Form
    {
        // 1. Valid Extensions
        private readonly HashSet<string> _validExtensions = new()
        {
            ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".c", ".h", ".go", ".rb", ".php", ".swift", ".kt"
        };

        // 2. Ignore Junk Folders
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "dist", "build", "venv", "__pycache__"};

        public Form1()
        {
            InitializeComponent();
            // REMOVED: btnStart.Click += ... 
            // Why? Because the Designer already handles this. Adding it here causes the "Line 51" error.
            btnStart.Click += BtnStart_Click;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string repoUrl = txtUrl.Text;
            if (string.IsNullOrWhiteSpace(repoUrl)) { Log("❌ Enter a valid URL."); return; }

            btnStart.Enabled = false;

            // --- PATH LOGIC ---
            string exeFolder = Application.StartupPath;
            string projectName = GetProjectNameFromUrl(repoUrl);
            string clonesRoot = Path.Combine(exeFolder, "Cloned_Repos");
            string targetCloneFolder = Path.Combine(clonesRoot, projectName);
            string pdfFilename = $"{projectName}_Documentation.pdf";
            string pdfPath = Path.Combine(exeFolder, pdfFilename);

            string reportContent = $"# Documentation for {projectName}\nSource: {repoUrl}\n\n";

            try
            {
                // 1. CLEANUP & CLONE
                if (Directory.Exists(targetCloneFolder)) GitService.DeleteDirectory(targetCloneFolder);
                Directory.CreateDirectory(clonesRoot);

                Log($"⬇️ Cloning {projectName}...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, targetCloneFolder));
                Log("✅ Clone Complete.");

                // 2. FIND FILES
                var allFiles = Directory.GetFiles(targetCloneFolder, "*.*", SearchOption.AllDirectories);
                var codeFiles = allFiles.Where(IsCodeFile).ToList();

                Log($"found {codeFiles.Count} code files. Starting Analysis...");

                int counter = 1;

                foreach (var file in codeFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string code = await File.ReadAllTextAsync(file);

                    // Skip empty files. (We removed the upper limit so the Agent handles truncation)
                    if (code.Length < 50) continue;

                    Log($"[{counter}/{codeFiles.Count}] 🧠 Analyzing {fileName}...");

                    // --- CALL THE FAIL-SAFE AGENT ---
                    // We pass 'null' for the client because the Agent now creates a FRESH connection internally
                    string analysis = await GeminiAgent.AnalyzeCode(null, fileName, code);

                    // --- CHECK FOR SKIPS/ERRORS ---
                    if (analysis.Contains("SKIPPED") || analysis.Contains("Error") || analysis.Contains("⚠️"))
                    {
                        Log($"⚠️ Skipped {fileName} (Timeout or Error). Moving on...");
                        reportContent += $"## 📂 File: {fileName}\n\n**Status:** ⚠️ Analysis Timed Out or Failed.\n\n";
                    }
                    else
                    {
                        reportContent += analysis + "\n\n";
                        Log($"✅ {fileName} Done!");
                    }

                    counter++;
                    // No Delay needed anymore because the Local AI is the bottleneck, not the network.
                }

                // 3. GENERATE PDF
                Log($"📄 Saving PDF as: {pdfFilename}...");
                PdfReport.Generate(pdfPath, reportContent);

                Log($"🎉 SUCCESS! PDF saved.");
                MessageBox.Show($"Documentation saved to:\n{pdfPath}", "Success");
            }
            catch (Exception ex)
            {
                Log($"❌ Critical Error: {ex.Message}");
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                // Cleanup (Optional)
                GitService.DeleteDirectory(targetCloneFolder);
                btnStart.Enabled = true;
            }
        }

        private string GetProjectNameFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "UnknownProject";
            var uri = new Uri(url);
            return uri.Segments.Last().Trim('/').Replace(".git", "");
        }

        private bool IsCodeFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (!_validExtensions.Contains(ext)) return false;

            foreach (var badFolder in _ignoredFolders)
            {
                if (path.Contains(Path.DirectorySeparatorChar + badFolder + Path.DirectorySeparatorChar))
                    return false;
            }
            return true;
        }

        private void Log(string msg)
        {
            lstLog.Items.Add($"{DateTime.Now.ToShortTimeString()}: {msg}");
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }
    }
}