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
            if (code.Length > 6000) code = code.Substring(0, 6000) + "...[truncated]";

            var prompt = $@"
                [ROLE: Senior Technical Writer]
                Analyze file: '{fileName}'.
                ### CODE
                {code}
                ### CONTEXT
                {context}
                ### OUTPUT TEMPLATE
                ## 📄 {fileName}
                **Type:** [Class/Form]
                ### 📘 Summary
                [Brief summary]
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
                Generate a Mermaid.js Class Diagram.
                ### FILES
                {projectSummary}
                ### RULES
                - Return ONLY raw mermaid code.
                - Start with 'classDiagram'.
                - NO markdown fences (```).
            ";
            return await CallOllama(prompt);
        }

        // --- NEW: SCHEMA GENERATOR ---
        public static async Task<string> GenerateDatabaseSchema(string dalCode)
        {
            var prompt = $@"
                [ROLE: Database Architect]
                Infer the SQL Schema from this C# Data Access Layer code.
                Generate a Mermaid.js ER Diagram.

                ### CODE SNIPPETS
                {dalCode}

                ### RULES
                - Look for 'SELECT * FROM TableName'.
                - Infer columns from parameters.
                - Return ONLY raw mermaid code.
                - Start with 'erDiagram'.
                - NO markdown fences.
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateReadme(string projectSummary, string repoUrl)
        {
            var prompt = $@"
                [ROLE: Senior DevOps Engineer]
                Write a **PROFESSIONAL README.MD**.
                
                ### REPO URL
                {repoUrl}
                ### CONTEXT
                {projectSummary}
                
                ### SECTIONS
                # [Project Name]
                ## 📖 Description
                ## ✨ Features
                ## 🛠️ Tech Stack
                ## 🚀 How to Run
                1. Clone: `git clone {repoUrl}`
                2. Restore: `dotnet restore`
                3. Run: `dotnet run`
                ## 🏗️ Architecture
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
                if (!response.IsSuccessStatusCode) return "AI Error.";
                dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                return json.response;
            }
            catch { return "Connection Failed."; }
        }
    }
}