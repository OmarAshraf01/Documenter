using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Documenter
{
    public static class RagService
    {
        // Stores all project code in memory (Key = FileName, Value = Code)
        private static Dictionary<string, string> KnowledgeBase = new Dictionary<string, string>();

        // Call this ONCE after cloning to "read" the whole project
        public static void IndexProject(string folderPath)
        {
            KnowledgeBase.Clear();
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs") || f.EndsWith(".java") || f.EndsWith(".py") || f.EndsWith(".cpp"));

            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                // Store unique files only
                if (!KnowledgeBase.ContainsKey(name))
                {
                    KnowledgeBase[name] = File.ReadAllText(file);
                }
            }
        }

        // Finds code related to the current file being analyzed
        public static string GetContext(string currentCode)
        {
            StringBuilder context = new StringBuilder();
            int matches = 0;

            foreach (var kvp in KnowledgeBase)
            {
                string className = kvp.Key;

                // RAG LOGIC: If the current file mentions another class (e.g., "new UserBLL()"), 
                // but isn't defining it, grab that class's code.
                if (currentCode.Contains(className) && !currentCode.Contains($"class {className}"))
                {
                    // Truncate to 1500 chars to avoid crashing the AI with too much text
                    string snippet = kvp.Value.Length > 1500 ? kvp.Value.Substring(0, 1500) + "...(truncated)" : kvp.Value;

                    context.AppendLine($"--- REFERENCE: {className}.cs ---\n{snippet}\n");
                    matches++;
                }

                // Limit to 3 related files to keep it fast
                if (matches >= 3) break;
            }

            return context.Length > 0 ? context.ToString() : "No external dependencies found.";
        }
    }
}