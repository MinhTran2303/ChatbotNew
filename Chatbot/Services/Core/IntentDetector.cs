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

        /// <summary>
        /// Gửi prompt đến Ollama để phân loại ngữ nghĩa
        /// </summary>
        public async Task<string> ClassifyAsync(string prompt)
        {
            try
            {
                string raw = await _llm.GenerateAsync(prompt);

                // Nếu model trả lời không đúng JSON, ta cố gắng trích JSON
                int jsonStart = raw.IndexOf('{');
                int jsonEnd = raw.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                    return raw.Substring(jsonStart, jsonEnd - jsonStart + 1);

                return raw;
            }
            catch (Exception ex)
            {
                return $"{{\"module\":\"\",\"intent\":\"\",\"error\":\"{ex.Message}\"}}";
            }
        }
    }
}
