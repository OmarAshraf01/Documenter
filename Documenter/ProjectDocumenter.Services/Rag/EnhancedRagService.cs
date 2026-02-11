using ProjectDocumenter.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectDocumenter.Services.Rag
{
    /// <summary>
    /// Enhanced RAG service with better context matching
    /// </summary>
    public class EnhancedRagService : IRagService
    {
        private readonly Dictionary<string, string> _knowledgeBase = new();
        private static readonly HashSet<string> ValidExtensions = new()
        {
            ".cs", ".java", ".py", ".cpp", ".js", ".ts", ".sql", ".xml", ".json", ".go", ".rs"
        };

        public void IndexProject(string projectPath)
        {
            _knowledgeBase.Clear();

            if (!Directory.Exists(projectPath)) return;

            var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(f => ValidExtensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (var file in files)
            {
                try
                {
                    var key = Path.GetFileNameWithoutExtension(file);
                    if (!_knowledgeBase.ContainsKey(key))
                    {
                        _knowledgeBase[ key] = File.ReadAllText(file);
                    }
                }
                catch
                {
                    // Skip files that can't be read
                }
            }
        }

        public string GetContext(string code, int maxContextItems = 3)
        {
            var context = new StringBuilder();
            var matches = 0;

            foreach (var (fileName, fileContent) in _knowledgeBase)
            {
                // Skip if it's the same file
                if (code.Equals(fileContent, StringComparison.Ordinal)) continue;

                // Check if the filename is referenced in the code (whole word)
                if (Regex.IsMatch(code, $@"\b{Regex.Escape(fileName)}\b"))
                {
                    var snippet = fileContent.Length > 1500
                        ? fileContent.Substring(0, 1500) + "...(truncated)"
                        : fileContent;

                    context.AppendLine($"--- REFERENCE: {fileName} ---\n{snippet}\n");
                    matches++;

                    if (matches >= maxContextItems) break;
                }
            }

            return context.Length > 0
                ? context.ToString()
                : "No external dependencies found.";
        }

        public void Clear()
        {
            _knowledgeBase.Clear();
        }

        public IReadOnlyList<string> GetIndexedFiles()
        {
            return _knowledgeBase.Keys.ToList();
        }
    }
}
