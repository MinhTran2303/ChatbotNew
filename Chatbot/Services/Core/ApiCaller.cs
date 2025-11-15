using System.Net.Http.Json;

namespace Chatbot.Services.Core
{
    public class ApiCaller
    {
        private readonly HttpClient _http;

        public ApiCaller(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> PostAsync(string url, object body)
        {
            var response = await _http.PostAsJsonAsync(url, body);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
