using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Documenter
{
    public class GeminiAgent
    {
        // 1. POINT TO LOCALHOST (Your computer's local AI server)
        private const string OllamaUrl = "http://localhost:11434/api/generate";

        public static async Task<string> AnalyzeCode(HttpClient client, string fileName, string codeContent)
        {
            // Safety: Truncate very large files to prevent crashing the local model
            var safeCode = codeContent.Length > 15000 ? codeContent.Substring(0, 15000) : codeContent;

            // 2. THE "SENIOR DEVELOPER" PROMPT
            // This forces DeepSeek to write clean, scannable docs with emojis.
            var prompt = $@"
                You are a Senior Lead Developer writing technical documentation. 
                Analyze this code file: '{fileName}'.

                GUIDELINES:
                1. BE CONCISE. Use bullet points. No long paragraphs.
                2. SKIP BASICS. Do not explain syntax (e.g. 'int is a number'). Focus on BUSINESS LOGIC.
                3. VISUALS. Use emojis (📄, ⚡, 🔗) to make it scannable.

                OUTPUT FORMAT (Strict Markdown):
                ## 📄 {fileName}
                **Language:** [Language Name]
                
                ### ⚡ Key Features
                * [Point 1: What does this file actually DO?]
                * [Point 2: Important logic/algorithms]
                * [Point 3: Key database or API interactions]

                ### 🔗 Structure
                (If class-based, use a Mermaid classDiagram. If script, use a flowchart TD).
                - RETURN ONLY THE MERMAID CODE inside ```mermaid blocks.
                ```mermaid
                [Mermaid Code Here]
                ```
                ---
                CODE CONTEXT:
                {safeCode}
            ";

            // 3. OLLAMA REQUEST FORMAT
            var requestBody = new
            {
                model = "deepseek-coder", // The model you downloaded
                prompt = prompt,
                stream = false            // Get the full response at once (easier to handle)
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // 4. SEND TO LOCAL SERVER
                var response = await client.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode)
                    return $"⚠️ Error: Local AI is not running. Open CMD and type 'ollama serve'.";

                string responseJson = await response.Content.ReadAsStringAsync();

                // 5. PARSE OLLAMA RESPONSE
                dynamic data = JsonConvert.DeserializeObject(responseJson);
                return data?.response ?? "No response from Local AI.";
            }
            catch (Exception ex)
            {
                return $"❌ Local AI Error: {ex.Message}. Make sure Ollama is installed!";
            }
        }
    }
}