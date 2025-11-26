using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Chatbot.Services.Core
{
    public class LlmClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;

        public LlmClient(IConfiguration config)
        {
            var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
            _model = config["Ollama:Model"] ?? "qwen2.5:7b";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public async Task<string> GenerateAsync(string prompt)
        {
            var requestBody = new
            {
                model = _model,
                prompt = prompt,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("/api/generate", requestBody);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            // Ollama API trả về { "response": "..."}
            if (doc.RootElement.TryGetProperty("response", out var resp))
                return resp.GetString() ?? "";

            return response.ToString();
        }
    }
}
