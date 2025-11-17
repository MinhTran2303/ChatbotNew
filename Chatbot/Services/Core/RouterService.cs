using System.Text.Json;
using Chatbot.Models;
using Chatbot.Services.Modules.CuringRoom;
using Microsoft.Extensions.DependencyInjection;

namespace Chatbot.Services.Core
{
    public class RouterService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly List<ModuleConfig> _modules;

        public RouterService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Services/Config/modules.json");
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Không tìm thấy file cấu hình module: {jsonPath}");

            var json = File.ReadAllText(jsonPath);
            _modules = JsonSerializer.Deserialize<List<ModuleConfig>>(json) ?? new();
        }

        public (string Name, string Database, object? Service)? DetectModule(string message)
        {
            message = message.ToLower();
            var module = _modules.FirstOrDefault(m => m.Keywords.Any(k => message.Contains(k)));

            if (module == null)
                return null;

            using var scope = _scopeFactory.CreateScope(); 
            object? service = module.ServiceType switch
            {
                "sql" => scope.ServiceProvider.GetService(typeof(CuringSqlService)),
                "api" => scope.ServiceProvider.GetService(typeof(CuringApiService)),
                _ => null
            };

            return (module.Name, module.Database, service);
        }
    }
}
