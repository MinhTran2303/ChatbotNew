using Chatbot.Services.Core;
using Chatbot.Services.Rack;
using System.Text;
using System.Text.Json;

namespace Chatbot.Services.Modules.Rack
{
    public class RackApiService
    {
        private readonly ApiCaller _api;

        public RackApiService(ApiCaller api)
        {
            _api = api;
        }

        // =====================================================================
        // 1) BUILD BODY THEO FILTER
        // =====================================================================
        private object BuildBody(RackFilter f)
        {
            return new
            {
                factory = f.Factory,
                floor = f.Floor,
                room = f.Room,
                model = f.Model,
                nickName = "ALL",
                group = f.Group,
                dateRange = ""
            };
        }

        // =====================================================================
        // 2) CALL API CHUẨN
        // =====================================================================
        private async Task<JsonElement?> CallApiAsync(RackFilter filter)
        {
            var response = await _api.PostAsync(
                "https://10.220.130.117/newweb/api/nvidia/rack/Monitor/GetDataMonitoring",
                BuildBody(filter)
            );

            if (string.IsNullOrWhiteSpace(response))
                return null;

            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.Clone();
        }

        private string Stamp() =>
            $"\n\n⏱ Cập nhật: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";


        // =====================================================================
        // 3) TỔNG QUAN (Input – Pass – Fail – YR – FPR)
        // =====================================================================
        public async Task<string> RackGetSummaryAsync(RackFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null)
                return "Không lấy được dữ liệu Rack Monitoring.";

            if (!root.Value.TryGetProperty("QuantitySummary", out var summary))
                return "API không trả về QuantitySummary.";

            int input = summary.GetProperty("Input").GetInt32();
            int pass = summary.GetProperty("Pass").GetInt32();
            int repass = summary.GetProperty("Re_Pass").GetInt32();
            int totalPass = summary.GetProperty("Total_Pass").GetInt32();
            int fail = summary.GetProperty("Fail").GetInt32();
            double fpr = summary.GetProperty("FPR").GetDouble();
            double yr = summary.GetProperty("YR").GetDouble();
            int wip = summary.GetProperty("WIP").GetInt32();

            // MODEL PASS
            var modelPass = new StringBuilder();

            if (root.Value.TryGetProperty("ModelDetails", out var modelArr))
            {
                var arr = modelArr.EnumerateArray()
                    .Select(m => new
                    {
                        Name = m.GetProperty("ModelName").GetString(),
                        Qty = m.GetProperty("TotalPass").GetInt32()
                    })
                    .OrderByDescending(x => x.Qty)
                    .ToList();

                if (arr.Count > 0)
                {
                    modelPass.AppendLine("\n📦 **PASS theo model:**");
                    foreach (var m in arr)
                        modelPass.AppendLine($"• {m.Name}: **{m.Qty} pcs**");
                }
            }

            return
                $"📊 **Tổng quan rack ({filter.Factory} – {filter.Group} – {filter.Floor} – {filter.Room} – Model {filter.Model}):**\n" +
                $"• INPUT: **{input} pcs**\n" +
                $"• PASS: **{pass} pcs**\n" +
                $"• RE-PASS: **{repass} pcs**\n" +
                $"• TOTAL PASS: **{totalPass} pcs**\n" +
                $"• FAIL: **{fail} pcs**\n" +
                $"• FPR: **{fpr:0.##}%**\n" +
                $"• YR: **{yr:0.##}%**\n" +
                $"• WIP: **{wip} pcs**\n" +
                modelPass.ToString() +
                Stamp();
        }


        // =====================================================================
        // 4) TRẠNG THÁI TẤT CẢ RACK
        // =====================================================================
        public async Task<string> RackGetStatusAsync(RackFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu Rack Monitoring.";

            if (!root.Value.TryGetProperty("RackDetails", out var arr))
                return "API không trả về RackDetails.";

            var list = arr.EnumerateArray()
                .Select(r => new
                {
                    Name = r.GetProperty("RackName").GetString(),
                    Pass = r.GetProperty("Pass").GetInt32(),
                    UT = r.GetProperty("UT").GetDouble(),
                    Model = r.GetProperty("ModelName").GetString()
                })
                .OrderByDescending(x => x.UT)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"🧱 **Trạng thái Rack ({filter.Factory}/{filter.Floor}/{filter.Group}):**\n");

            foreach (var r in list)
                sb.AppendLine($"• {r.Name} – PASS **{r.Pass} pcs** – UT **{r.UT:0.0}%** – Model **{r.Model}**");

            sb.AppendLine(Stamp());
            return sb.ToString();
        }


        // =====================================================================
        // 5) CHI TIẾT 1 RACK
        // =====================================================================
        public async Task<string> RackGetDetailAsync(string rackNo, RackFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null)
                return "Không lấy được dữ liệu Rack Monitoring.";

            if (!root.Value.TryGetProperty("RackDetails", out var arr))
                return "API không trả về RackDetails.";

            // ==== Tìm đúng rack ====
            var rack = arr.EnumerateArray()
                .FirstOrDefault(r =>
                    (r.TryGetProperty("RackName", out var rn) ? rn.GetString() ?? "" : "")
                    .Replace("RACK ", "") == rackNo);

            if (rack.ValueKind == JsonValueKind.Undefined)
                return $"❌ Không tìm thấy rack {rackNo}.";

            // ==== Lấy rack-level fields an toàn ====
            string product = rack.TryGetProperty("ProductName", out var p1) ? p1.GetString() ?? "" : "";
            string model = rack.TryGetProperty("ModelName", out var p2) ? p2.GetString() ?? "" : "";
            int totalPass = rack.TryGetProperty("Total_Pass", out var p3) ? p3.GetInt32() : 0;
            double ut = rack.TryGetProperty("UT", out var p4) ? p4.GetDouble() : 0;
            double yr = rack.TryGetProperty("YR", out var p5) ? p5.GetDouble() : 0;

            // Nếu YR bị thiếu
            if (yr == 0 && rack.TryGetProperty("Input", out var inp) &&
                          rack.TryGetProperty("Pass", out var pass))
            {
                int i = inp.GetInt32();
                int p = pass.GetInt32();
                yr = i > 0 ? (double)p / i * 100 : 0;
            }

            var sb = new StringBuilder();

            // ===== HEADER =====
            sb.AppendLine($"📌 **RACK {rackNo}** `[ {product} ]`");
            sb.AppendLine($"📦 **{totalPass} PCS**");
            sb.AppendLine($"- UT: **{ut:0.00}%**");
            sb.AppendLine($"- Model: **{model}**");
            sb.AppendLine($"- Y.R: **{yr:0.##}%**");

            sb.AppendLine("\n🔧 **Slot Details:**");

            // ===== SLOT DETAILS =====
            if (rack.TryGetProperty("SlotDetails", out var slots) &&
                slots.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in slots.EnumerateArray())
                {
                    string slotNo = s.TryGetProperty("SlotNumber", out var s1) ? s1.GetString() ?? "" : "";
                    string slotName = s.TryGetProperty("SlotName", out var s2) ? s2.GetString() ?? "" : "";

                    int input = s.TryGetProperty("Input", out var i1) ? i1.GetInt32() : 0;
                    int totalP = s.TryGetProperty("Total_Pass", out var i2) ? i2.GetInt32() : 0;

                    string status = s.TryGetProperty("Status", out var st) ? st.GetString() ?? "" : "";

                    double yrr = input > 0 ? (double)totalP / input * 100 : 0;

                    // ===== STATUS ICON =====
                    string icon = status switch
                    {
                        "Pass" => "🟢",
                        "Fail" => "🔴",
                        "Testing" => "🔵",
                        "Waiting" => "🟠",
                        "Offline" => "⚪",
                        "Hold" => "🟣",
                        _ => "⚪"
                    };

                    sb.AppendLine(
                        $"• {icon} **Slot {slotNo}** – `{slotName}` – {totalP}/{input} – **{yrr:0.#}%** – ({status})"
                    );
                }
            }

            sb.AppendLine(Stamp());
            return sb.ToString();
        }




        // =====================================================================
        // 6) SLOT STATUS
        // =====================================================================
        public async Task<string> RackGetSlotStatusAsync(RackFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null)
                return "Không lấy được dữ liệu Rack Monitoring.";

            if (!root.Value.TryGetProperty("SlotStatic", out var arr))
                return "API không trả về SlotStatic.";

            int pass = 0, fail = 0, testing = 0, waiting = 0, offline = 0, hold = 0;

            foreach (var item in arr.EnumerateArray())
            {
                string st = item.GetProperty("Status").GetString() ?? "";
                int val = item.GetProperty("Value").GetInt32();

                switch (st)
                {
                    case "Pass": pass = val; break;
                    case "Fail": fail = val; break;
                    case "Testing": testing = val; break;
                    case "Waiting": waiting = val; break;
                    case "NotUsed": offline = val; break;
                    case "Offline": offline = val; break;
                    case "Hold": hold = val; break;
                }
            }

            int total = pass + fail + testing + waiting + offline + hold;

            return
                $"🎯 **Tình trạng Slot:**\n" +
                $"- 🟢 Pass: **{pass} slot**\n" +
                $"- 🔴 Fail: **{fail} slot**\n" +
                $"- 🔵 Testing: **{testing} slot**\n" +
                $"- 🟠 Waiting: **{waiting} slot**\n" +
                $"- ⚪ Offline: **{offline} slot**\n" +
                $"- 🟣 Hold: **{hold} slot**\n\n" +
                $"📦 Tổng: **{total} slot**\n" +
                Stamp();
        }


        // =====================================================================
        // 7) PASS BY MODEL (ModelDetails)
        // =====================================================================
        public async Task<string> RackGetPassByModelAsync(RackFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu Rack Monitoring.";

            if (!root.Value.TryGetProperty("ModelDetails", out var arr))
                return "API không trả về ModelDetails.";

            var sb = new StringBuilder();
            sb.AppendLine("📦 **PASS theo Model:**\n");

            foreach (var m in arr.EnumerateArray().OrderByDescending(x => x.GetProperty("TotalPass").GetInt32()))
                sb.AppendLine($"• **{m.GetProperty("ModelName").GetString()}** – {m.GetProperty("TotalPass").GetInt32()} pcs");

            sb.AppendLine(Stamp());
            return sb.ToString();
        }


        // =====================================================================
        // 8) RACK UT CAO NHẤT
        // =====================================================================
        public async Task<string> RackGetUTAsync(RackFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu Rack Monitoring.";

            if (!root.Value.TryGetProperty("RackDetails", out var arr))
                return "API không trả về RackDetails.";

            var top = arr.EnumerateArray()
                .Select(r => new
                {
                    Name = r.GetProperty("RackName").GetString(),
                    UT = r.GetProperty("UT").GetDouble()
                })
                .OrderByDescending(x => x.UT)
                .First();

            return
                $"⚡ **Rack có UT cao nhất:**\n" +
                $"- {top.Name}: **{top.UT:0.0}%**" +
                Stamp();
        }
    }
}
