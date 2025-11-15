using System.Text.Json;
using Chatbot.Models;

namespace Chatbot.Services.Core
{
    public class RouterService
    {
        private readonly List<ModuleConfig> _modules;

        public RouterService()
        {
            var json = File.ReadAllText("Services/Config/modules.json");
            _modules = JsonSerializer.Deserialize<List<ModuleConfig>>(json) ?? new();
        }

        public ModuleConfig? DetectModule(string message)
        {
            message = message.ToLower();
            return _modules.FirstOrDefault(m => m.Keywords.Any(k => message.Contains(k)));
        }
    }
}
