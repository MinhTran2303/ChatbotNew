using Chatbot.Services.Modules.CuringRoom;
using Chatbot.Services.Modules.NVIDIA_SWITCH.CuringRoom;
using Chatbot.Services.Modules.Rack;
using Chatbot.Services.Modules.Station;
using Chatbot.Services.Rack;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace Chatbot.Services.Core
{
    public class RouterService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IntentDetector _intentDetector;
        private readonly ModulePromptLoader _loader;

        public RouterService(
            IServiceScopeFactory scopeFactory,
            IntentDetector intentDetector)
        {
            _scopeFactory = scopeFactory;
            _intentDetector = intentDetector;
            _loader = new ModulePromptLoader();
        }

        // ===============================================
        // MAIN ENTRY
        // ===============================================
        public async Task<string> HandleUserQueryAsync(string message)
        {
            message = Normalize(message);

            // ⚠️ Không detect module bằng keyword nữa
            // Ta build 1 prompt global để LLM chọn module + intent
            string globalPrompt = _loader.BuildIntentPromptALL(message);

            // LLM trả về cả module + intent
            var (module, intent, rawJson) = await _intentDetector.DetectAsync(globalPrompt);

            if (string.IsNullOrWhiteSpace(module))
                return $"⚠ Không xác định được module: {rawJson}";

            if (string.IsNullOrWhiteSpace(intent))
                return $"⚠ Không xác định được intent: {rawJson}";

            // Lấy module config
            var moduleCfg = _loader.GetModule(module);
            if (moduleCfg == null)
                return $"⚠ Module '{module}' không tồn tại trong cấu hình.";

            using var scope = _scopeFactory.CreateScope();

            // Điều hướng đến đúng module
            return module switch
            {
                "CuringRoom" => await HandleCuring(message, intent, scope),
                "RackMonitoring" => await HandleRack(message, intent, scope),
                "Station" => await HandleStation(message, intent, scope),
                _ => $"⚠ Module '{module}' chưa hỗ trợ."
            };
        }


        // ===============================================
        // MODULE HANDLERS
        // ===============================================
        private async Task<string> HandleCuring(string msg, string intent, IServiceScope scope)
        {
            var api = scope.ServiceProvider.GetRequiredService<CuringApiService>();
            var sql = scope.ServiceProvider.GetRequiredService<CuringSqlService>();
            var filter = CuringFilterParser.Parse(msg);

            return intent switch
            {
                "GetSummary" => await api.GetSummaryAsync(filter),
                "GetDisplayedRack" => await api.GetDisplayedRacksAsync(filter),
                "GetRackStatus" => await api.GetRackStatusAsync(filter),
                "GetTemperature" => await sql.GetCuringTemperatureAsync(),

                _ => $"⚙ Intent '{intent}' chưa hỗ trợ trong module CuringRoom."
            };
        }


        private async Task<string> HandleRack(string msg, string intent, IServiceScope scope)
        {
            var api = scope.ServiceProvider.GetRequiredService<RackApiService>();
            var filter = RackFilterParser.Parse(msg);
            var rackNo = ExtractRackNumber(msg);

            return intent switch
            {
                "RackSummary" => await api.RackGetSummaryAsync(filter),
                "RackStatus" => await api.RackGetStatusAsync(filter),
                "RackDetail" => await api.RackGetDetailAsync(rackNo, filter),
                "RackSlotStatus" => await api.RackGetSlotStatusAsync(filter),
                "RackPassByModel" => await api.RackGetPassByModelAsync(filter),
                "RackUT" => await api.RackGetUTAsync(filter),

                _ => $"⚙ Intent '{intent}' chưa hỗ trợ trong module RackMonitoring."
            };
        }


        private async Task<string> HandleStation(string msg, string intent, IServiceScope scope)
        {
            var api = scope.ServiceProvider.GetRequiredService<StationApiService>();
            var filter = StationFilterParser.Parse(msg);

            return intent switch
            {
                "Station_Overview" => await api.StationOverviewAsync(filter),
                "Station_TopError" => await api.StationTopErrorAsync(filter),
                "Station_TrackingChart" => await api.StationTrackingChartAsync(filter),
                "Station_YieldTrend" => await api.StationYieldTrendAsync(filter),

                _ => $"⚙ Intent '{intent}' chưa hỗ trợ trong module Station."
            };
        }


        // ===============================================
        // UTILS
        // ===============================================
        private string Normalize(string text)
        {
            text = Regex.Replace(text.ToLower(), @"\s+", " ").Trim();
            return text.Replace("\u00A0", " ");
        }

        private string ExtractRackNumber(string msg)
        {
            msg = msg.ToLower();

            var m1 = Regex.Match(msg, @"rack\s*(\d+)");
            if (m1.Success) return m1.Groups[1].Value;

            var m2 = Regex.Match(msg, @"rack\s*số\s*(\d+)");
            if (m2.Success) return m2.Groups[1].Value;

            var m3 = Regex.Match(msg, @"#(\d+)");
            if (m3.Success) return m3.Groups[1].Value;

            return "";
        }
    }
}
