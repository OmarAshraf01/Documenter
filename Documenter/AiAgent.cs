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
            // Truncate huge files
            if (code.Length > 6000) code = code.Substring(0, 6000) + "...[truncated]";

            var prompt = $@"
                [ROLE: Senior Technical Writer]
                Analyze this file: '{fileName}'.

                ### 📂 CODE
                {code}

                ### 🧩 CONTEXT
                {context}

                ### INSTRUCTIONS
                1. If this is a UI file (*.Designer.cs), ONLY list the controls (Buttons, Labels).
                2. If this is Logic (*.cs), explain the functionality.
                3. **FORMATTING**: Use Markdown Tables for properties/methods.

                ### OUTPUT TEMPLATE
                ## 📄 {fileName}
                **Type:** [Class/Form/Interface]

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
                Generate a **Mermaid.js Class Diagram** code block based on these files.
                Focus on the BLL (Business Logic) and DAL (Data Access) relationships.

                ### FILES
                {projectSummary}

                ### OUTPUT
                ONLY return the mermaid code starting with 'classDiagram'. Do not add explanations.
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateReadme(string projectSummary, string repoUrl)
        {
            var prompt = $@"
                [ROLE: Senior DevOps Engineer]
                Write a **PROFESSIONAL DEPLOYMENT GUIDE & README** for this C# project.

                ### REPOSITORY URL
                {repoUrl}

                ### PROJECT CONTEXT
                {projectSummary}

                ### REQUIRED SECTIONS (Use Markdown):
                1. **Project Title & Description**: Professional summary.
                2. **Key Features**: Bullet points of what it does.
                3. **Prerequisites**: What needs to be installed? (e.g., .NET SDK, SQL Server, Visual Studio).
                4. **🚀 Deployment & Setup Guide** (Crucial Section):
                   - Step 1: Clone the Repo (`git clone {repoUrl}`)
                   - Step 2: Database Setup (Explain connection strings configuration).
                   - Step 3: Restore Dependencies (`dotnet restore`).
                   - Step 4: Run the Application.
                5. **Architecture**: Brief mention of the BLL/DAL layers.

                Make it look like a top-tier GitHub repository README.
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