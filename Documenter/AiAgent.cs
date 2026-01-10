using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Documenter
{
    public class AiAgent
    {
        // Connect to Docker on Localhost
        private const string OllamaUrl = "http://localhost:11434/api/generate";
        private const string ModelName = "qwen2.5-coder:1.5b";

        public static async Task<string> AnalyzeCode(string fileName, string codeContent, string context)
        {
            using var client = new HttpClient();
            // Qwen 7B on CPU can be slow. Set timeout to 10 minutes.
            client.Timeout = TimeSpan.FromMinutes(10);

            // Truncate the main file if it's huge
            string safeCode = codeContent.Length > 8000 ? codeContent.Substring(0, 8000) + "...(file truncated)" : codeContent;

            var prompt = $@"
                [ROLE: Senior System Architect]
                Analyze this file: '{fileName}'.

                ### 📂 CODE TO ANALYZE
                {safeCode}

                ### 🧩 RELATED CONTEXT (References found in project)
                {context}

                ### TASK
                1. Identify the file type (Class/Form/Interface).
                2. Explain the logic clearly. USE THE CONTEXT to explain external calls (e.g. 'It calls donorBLL to save data').
                3. List key methods and their purpose.

                ### OUTPUT FORMAT (Markdown)
                ## 📂 File: {fileName}
                **Type:** [Class Type]
                
                ### 🏗️ Logic & Structure
                [Explanation...]

                ### 🔗 Relationships
                * **Dependencies:** [List dependencies found in Context]

                ### 🛠️ Key Methods
                * **[Method Name]**: [Explanation]
            ";

            var requestBody = new
            {
                model = ModelName,
                prompt = prompt,
                stream = false
            };

            try
            {
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode)
                    return $"⚠️ AI Error: {response.StatusCode}. Is Docker running?";

                string responseJson = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(responseJson);
                return data?.response ?? "No response.";
            }
            catch (Exception ex)
            {
                return $"❌ Connection Error: {ex.Message}. Make sure Docker is running Qwen.";
            }
        }
    }
}