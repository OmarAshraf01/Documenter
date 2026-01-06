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
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "dist", "build", "venv", "__pycache__" };

        public Form1()
        {
            InitializeComponent();
            btnStart.Click += BtnStart_Click;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string repoUrl = txtUrl.Text;
            if (string.IsNullOrWhiteSpace(repoUrl)) { Log("❌ Enter a valid URL."); return; }

            btnStart.Enabled = false;

            // --- NEW PATH LOGIC ---
            // 1. Get the folder where the .exe is running
            string exeFolder = Application.StartupPath;

            // 2. Extract Project Name from URL (e.g., 'Newtonsoft.Json')
            string projectName = GetProjectNameFromUrl(repoUrl);

            // 3. Define where to Clone (Separate folder next to exe)
            string clonesRoot = Path.Combine(exeFolder, "Cloned_Repos");
            string targetCloneFolder = Path.Combine(clonesRoot, projectName);

            // 4. Define where to save PDF (Same folder as exe)
            string pdfFilename = $"{projectName}_Documentation.pdf";
            string pdfPath = Path.Combine(exeFolder, pdfFilename);

            string reportContent = $"# Documentation for {projectName}\nSource: {repoUrl}\n\n";

            try
            {
                // Clean up previous clone if it exists
                if (Directory.Exists(targetCloneFolder)) GitService.DeleteDirectory(targetCloneFolder);
                Directory.CreateDirectory(clonesRoot);

                Log($"⬇️ Cloning {projectName}...");
                Log($"📂 Destination: {targetCloneFolder}");

                await Task.Run(() => GitService.CloneRepository(repoUrl, targetCloneFolder));
                Log("✅ Clone Complete.");

                var allFiles = Directory.GetFiles(targetCloneFolder, "*.*", SearchOption.AllDirectories);
                var codeFiles = allFiles.Where(IsCodeFile).ToList();

                Log($"found {codeFiles.Count} code files. Starting Analysis...");

                using var client = new HttpClient();
                int counter = 1;

                foreach (var file in codeFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string code = await File.ReadAllTextAsync(file);

                    if (code.Length < 50 || code.Length > 30000) continue;

                    Log($"[{counter}/{codeFiles.Count}] 🧠 Analyzing {fileName}...");

                    string analysis = await GeminiAgent.AnalyzeCode(client, fileName, code);
                    reportContent += analysis + "\n\n";

                    await Task.Delay(4000);
                    counter++;
                }

                Log($"📄 Saving PDF as: {pdfFilename}...");
                PdfReport.Generate(pdfPath, reportContent);

                Log($"🎉 SUCCESS! PDF saved in app folder.");
                MessageBox.Show($"Documentation saved to:\n{pdfPath}", "Success");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                // OPTIONAL: Keep the clone? Comment this line out if you want to keep the downloaded files.
                GitService.DeleteDirectory(targetCloneFolder);
                btnStart.Enabled = true;
            }
        }

        private string GetProjectNameFromUrl(string url)
        {
            // Logic: splits 'github.com/User/Project' and takes 'Project'
            // Removes .git if present (e.g. Project.git -> Project)
            if (string.IsNullOrWhiteSpace(url)) return "UnknownProject";

            var uri = new Uri(url);
            string lastSegment = uri.Segments.Last();
            return lastSegment.Trim('/').Replace(".git", "");
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