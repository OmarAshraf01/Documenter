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

        public static async Task<string> AnalyzeCode(HttpClient sharedClient, string fileName, string codeContent)
        {
            // --- FIX: CREATE A FRESH CLIENT FOR EVERY REQUEST ---
            // This solves the "Instance has already started" error because we make a new one each time.
            using var myClient = new HttpClient();

            // Give it 10 minutes to think (Prevents timeout errors)
            myClient.Timeout = TimeSpan.FromMinutes(10);

            var safeCode = codeContent.Length > 15000 ? codeContent.Substring(0, 15000) : codeContent;

            // THE "FRIENDLY EXPLAINER" PROMPT
            var prompt = $@"
                You are a Senior Technical Writer creating documentation for a Product Manager. 
                Your goal is to explain this code file ('{fileName}') simply and clearly.

                RULES:
                1. NO CODING ADVICE. Do not say 'You should improve this'. Do not say 'It seems you pasted code'.
                2. PLAIN ENGLISH. Avoid complex jargon. Explain *what* the code does for the business.
                3. USE EMOJIS. Make it visually engaging (e.g., 🚀, 🛡️, 💾).
                4. BE BRIEF. Bullet points only.

                OUTPUT FORMAT (Strict Markdown):
                ## 📄 {fileName}
                **Language:** [Language Name]

                ### 💡 What is this file?
                [1 sentence explanation in plain English. Example: 'This file handles user logins and keeps passwords safe.']

                ### ⚡ Key Features
                * 🛡️ **[Feature Name]:** [Simple explanation of what it does]
                * 💾 **[Feature Name]:** [Simple explanation]
                * 🚀 **[Feature Name]:** [Simple explanation]

                ### 🔗 Visual Structure
                (Create a simple Mermaid diagram to show how this file works).
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
                // Use 'myClient' (the fresh one) instead of 'sharedClient'
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