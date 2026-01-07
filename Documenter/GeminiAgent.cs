using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Documenter
{
    public class GeminiAgent // Keeping the name
    {
        // 1. POINT TO LOCALHOST (own computer)
        private const string OllamaUrl = "http://localhost:11434/api/generate";

        public static async Task<string> AnalyzeCode(HttpClient client, string fileName, string codeContent)
        {
            // Safety: DeepSeek handles large context well, but let's keep it safe.
            var safeCode = codeContent.Length > 15000 ? codeContent.Substring(0, 15000) : codeContent;

            var prompt = $@"
                You are an Expert Technical Writer. Analyze this code file: '{fileName}'.
                
                1. LANGUAGE: Identify the language.
                2. SUMMARY: Explain the business logic clearly.
                3. DIAGRAM: Write a Mermaid 'classDiagram' or 'flowchart TD'.
                   - RETURN ONLY THE MERMAID CODE inside ```mermaid blocks.

                ## File: {fileName}
                **Language**: [Language Name]
                **Summary**: [Your explanation]
                
                **Diagram**:
                ```mermaid
                [Mermaid Code Here]
                ```
                ---
                CODE:
                {safeCode}
            ";

            // 2. NEW JSON FORMAT (Ollama expects this structure)
            var requestBody = new
            {
                model = "deepseek-coder", // The model
                prompt = prompt,
                stream = false            // Get the whole response at once
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // 3. SEND TO LOCAL SERVER
                var response = await client.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode)
                    return $"Error: Local AI is not running. Open CMD and type 'ollama serve'.";

                string responseJson = await response.Content.ReadAsStringAsync();

                // 4. PARSE OLLAMA RESPONSE
                dynamic data = JsonConvert.DeserializeObject(responseJson);
                return data?.response ?? "No response.";
            }
            catch (Exception ex)
            {
                return $"Local AI Error: {ex.Message}. Make sure Ollama is installed!";
            }
        }
    }
}