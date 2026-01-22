using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Documenter
{
    public static class RagService
    {
        // Stores all project code: Key = ClassName/FileName, Value = Code Content
        private static Dictionary<string, string> KnowledgeBase = new Dictionary<string, string>();

        // 1. Valid extensions to index
        private static readonly HashSet<string> ValidExtensions = new()
        { ".cs", ".java", ".py", ".cpp", ".js", ".ts", ".sql", ".xml", ".json" };

        public static void IndexProject(string folderPath)
        {
            KnowledgeBase.Clear();
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => ValidExtensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (var file in files)
            {
                // We use the file name (without extension) as the 'Key' (e.g., "UserBLL")
                string key = Path.GetFileNameWithoutExtension(file);

                // Avoid duplicates
                if (!KnowledgeBase.ContainsKey(key))
                {
                    KnowledgeBase[key] = File.ReadAllText(file);
                }
            }
        }

        public static string GetContext(string currentCode)
        {
            StringBuilder context = new StringBuilder();
            int matches = 0;

            // Iterate through every file in our knowledge base
            foreach (var kvp in KnowledgeBase)
            {
                string otherFileName = kvp.Key;
                string otherFileContent = kvp.Value;

                // RULE 1: Don't include the file itself as context
                // (Simple check: if the content is identical, skip it)
                if (currentCode.Equals(otherFileContent)) continue;

                // RULE 2: Use Regex for "Whole Word" matching.
                // This prevents "User" matching inside "UserList" or "SuperUser".
                // We look for the other file's name inside the current code.
                if (Regex.IsMatch(currentCode, $@"\b{Regex.Escape(otherFileName)}\b"))
                {
                    // Truncate to save token space (1500 chars is usually enough for context)
                    string snippet = otherFileContent.Length > 1500
                        ? otherFileContent.Substring(0, 1500) + "...(truncated)"
                        : otherFileContent;

                    context.AppendLine($"--- REFERENCE: {otherFileName} ---\n{snippet}\n");
                    matches++;
                }

                // RULE 3: Stop after 3 relevant files to keep the prompt fast
                if (matches >= 3) break;
            }

            return context.Length > 0 ? context.ToString() : "No external dependencies found.";
        }
    }
}