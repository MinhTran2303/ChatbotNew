using Chatbot.Services.Core;
using Chatbot.Services.Modules.CuringRoom;
using Chatbot.Services.Modules.NVIDIA_SWITCH.CuringRoom;
using Chatbot.Services.Modules.Rack;
using Chatbot.Services.Rack;
using Chatbot.Services.Modules.Station;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;

namespace Chatbot.Services.Core
{
    public class RouterService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IntentDetector _intentDetector;
        private readonly IConfiguration _config;

        public RouterService(IServiceScopeFactory scopeFactory,
                             IntentDetector intentDetector,
                             IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _intentDetector = intentDetector;
            _config = config;
        }


        // ================================================================
        // 1) MODULE DEFINITIONS — MÔ TẢ CHUẨN
        // ================================================================
        private string GetModuleDescription()
        {
            return @"
Bạn là hệ thống ROUTER cho Smart Factory AIOT.

Nhiệm vụ:
1) Xác định MODULE phù hợp cho câu hỏi của người dùng.
2) Xác định INTENT bên trong module đó.
3) Trả về JSON:
{
  ""module"": ""TênModule"",
  ""intent"": ""TênIntent""
}

====================================================
📦 MODULE LIST
====================================================

### **MODULE: CuringRoom**
Mô tả: Phòng sấy – quản lý rack curing, nhiệt độ, trạng thái, cycle time.
Intent:
- GetDisplayedRack: danh sách rack trong phòng sấy
- GetRackStatus: rack nào đang chạy / đã hoàn thành
- GetTemperature: nhiệt độ – độ ẩm
- GetSummary: tổng quan curing room

Ví dụ câu hỏi:
- Rack curing nào đang chạy?
- Nhiệt độ phòng sấy hôm nay bao nhiêu?
- Cho tôi tổng quan curing room

====================================================

### **MODULE: RackMonitoring**
Mô tả: Dashboard RACK – JTAG/GB200/GB300/Slot.
Intent:
- RackSummary: tổng quan rack – số liệu rack ngày hôm nay
- RackStatus: trạng thái rack đang chạy
- RackDetail: chi tiết 1 rack (#1/#2)
- RackSlotStatus: slot WAITING/TESTING/PASS/FAIL
- RackPassByModel: model nào pass nhiều nhất
- RackUT: rack nào UT cao nhất

Ví dụ:
- Tình hình rack hôm nay?
- Rack 2 đang test model gì?
- Pass theo model hôm nay?

====================================================

### **MODULE: Station**
Mô tả: ICT / FT / CTO Station Dashboard (Switch/Adapter).
Intent:
- Station_Overview: tổng quan sản lượng – input/pass/fail
- Station_TopError: top lỗi
- Station_TrackingChart: input-pass-fail-repair theo ngày
- Station_YieldTrend: xu hướng yield (FPR/SPR/YR)

Ví dụ:
- Tổng quan station ICT của SWITCH hôm nay?
- Cho tôi top error ICT
- Yield trend FT trong tuần
- Tracking chart CTO

====================================================

Hãy chọn MODULE phù hợp nhất dựa vào mô tả, không chọn theo từ khóa đơn lẻ nếu không khớp ngữ cảnh.
            ";
        }


        // =====================================================================
        // 2) HANDLE USER QUERY
        // =====================================================================
        public async Task<string> HandleUserQueryAsync(string message)
        {
            var (module, intent) = await DetectModule(message);

            if (string.IsNullOrWhiteSpace(module))
                return "❓ Tôi không xác định được module phù hợp.";

            using var scope = _scopeFactory.CreateScope();
            string result = "";

            switch (module)
            {
                // ---------------------- CURING ----------------------
                case "CuringRoom":
                    var curingSql = scope.ServiceProvider.GetRequiredService<CuringSqlService>();
                    var curingApi = scope.ServiceProvider.GetRequiredService<CuringApiService>();
                    var curingFilter = CuringFilterParser.Parse(message);

                    result = intent switch
                    {
                        "GetDisplayedRack" => await curingApi.GetDisplayedRacksAsync(curingFilter),
                        "GetRackStatus" => await curingApi.GetRackStatusAsync(curingFilter),
                        "GetTemperature" => await curingSql.GetCuringTemperatureAsync(),
                        "GetSummary" => await curingApi.GetSummaryAsync(curingFilter),
                        _ => "⚙️ Intent curing chưa hỗ trợ."
                    };
                    break;

                // ---------------------- RACK ----------------------
                case "RackMonitoring":
                    var rackApi = scope.ServiceProvider.GetRequiredService<RackApiService>();
                    var filter = RackFilterParser.Parse(message);

                    result = intent switch
                    {
                        "RackSummary" => await rackApi.RackGetSummaryAsync(filter),
                        "RackStatus" => await rackApi.RackGetStatusAsync(filter),
                        "RackDetail" => await rackApi.RackGetDetailAsync(ExtractRackName(message), filter),
                        "RackSlotStatus" => await rackApi.RackGetSlotStatusAsync(filter),
                        "RackPassByModel" => await rackApi.RackGetPassByModelAsync(filter),
                        "RackUT" => await rackApi.RackGetUTAsync(filter),
                        _ => "⚙️ Intent rack chưa hỗ trợ."
                    };
                    break;

                // ---------------------- STATION ----------------------
                case "Station":
                    var stApi = scope.ServiceProvider.GetRequiredService<StationApiService>();
                    var stFilter = StationFilterParser.Parse(message);

                    result = intent switch
                    {
                        "Station_Overview" => await stApi.StationOverviewAsync(stFilter),
                        "Station_TopError" => await stApi.StationTopErrorAsync(stFilter),
                        "Station_TrackingChart" => await stApi.StationTrackingChartAsync(stFilter),
                        "Station_YieldTrend" => await stApi.StationYieldTrendAsync(stFilter),
                        _ => "⚙️ Intent station chưa hỗ trợ."
                    };
                    break;

                default:
                    result = $"⚠ Module '{module}' chưa được định nghĩa.";
                    break;
            }

            return result;
        }


        // =====================================================================
        // 3) DETECT MODULE / INTENT (Module Description Routing)
        // =====================================================================
        public async Task<(string Module, string Intent)> DetectModule(string message)
        {
            string prompt = GetModuleDescription() + $@"

===============================
CÂU HỎI NGƯỜI DÙNG:
{message}
===============================

Yêu cầu:
- Chọn module khớp nhất theo đúng mô tả.
- Chọn intent đúng nhất bên trong module.
- Trả về JSON:
{{ ""module"": ""TênModule"", ""intent"": ""TênIntent"" }}
";

            string res = await _intentDetector.ClassifyAsync(prompt);

            try
            {
                using var doc = JsonDocument.Parse(res);
                string module = doc.RootElement.GetProperty("module").GetString() ?? "";
                string intent = doc.RootElement.GetProperty("intent").GetString() ?? "";
                return (module, intent);
            }
            catch
            {
                return ("", "");
            }
        }


        // =====================================================================
        // 4) EXTRACT RACK NAME
        // =====================================================================
        private string ExtractRackName(string message)
        {
            message = message.ToLower();

            var m1 = Regex.Match(message, @"rack\s*(\d+)");
            if (m1.Success) return m1.Groups[1].Value;

            var m2 = Regex.Match(message, @"rack\s*số\s*(\d+)");
            if (m2.Success) return m2.Groups[1].Value;

            var m3 = Regex.Match(message, @"#(\d+)");
            if (m3.Success) return m3.Groups[1].Value;

            return "";
        }
    }
}
