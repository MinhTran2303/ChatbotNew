using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Chatbot.Services.Core
{
    public class ApiCaller
    {
        private readonly HttpClient _client;

        public ApiCaller(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient("DefaultClient");
        }

        public async Task<string?> PostAsync(string url, object body)
        {
            try
            {
                var json = JsonSerializer.Serialize(body);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiCaller] Error calling {url}: {ex.Message}");
                return null;
            }
        }
    }
}
