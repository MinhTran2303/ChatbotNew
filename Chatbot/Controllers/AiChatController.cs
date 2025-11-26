using Microsoft.AspNetCore.Mvc;
using Chatbot.Models;
using Chatbot.Services.Core;
using Microsoft.Extensions.Logging;

namespace Chatbot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiChatController : ControllerBase
    {
        private readonly RouterService _router;
        private readonly ILogger<AiChatController> _logger;

        public AiChatController(RouterService router, ILogger<AiChatController> logger)
        {
            _router = router;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(body.Message))
                    return BadRequest(new { reply = "Tin nhắn rỗng. Hãy nhập câu hỏi của bạn." });

                // Gọi router xử lý ngữ nghĩa + điều hướng module
                string reply = await _router.HandleUserQueryAsync(body.Message);

                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi trong khi xử lý Chat");
                return StatusCode(500, new { reply = "Đã xảy ra lỗi khi xử lý yêu cầu của bạn." });
            }
        }
    }
}
