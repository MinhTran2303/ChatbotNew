using Chatbot.Services.Core;
using System.Text.Json;
using static Chatbot.Services.Modules.NVIDIA_SWITCH.CuringRoom.CuringFilterParser;

namespace Chatbot.Services.Modules.CuringRoom
{
    public class CuringApiService
    {
        private readonly ApiCaller _api;

        public CuringApiService(ApiCaller api)
        {
            _api = api;
        }

        // --------------------------------------------------------
        // CALL API CHUNG (DÙNG FILTER F16 / F17 / ROOM / FLOOR)
        // --------------------------------------------------------
        private async Task<JsonElement?> CallApiAsync(CuringFilter f)
        {
            var response = await _api.PostAsync(
                "https://10.220.130.117/newweb/api/nvidia/dashboard/CuringMonitor/GetCuringData",
                new
                {
                    modelSerial = f.ModelSerial,   // SWITCH
                    customer = f.Customer,         // NVIDIA
                    factory = f.Factory,           // F16 / F17
                    floor = f.Floor,               // 3F
                    area = f.Area,                 // ROOM1
                    tray = "string"
                }
            );

            if (string.IsNullOrWhiteSpace(response))
                return null;

            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.Clone();
        }

        private string Stamp() =>
            $"\n\n🕒 Cập nhật: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";


        // --------------------------------------------------------
        // SUMMARY
        // --------------------------------------------------------
        public async Task<string> GetSummaryAsync(CuringFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu API.";

            int wip = root.Value.GetProperty("wip").GetInt32();
            int pass = root.Value.GetProperty("pass").GetInt32();

            var passDetails = root.Value.GetProperty("passDetails")
                .EnumerateArray()
                .Select(x => new {
                    Model = x.GetProperty("modelName").GetString(),
                    Qty = x.GetProperty("qty").GetInt32()
                })
                .OrderByDescending(x => x.Qty)
                .ToList();

            string modelPassText = string.Join("\n",
                passDetails.Select(x => $"- **{x.Model}**: {x.Qty} pcs")
            );

            return
                $"📦 **Tổng quan Curing:**\n" +
                $"• **WIP:** {wip}\n" +
                $"• **PASS:** {pass}\n\n" +
                $"📊 **PASS theo Model:**\n" +
                $"{modelPassText}" +
                Stamp();
        }


        // --------------------------------------------------------
        // DISPLAYED RACKS (curing-rack, tray)
        // --------------------------------------------------------
        public async Task<string> GetDisplayedRacksAsync(CuringFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu API.";

            if (!root.Value.TryGetProperty("rackDetails", out var arr) || arr.GetArrayLength() == 0)
                return "📭 Không có tray nào hiển thị trong phòng curing." + Stamp();

            var racks = arr.EnumerateArray()
                .Select(r => new {
                    Name = r.GetProperty("name").GetString(),
                    Time = r.GetProperty("time").GetString(),
                    Model = r.GetProperty("modelName").GetString(),
                    Qty = r.GetProperty("number").GetInt32(),
                    Status = r.GetProperty("status").GetString(),
                    Percent = r.GetProperty("percent").GetDouble()
                })
                .OrderByDescending(x => x.Percent)
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("🧱 **Curing Rack đang hiển thị:**\n");

            foreach (var r in racks)
            {
                string icon = r.Status == "running" ? "🟡" : "🟢";
                sb.AppendLine($"{icon} **{r.Name}** – {r.Model} – {r.Qty} pcs – ⏱ {r.Time} – {r.Percent:0.0}%");
            }

            sb.AppendLine(Stamp());
            return sb.ToString();
        }


        // --------------------------------------------------------
        // RACK STATUS (RUNNING / FINISHED)
        // --------------------------------------------------------
        public async Task<string> GetRackStatusAsync(CuringFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu API.";

            if (!root.Value.TryGetProperty("rackDetails", out var arr) || arr.GetArrayLength() == 0)
                return "Không có curing rack nào." + Stamp();

            var running = new List<string>();
            var finished = new List<string>();

            foreach (var r in arr.EnumerateArray())
            {
                string name = r.GetProperty("name").GetString()!;
                string status = r.GetProperty("status").GetString()!;

                if (status.Equals("running", StringComparison.OrdinalIgnoreCase))
                    running.Add(name);
                else
                    finished.Add(name);
            }

            return
                $"🟡 **Đang chạy:** {string.Join(", ", running)}\n" +
                $"🟢 **Hoàn thành:** {string.Join(", ", finished)}" +
                Stamp();
        }


        // --------------------------------------------------------
        // MODULE HANDLER
        // --------------------------------------------------------
        public async Task<string> HandleIntentAsync(string intent, CuringFilter filter)
        {
            return intent switch
            {
                "GetSummary" => await GetSummaryAsync(filter),
                "GetDisplayedRack" => await GetDisplayedRacksAsync(filter),
                "GetRackStatus" => await GetRackStatusAsync(filter),
                _ => "❓ Tôi chưa hiểu bạn muốn hỏi gì trong module Curing Room."
            };
        }
    }
}
