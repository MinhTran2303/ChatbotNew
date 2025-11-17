using Microsoft.AspNetCore.Mvc;
using Chatbot.Models;
using Chatbot.Services.Core;
using Chatbot.Services.Modules.CuringRoom;
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
        private readonly ILogger<AiChatController> _logger;

        public AiChatController(
            RouterService router,
            SqlExecutor sql,
            ApiCaller api,
            LlmRouterService llm,
            ILogger<AiChatController> logger)
        {
            _router = router;
            _sql = sql;
            _api = api;
            _llm = llm;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(body.Message))
                    return BadRequest(new { reply = "Tin nhắn rỗng. Hãy nhập câu hỏi của bạn." });

                var moduleInfo = _router.DetectModule(body.Message);
                if (moduleInfo == null)
                    return Ok(new { reply = "Xin lỗi, tôi chưa hiểu câu hỏi thuộc khu vực nào." });

                var (Name, Database, Service) = moduleInfo.Value;

                _logger.LogInformation($"[Router] User query: '{body.Message}' → Module: {Name}, DB: {Database}");

                string replyText = string.Empty;

                // Nếu là module Curing (SQL)
                if (Service is CuringSqlService curingSql)
                {
                    replyText = await curingSql.HandleUserQueryAsync(body.Message);
                }
                // Nếu là module Curing (API)
                else if (Service is CuringApiService curingApi)
                {
                    replyText = await curingApi.GetSummaryAsync();
                }
                else
                {
                    // fallback test: query ví dụ
                    var data = await _sql.ExecuteQueryAsync(Database, "SELECT TOP 3 * FROM Tray");
                    var json = JsonSerializer.Serialize(data);
                    var prompt = $"Ngữ cảnh: {Name}\nDữ liệu: {json}\nCâu hỏi: {body.Message}";
                    replyText = await _llm.GenerateAsync("qwen2.5:7b", prompt);
                }

                return Ok(new { reply = replyText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi trong khi xử lý Chat");
                return StatusCode(500, new { reply = "Đã xảy ra lỗi khi xử lý yêu cầu của bạn." });
            }
        }
    }
}
