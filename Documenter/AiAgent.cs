using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                [ROLE: Senior Technical Writer]
                Analyze this source file: '{fileName}'.
                
                ### CODE
                {code}
                ### CONTEXT
                {context}
                
                ### OUTPUT FORMAT (Markdown)
                ## 📄 {fileName}
                **Type:** [Class/Interface/Form]
                ### 📘 Summary
                [Concise summary of responsibility]
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
                Create a High-Level Layered Architecture Diagram using Mermaid.js.
                
                ### FILES
                {projectSummary}

                ### RULES
                - Use 'graph TD'.
                - Group nodes using 'subgraph UI', 'subgraph BLL', 'subgraph DAL', 'subgraph Core' (infer appropriate layers).
                - Connect the layers logically (e.g., UI --> BLL --> DAL).
                - Return ONLY valid Mermaid code.
                - NO markdown fences (```).
                - IF NO CLEAR ARCHITECTURE: Just map the main file dependencies.
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateDatabaseSchema(string dalCode)
        {
            var prompt = $@"
                [ROLE: Database Architect]
                Analyze the code below. If it contains SQL tables, Entity Framework Models, or DTOs, generate a Mermaid ER Diagram.
                
                **CRITICAL RULE:** IF NO DATABASE DEFINITIONS ARE FOUND, RETURN THE STRING 'N/A' ONLY. Do not invent a schema.

                ### CODE SNIPPETS
                {dalCode}

                ### RULES
                - Start with 'erDiagram'.
                - Use STRICT format: 'EntityName {{ type name }}'. 
                - **NO SPACES in Entity names** (Use 'User_Table', NOT 'User Table').
                - Return ONLY valid Mermaid code.
                - NO markdown fences.
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateReadme(string projectSummary, string repoUrl)
        {
            var prompt = $@"
                [ROLE: Senior Developer Advocate]
                Write a professional, comprehensive README.md.

                ### REPO URL
                {repoUrl}
                ### CODE SUMMARY
                {projectSummary}

                ### OUTPUT FORMAT
                # [Project Name]

                ![Platform](https://img.shields.io/badge/Platform-Windows%7CLinux-blue)
                ![Language](https://img.shields.io/badge/Language-Inferred-purple)

                ## 📖 Description
                [Write a compelling, professional 2-paragraph description of what the project does, inferred from the code.]

                ## ✨ Key Features
                [Bullet points of specific features found in the code, e.g., 'User Authentication', 'PDF Generation'.]

                ## 🛠️ Tech Stack
                [List languages and frameworks used.]

                ## ⚙️ Prerequisites
                [List software needed to run this, e.g., Visual Studio, SQL Server, Node.js]

                ## 🚀 Installation & Setup Guide
                1. Clone the repository: `git clone {repoUrl}`
                2. [Step inferred from code, e.g., 'Open Solution in VS', 'Run npm install']
                3. [Step inferred, e.g., 'Update connection string in App.config']

                ## 🎮 How to Use
                [Explain how to run the application.]

                ## 🐛 Troubleshooting
                | Issue | Solution |
                |---|---|
                | [Common issue 1 inferred] | [Solution] |
            ";
            return await CallOllama(prompt);
        }

        private static async Task<string> CallOllama(string prompt)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
            var payload = new { model = ModelName, prompt = prompt, stream = false };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode) return "AI Error: " + response.ReasonPhrase;

                var contentString = await response.Content.ReadAsStringAsync();
                JObject? json = JsonConvert.DeserializeObject<JObject>(contentString);
                if (json == null) return "AI Error: Empty AI response.";

                JToken? responseToken = json["response"];
                if (responseToken == null) return "AI Error: Missing response field.";

                string result = responseToken.ToString();

                // CLEANUP: Remove markdown fences
                return Regex.Replace(result, @"^```[a-z]*\s*|\s*```$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();
            }
            catch { return "Error: AI Connection Failed."; }
        }
    }
}