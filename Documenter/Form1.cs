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
            ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".c", ".h", ".go", ".rb", ".php", ".swift"
        };

        // 2. Ignore Junk Folders
        private readonly string[] _ignoredFolders = { "node_modules", "bin", "obj", ".git", ".vs", "dist", "build", "venv", "__pycache__" };

        public Form1()
        {
            InitializeComponent();

            // IMPORTANT: This line manually connects the button click.
            // Even if the Designer is confused, this code will force it to work.
            btnStart.Click += BtnStart_Click;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string repoUrl = txtUrl.Text;
            if (string.IsNullOrWhiteSpace(repoUrl)) { Log("❌ Enter a valid URL."); return; }

            btnStart.Enabled = false;
            string tempFolder = Path.Combine(Path.GetTempPath(), "Documenter_Agent_" + Guid.NewGuid().ToString().Substring(0, 5));
            string reportContent = $"# Documentation for {repoUrl}\n\n";

            try
            {
                Log($"⬇️ Cloning {repoUrl}...");
                await Task.Run(() => GitService.CloneRepository(repoUrl, tempFolder));
                Log("✅ Clone Complete.");

                var allFiles = Directory.GetFiles(tempFolder, "*.*", SearchOption.AllDirectories);
                var codeFiles = allFiles.Where(IsCodeFile).ToList();

                Log($"📂 Found {codeFiles.Count} code files. Starting Analysis...");

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

                    await Task.Delay(4000); // 4-second delay for Free Tier
                    counter++;
                }

                Log("📄 Generating PDF...");
                string pdfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Documenter_Report.pdf");
                PdfReport.Generate(pdfPath, reportContent);

                Log($"🎉 DONE! Saved to Desktop: {pdfPath}");
                MessageBox.Show("Documentation Created Successfully!", "Success");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                GitService.DeleteDirectory(tempFolder);
                btnStart.Enabled = true;
            }
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