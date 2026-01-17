using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Documenter
{
    public class AiAgent
    {
        private const string OllamaUrl = "http://localhost:11434/api/generate";
        private const string ModelName = "qwen2.5-coder:1.5b";

        // 1. Analyze a Single Code File
        public static async Task<string> AnalyzeCode(string fileName, string code, string context)
        {
            // Truncate huge files
            if (code.Length > 6000) code = code.Substring(0, 6000) + "...[truncated]";

            var prompt = $@"
                [ROLE: Expert Technical Writer]
                Analyze this file: '{fileName}'.

                ### 📂 CODE
                {code}

                ### 🧩 CONTEXT
                {context}

                ### INSTRUCTIONS
                1. IF '*.Designer.cs': Briefly describe UI controls only.
                2. IF '*.cs': Explain Logic, Methods, and Data Flow.
                3. **CRITICAL**: Format tables perfectly using Markdown.
                
                ### OUTPUT FORMAT (Markdown)
                ## 📄 {fileName}
                **Type:** [Class/Form/Interface]

                ### 📘 Summary
                [Brief description]

                ### 🛠️ Key Components
                | Name | Type | Description |
                |---|---|---|
                | [Name] | [Type] | [Description] |
            ";

            return await CallOllama(prompt);
        }

        // 2. Generate the "README / User Guide"
        public static async Task<string> GenerateReadme(string projectSummary)
        {
            var prompt = $@"
                [ROLE: Product Manager]
                Write a PROFESSIONAL README.MD and USER GUIDE for this project based on the file summaries below.

                ### 📂 PROJECT FILE SUMMARIES
                {projectSummary}

                ### TASK
                Write a Github-style README that includes:
                1. **Project Title & Description** (Infer from the summaries).
                2. **Features List** (What can this app do?).
                3. **Getting Started** (How to run it?).
                4. **Architecture Overview** (How BLL/DAL/UI connect).

                Make it look professional.
            ";

            return await CallOllama(prompt);
        }

        private static async Task<string> CallOllama(string prompt)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var payload = new { model = ModelName, prompt = prompt, stream = false };

            try
            {
                var response = await client.PostAsync(OllamaUrl,
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode) return "⚠️ AI Error.";

                dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                return json.response;
            }
            catch { return "⚠️ Connection Failed."; }
        }
    }
}