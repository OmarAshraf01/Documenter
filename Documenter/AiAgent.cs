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
                Analyze this source file: '{fileName}'.
                
                ### CODE
                {code}
                ### CONTEXT
                {context}
                
                ### OUTPUT FORMAT (Markdown)
                ## 📄 {fileName}
                **Type:** [Class/Interface/Component]
                ### 📘 Summary
                [Concise summary of functionality]
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
                Create a Mermaid.js Class Diagram.
                ### FILES SUMMARY
                {projectSummary}
                ### RULES
                - Return ONLY raw mermaid code.
                - Start with 'classDiagram'.
                - NO markdown fences (```).
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateDatabaseSchema(string dalCode)
        {
            var prompt = $@"
                [ROLE: Database Architect]
                Infer the Database Schema (ER Diagram) based on these data models/DAL code.
                
                ### CODE SNIPPETS
                {dalCode}

                ### RULES
                - Return ONLY raw mermaid code.
                - Start with 'erDiagram'.
                - Infer relationships based on ID naming (e.g. UserID in Orders table).
                - NO markdown fences.
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateReadme(string projectSummary, string repoUrl)
        {
            var prompt = $@"
                [ROLE: Senior Developer Advocate]
                Write a detailed **README.md** for this project.

                ### REPO URL
                {repoUrl}
                ### PROJECT CONTEXT
                {projectSummary}

                ### REQUIRED SECTIONS
                # [Project Name]
                ## 📖 Overview
                ## ✨ Key Features
                ## 🛠️ Tech Stack
                ## 🚀 How to Use (Step-by-Step)
                *Explain exactly how to setup, configure, and run this application based on the code analysis.*
                ## 🏗️ Architecture
            ";
            return await CallOllama(prompt);
        }

        private static async Task<string> CallOllama(string prompt)
        {
            // CHANGED: Timeout increased to 20 minutes
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };

            var payload = new { model = ModelName, prompt = prompt, stream = false };
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode) return "AI Error: " + response.ReasonPhrase;

                dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                return json.response;
            }
            catch { return "Error: AI Connection Failed or Timed Out."; }
        }
    }
}