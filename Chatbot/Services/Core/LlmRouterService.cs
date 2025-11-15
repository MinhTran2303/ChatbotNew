using System.Text;
using System.Text.Json;

namespace Chatbot.Services.Core
{
    public class LlmRouterService
    {
        private readonly HttpClient _http;

        public LlmRouterService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> GenerateAsync(string model, string prompt)
        {
            var body = new { model = model, prompt = prompt };

            var json = JsonSerializer.Serialize(body);
            var response = await _http.PostAsync("http://localhost:11434/api/generate",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var result = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(result);
            return doc.RootElement.GetProperty("response").GetString() ?? "Không có phản hồi.";
        }
    }
}
