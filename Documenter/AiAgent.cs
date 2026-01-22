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
                [ROLE: Technical Writer]
                Analyze this file: '{fileName}'.
                
                ### CODE
                {code}
                ### CONTEXT
                {context}
                
                ### STRICT OUTPUT RULES
                1. Markdown only.
                2. NO conversational text ('Here is the analysis').
                3. NO closing remarks.

                ### FORMAT
                ## 📄 {fileName}
                **Type:** [Class/Interface]
                ### 📘 Summary
                [One sentence summary]
                ### 🛠️ Key Components
                | Name | Type | Description |
                |---|---|---|
                | [Name] | [Type] | [Description] |
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> AnalyzeDatabaseLogic(string dalCode)
        {
            var prompt = $@"
                [ROLE: Senior Backend Developer]
                Analyze the provided code snippets (DAL/SQL).
                Create a **Markdown Table** summarizing the database entities.
                ### CODE SNIPPETS
                {dalCode}
                ### INSTRUCTIONS
                1. Identify Table Names/Entities.
                2. Describe what they store.
                3. Do NOT draw diagrams. Output text/table only.
                4. If no database logic is found, return 'N/A'.
                ### OUTPUT FORMAT
                ## 🗄️ Database Structure
                The system appears to use the following data structure:
                | Entity / Table | Inferred Fields | Purpose |
                |---|---|---|
                | [Name] | [Fields] | [Description] |
            ";
            return await CallOllama(prompt);
        }

        public static async Task<string> GenerateReadme(string projectSummary, string repoUrl)
        {
            var prompt = $@"
                [ROLE: Senior Developer]
                Write a professional README.md.
                ### REPO URL
                {repoUrl}
                ### CODE SUMMARY
                {projectSummary}
                ### STRICT OUTPUT RULES
                1. Markdown only.
                2. NO conversational text.
                ### FORMAT
                # [Project Name]
                ## 📖 Description
                [Professional description]
                ## ✨ Key Features
                [Bullet points]
                ## 🛠️ Tech Stack
                [List]
                ## 🚀 Setup
                1. Clone: `git clone {repoUrl}`
                2. [Step 2]
                3. [Step 3]
            ";
            return await CallOllama(prompt);
        }

        private static async Task<string> CallOllama(string prompt)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
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
                result = Regex.Replace(result, @"^Here is.*?:", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                result = Regex.Replace(result, @"Note:.*", "", RegexOptions.IgnoreCase);
                return Regex.Replace(result, @"^```[a-z]*\s*|\s*```$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();
            }
            catch { return "Error: AI Connection Failed."; }
        }
    }
}