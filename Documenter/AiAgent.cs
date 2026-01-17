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

        public static async Task<string> AnalyzeCode(string fileName, string code, string context)
        {
            if (code.Length > 8000) code = code.Substring(0, 8000) + "...[truncated]";

            var prompt = $@"
                [ROLE: Expert Technical Writer]
                Analyze file: '{fileName}'.

                ### 📂 CODE
                {code}

                ### 🧩 CONTEXT
                {context}

                ### RULES
                1. Use **Standard Markdown Tables** for properties/methods. 
                   - Format: | Name | Type | Description |
                   - ALWAYS include a blank line before the table.
                2. If '*.Designer.cs': Only summarize UI controls.
                3. If '*.cs': Explain Logic.

                ### OUTPUT FORMAT
                ## 📄 {fileName}
                **Type:** [Class/Form]

                ### 📘 Summary
                [Brief description]

                ### 🛠️ Key Components
                | Name | Type | Description |
                |---|---|---|
                | [Name] | [Type] | [Description] |
            ";

            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateDiagram(string projectSummary)
        {
            var prompt = $@"
                [ROLE: System Architect]
                Based on these file summaries, generate a **Mermaid.js Class Diagram** representing the relationship between BLL, DAL, and Forms.

                ### FILES
                {projectSummary}

                ### OUTPUT
                Return ONLY the mermaid code. Start with 'classDiagram'.
                Example:
                classDiagram
                  class LoginBLL
                  class LoginDAL
                  LoginBLL --> LoginDAL
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateReadme(string projectSummary)
        {
            var prompt = $@"
                [ROLE: Product Manager]
                Write a concise README.MD for this project.
                
                ### FILES
                {projectSummary}

                ### FORMAT
                # Project Name
                ## Description
                ## Features
                ## How to Run
            ";
            return await CallOllama(prompt);
        }

        private static async Task<string> CallOllama(string prompt)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var payload = new { model = ModelName, prompt = prompt, stream = false };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(OllamaUrl, content);
                if (!response.IsSuccessStatusCode) return "⚠️ AI Error.";

                dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                return json.response;
            }
            catch { return "⚠️ Connection Failed."; }
        }
    }
}