using System.Text.Json;

namespace Chatbot.Services.Core
{
    public class IntentDetector
    {
        private readonly LlmClient _llm;

        public IntentDetector(LlmClient llm)
        {
            _llm = llm;
        }

        // Detect intent in module
        public async Task<(string module, string intent, string rawJson)> DetectAsync(string prompt)
        {
            string raw = await _llm.GenerateAsync(prompt);

            int s = raw.IndexOf('{');
            int e = raw.LastIndexOf('}');
            if (s >= 0 && e > s)
                raw = raw.Substring(s, e - s + 1);

            using var doc = JsonDocument.Parse(raw);
            string module = doc.RootElement.GetProperty("module").GetString() ?? "";
            string intent = doc.RootElement.GetProperty("intent").GetString() ?? "";

            return (module, intent, raw);
        }

    }
}
