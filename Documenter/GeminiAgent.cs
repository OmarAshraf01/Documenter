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
            // --- FIX: CREATE A FRESH CLIENT FOR EVERY REQUEST ---
            // This solves the "Instance has already started" error 100%.
            using var myClient = new HttpClient();

            // Give it 10 minutes to think (Solves the Timeout error)
            myClient.Timeout = TimeSpan.FromMinutes(10);

            var safeCode = codeContent.Length > 15000 ? codeContent.Substring(0, 15000) : codeContent;

            // THE "NON-TECHNICAL FRIENDLY" PROMPT
            var prompt = $@"
                You are a Friendly Coding Mentor. You are explaining this file ('{fileName}') to a beginner student.
                
                GOAL: Make them understand *why* this file exists and what problem it solves.

                RULES:
                1. NO ROBOT TALK. Don't say 'This class instantiates objects'. Say 'This tool builds the user accounts'.
                2. BE ENCOURAGING & SIMPLE. Use clear, plain English.
                3. USE EMOJIS. Make it look like a blog post.
                4. KEEP IT SHORT.

                OUTPUT FORMAT (Strict Markdown):
                ## 📄 {fileName}
                **Language:** [Language Name]

                ### 💡 In Plain English
                [2-3 sentences explaining the file like you are talking to a friend. Use an analogy if possible.]

                ### ⚡ What it actually does
                * 🛡️ **[Feature Name]:** [Simple explanation]
                * 💾 **[Feature Name]:** [Simple explanation]
                * 🚀 **[Feature Name]:** [Simple explanation]

                ### 🔗 How it connects
                (A simple Mermaid diagram showing the flow).
                ```mermaid
                [Mermaid Code Here]
                ```
                ---
                CODE CONTEXT:
                {safeCode}
            ";

            var requestBody = new
            {
                model = "deepseek-coder",
                prompt = prompt,
                stream = false
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // We use 'myClient' here, so the old one doesn't cause errors
                var response = await myClient.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode)
                    return $"⚠️ Error: Local AI is not running. Open CMD and type 'ollama serve'.";

                string responseJson = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(responseJson);
                return data?.response ?? "No response from Local AI.";
            }
            catch (Exception ex)
            {
                return $"❌ Local AI Error: {ex.Message}";
            }
        }
    }
}