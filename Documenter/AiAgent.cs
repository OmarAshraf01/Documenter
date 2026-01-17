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
        // Ensure you have this model: ollama pull qwen2.5-coder:1.5b
        private const string ModelName = "qwen2.5-coder:1.5b";

        public static async Task<string> AnalyzeCode(string fileName, string code, string context)
        {
            // Increased limit to capture more context
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
            // CHANGED: Requests a 'graph TD' (Flowchart) instead of 'classDiagram'.
            // This prevents the 'messy spiderweb' look and creates organized layers.
            var prompt = $@"
                [ROLE: System Architect]
                Create a High-Level Layered Architecture Diagram using Mermaid.js.
                Group the components into layers: UI (Forms), BLL (Logic), and DAL (Data).

                ### FILES
                {projectSummary}

                ### RULES
                - Use 'graph TD'.
                - Group nodes using 'subgraph UI', 'subgraph BLL', 'subgraph DAL'.
                - Connect the layers: UI --> BLL --> DAL.
                - Return ONLY valid Mermaid code.
                - NO markdown fences (```).
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateDatabaseSchema(string dalCode)
        {
            // CHANGED: Added STRICT rules to prevent the 'Syntax error in text' bug.
            var prompt = $@"
                [ROLE: Database Architect]
                Infer the Database Schema (ERD) from this code.

                ### CODE SNIPPETS
                {dalCode}

                ### RULES
                - Start with 'erDiagram'.
                - Use STRICT format: 'EntityName {{type name }}'.
                - **CRITICAL: NO SPACES in Entity names** (Use 'User_Table', NOT 'User Table').
                - Infer relationships (e.g., User ||--o{{Order}}).
                - Return ONLY valid Mermaid code.
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateReadme(string projectSummary, string repoUrl)
        {
            var prompt = $@"
                [ROLE: Senior Developer Advocate]
                Write a comprehensive README.md.

                ### REPO URL
                {repoUrl}
                ### FILES
                {projectSummary}

                ### SECTIONS
                # [Project Name]
                ## 📖 Overview
                ## ✨ Features
                ## 🏗️ Architecture (UI -> BLL -> DAL)
                ## 🚀 Setup & Usage
                ## 🛠️ Tech Stack
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
                if (responseToken == null) return "AI Error: AI response missing 'response' field.";

                string result = responseToken.ToString();

                // CLEANUP: Automatically remove markdown fences (```mermaid ... ```) 
                // This ensures the HTML renderer doesn't break if the AI ignores the "no fences" rule.
                return Regex.Replace(result, @"^```[a-z]*\s*|\s*```$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();
            }
            catch { return "Error: AI Connection Failed."; }
        }
    }
}