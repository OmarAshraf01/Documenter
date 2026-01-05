using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AutoDocGui
{
    public class GeminiAgent
    {
        // !!! PASTE YOUR GOOGLE API KEY INSIDE THE QUOTES BELOW !!!
        private const string ApiKey = "AIzaSyBqxYv88YPxb1KV6KmrQtlX08m7d9vy8us";

        private static readonly string ModelUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={ApiKey}";

        public static async Task<string> AnalyzeCode(HttpClient client, string fileName, string codeContent)
        {
            // Instructions for the AI to handle ANY language
            var prompt = $@"
                You are an Expert Technical Writer. Analyze this code file: '{fileName}'.
                
                1. LANGUAGE: Identify the language (Python, C#, Java, etc.).
                2. SUMMARY: Explain the business logic clearly.
                3. DIAGRAM: 
                   - If OOP (Java/C#), write a Mermaid 'classDiagram'.
                   - If Script (Python/JS), write a Mermaid 'flowchart TD'.
                   - Return ONLY the Mermaid syntax inside the code block.

                Respond in this Markdown format:
                ## File: {fileName}
                **Language**: [Language Name]
                **Summary**: [Your explanation]
                
                **Diagram**:
                ```mermaid
                [Mermaid Code Here]
                ```
                ---
                CODE PREVIEW:
                {codeContent.Substring(0, Math.Min(500, codeContent.Length))}...
            ";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(ModelUrl, content);
                if (!response.IsSuccessStatusCode) return $"Error: API returned {response.StatusCode}";

                string responseJson = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(responseJson);
                return data?.candidates?[0]?.content?.parts?[0]?.text ?? "No response generated.";
            }
            catch (Exception ex)
            {
                return $"AI Connection Error: {ex.Message}";
            }
        }
    }
}