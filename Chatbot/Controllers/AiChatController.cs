using Microsoft.AspNetCore.Mvc;
using Chatbot.Models;
using Chatbot.Services.Core;
using System.Text.Json;

namespace Chatbot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiChatController : ControllerBase
    {
        private readonly RouterService _router;
        private readonly SqlExecutor _sql;
        private readonly ApiCaller _api;
        private readonly LlmRouterService _llm;

        public AiChatController(RouterService router, SqlExecutor sql, ApiCaller api, LlmRouterService llm)
        {
            _router = router;
            _sql = sql;
            _api = api;
            _llm = llm;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest body)
        {
            var module = _router.DetectModule(body.Message);
            if (module == null)
                return Ok(new { reply = "Xin lỗi, tôi chưa hiểu câu hỏi thuộc khu vực nào." });

            string data = "";
            if (!string.IsNullOrEmpty(module.Database))
            {
                var result = await _sql.ExecuteQueryAsync(module.Database, "SELECT TOP 3 * FROM ExampleTable");
                data = JsonSerializer.Serialize(result);
            }

            string prompt = $"Ngữ cảnh: {module.Name}\nDữ liệu: {data}\nCâu hỏi: {body.Message}";
            var reply = await _llm.GenerateAsync("qwen2.5:7b", prompt);

            return Ok(new { reply });
        }
    }
}
