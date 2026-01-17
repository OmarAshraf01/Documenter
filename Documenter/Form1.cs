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
        // Added .sql, .xml, .config
        private readonly HashSet<string> _validExtensions = new() { ".cs", ".py", ".java", ".js", ".ts", ".cpp", ".sql", ".xml", ".config" };
        private readonly string[] _ignoredFolders = { "node_modules", ".git", ".vs", "bin", "obj", "properties", "debug", "lib", "packages" };

        private string _selectedBasePath = string.Empty;
        private string _currentLogFile = string.Empty;

        public Form1()
        {
            InitializeComponent();
            btnStart.Click += BtnStart_Click;
            btnBrowse.Click += BtnBrowse_Click;
            this.Load += Form1_Load;

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
            string repoUrl = txtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(repoUrl)) { MessageBox.Show("Enter URL"); return; }

            btnStart.Enabled = false; btnBrowse.Enabled = false;

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
                var erData = new StringBuilder();

                var files = Directory.GetFiles(codeFolder, "*.*", SearchOption.AllDirectories)
                                     .Where(IsCodeFile)
                                     .OrderBy(f => f) // Order alphabetically
                                     .ToList();

                Log("🌳 Generating Folder Structure...");
                // Improved Tree Logic
                htmlBuilder.InjectProjectStructure(GenerateTreeRecursively(codeFolder, ""));

                int total = files.Count;
                if (progressBar1 != null) { progressBar1.Maximum = total; progressBar1.Value = 0; }

                for (int i = 0; i < total; i++)
                {
                    string file = files[i];
                    string name = Path.GetFileName(file);

                    string msg = $"Analyzing ({i + 1}/{total}): {name}";
                    Log(msg);
                    if (lblStatus != null) lblStatus.Text = msg;
                    if (progressBar1 != null) progressBar1.Value = i + 1;
                    await Task.Delay(10); // UI Refresh

                    string code = await File.ReadAllTextAsync(file);
                    if (code.Length > 10)
                    {
                        string context = RagService.GetContext(code);
                        string analysis = await AiAgent.AnalyzeCode(name, code, context);
                        htmlBuilder.AddMarkdown(analysis);

                        string snippet = string.Join("\n", code.Split('\n').Take(20));
                        summaryForAi.AppendLine($"File: {name}\nType: {Path.GetExtension(name)}\n{snippet}\n");

                        string lower = name.ToLower();
                        if (lower.Contains("dal") || lower.Contains("model") || lower.Contains("entity") || lower.Contains("context") || code.Contains("CREATE TABLE"))
                        {
                            erData.AppendLine($"--- {name} ---\n{code}\n");
                        }
                    }
                }

                Log("📊 Generating Architecture...");
                string arch = await AiAgent.GenerateDiagram(summaryForAi.ToString());
                htmlBuilder.InjectDiagram(arch);

                if (erData.Length > 50)
                {
                    Log("🗄️ Generating Schema...");
                    string erd = await AiAgent.GenerateDatabaseSchema(erData.ToString());
                    htmlBuilder.InjectDatabaseSchema(erd);
                }

                Log("📘 Generating README...");
                string readme = await AiAgent.GenerateReadme(summaryForAi.ToString(), repoUrl);
                htmlBuilder.InjectReadme(readme);

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

        // Recursive tree generation for better visualization
        private string GenerateTreeRecursively(string dir, string indent)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var directories = Directory.GetDirectories(dir)
                    .Where(d => !_ignoredFolders.Contains(Path.GetFileName(d).ToLower()));

                foreach (var d in directories)
                {
                    sb.AppendLine($"{indent}📁 {Path.GetFileName(d)}/");
                    sb.Append(GenerateTreeRecursively(d, indent + "    "));
                }

                var files = Directory.GetFiles(dir).Where(IsCodeFile);
                foreach (var f in files)
                {
                    sb.AppendLine($"{indent}📄 {Path.GetFileName(f)}");
                }
            }
            catch { }
            return sb.ToString();
        }

        private bool IsCodeFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (!_validExtensions.Contains(ext)) return false;
            foreach (var bad in _ignoredFolders)
                if (path.ToLower().Contains(Path.DirectorySeparatorChar + bad + Path.DirectorySeparatorChar)) return false;
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