using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Documenter
{
    public class GeminiAgent
    {
        private const string OllamaUrl = "http://localhost:11434/api/generate";

        public static async Task<string> AnalyzeCode(HttpClient ignoredClient, string fileName, string codeContent)
        {
            // 1. FAIL-SAFE: Create a fresh connection for every file
            using var myClient = new HttpClient();

            // 2. HARD LIMIT: If AI takes more than 3 minutes, kill it and move on.
            myClient.Timeout = TimeSpan.FromMinutes(3);

            // 3. SAFETY CUT: If code is massive (>10k chars), cut it so AI doesn't crash.
            var safeCode = codeContent.Length > 10000 ? codeContent.Substring(0, 10000) + "\n...(truncated)..." : codeContent;

            // 4. ARCHITECT PROMPT (The good one you asked for)
            var prompt = $@"
                You are a Senior System Architect. Analyze this C# file: '{fileName}'.
                
                YOUR TASK:
                1. Define the class and its role.
                2. List Variables/Properties.
                3. List Methods/Functions.
                4. Explain relationships (Inheritance/Dependencies).

                OUTPUT FORMAT (Strict Markdown):
                ## 📂 File: {fileName}
                **Type:** [Class / Interface / Form]
                
                ### 🏗️ Structure & Logic
                [Brief explanation of what this file does.]

                ### 🔗 Relationships
                * **Inherits:** [Parent Class]
                * **Uses:** [List other classes it calls]

                ### 📦 Variables
                | Name | Type | Description |
                |---|---|---|
                | [Name] | [Type] | [Brief Desc] |

                ### 🛠️ Methods
                * **[MethodName]**: [What does it do?]

                ### 📐 Visual Diagram
                ```mermaid
                classDiagram
                class {fileName.Replace(".", "_")} {{
                    +[Property]
                    +[Method]()
                }}
                ```
                ---
                CODE:
                {safeCode}
            ";

            var requestBody = new
            {
                model = "deepseek-coder",
                prompt = prompt,
                stream = false
            };

            try
            {
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send request and wait (up to 3 mins)
                var response = await myClient.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode)
                    return $"⚠️ AI Error: {response.StatusCode}. Is Ollama running?";

                string responseJson = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(responseJson);
                return data?.response ?? "No response.";
            }
            catch (TaskCanceledException)
            {
                // This catches the "15 mins stuck" issue!
                return "⚠️ SKIPPED: File was too complex and timed out (Limit: 3 mins).";
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }
    }
}