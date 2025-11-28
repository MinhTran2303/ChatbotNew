using Chatbot.Services.Core;
using System.Text.Json;
using static Chatbot.Services.Modules.Station.StationFilterParser;

namespace Chatbot.Services.Modules.Station
{
    public class StationApiService
    {
        private readonly ApiCaller _api;

        public StationApiService(ApiCaller api)
        {
            _api = api;
        }

        private async Task<JsonElement?> CallApiAsync(StationFilter filter)
        {
            string url =
                $"https://10.220.130.117/newweb/api/nvidia/station/Dashboard/GetDashboardTrakingData" +
                $"?ModelSerial={filter.ModelSerial}&GroupName={filter.GroupName}";

            var response = await _api.GetAsync(url);
            if (string.IsNullOrEmpty(response)) return null;

            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.Clone();
        }

        private string Stamp() =>
            $"\n\n🕒 Cập nhật: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";

        // ================================
        // OVERVIEW
        // ================================
        public async Task<string> StationOverviewAsync(StationFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu Station Dashboard.";

            var arr = root.Value.GetProperty("rateDatas").EnumerateArray().ToList();
            if (!arr.Any()) return "❗Không có dữ liệu.";

            JsonElement data;

            if (filter.Date != null)
            {
                data = arr.FirstOrDefault(x =>
                    x.GetProperty("worK_DATE").GetString() == filter.Date);

                if (data.ValueKind == JsonValueKind.Undefined)
                    return $"❗Không có dữ liệu ngày **{filter.Date}**.";
            }
            else
            {
                data = arr.Last();
            }

            return
                $"📦 **Tổng quan Station – {filter.ModelSerial}/{filter.GroupName}**\n" +
                $"📅 Ngày: {data.GetProperty("worK_DATE").GetString()}\n\n" +
                $"• Input: {data.GetProperty("input").GetInt32()}\n" +
                $"• Pass: {data.GetProperty("pass").GetInt32()}\n" +
                $"• First Fail: {data.GetProperty("firsT_FAIL").GetInt32()}\n" +
                $"• Repair: {data.GetProperty("repaiR_QTY").GetInt32()}\n" +
                $"• FPR: {data.GetProperty("fpr").GetDouble():0.00}%\n" +
                $"• SPR: {data.GetProperty("spr").GetDouble():0.00}%\n" +
                $"• YR: {data.GetProperty("yr").GetDouble():0.00}%\n" +
                $"• RR: {data.GetProperty("rr").GetDouble():0.00}%\n" +
                Stamp();
        }

        // ================================
        // TOP ERROR
        // ================================
        public async Task<string> StationTopErrorAsync(StationFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu Station Dashboard.";

            var errors = root.Value.GetProperty("errorCodeDatas")
                .EnumerateArray()
                .OrderByDescending(e => e.GetProperty("totaL_FAIL").GetInt32())
                .Take(10)
                .ToList();

            if (!errors.Any()) return "❗Không có error.";

            string list = string.Join("\n",
                errors.Select(x =>
                    $"• **{x.GetProperty("erroR_CODE").GetString()}**: {x.GetProperty("totaL_FAIL").GetInt32()} pcs"
                ));

            return
                $"⚠️ **TOP 10 ERROR – {filter.ModelSerial}/{filter.GroupName}**\n\n" +
                $"{list}" +
                Stamp();
        }

        // ================================
        // TRACKING CHART
        // ================================
        public async Task<string> StationTrackingChartAsync(StationFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu Station Dashboard.";

            var arr = root.Value.GetProperty("rateDatas").EnumerateArray().ToList();
            if (!arr.Any()) return "❗Không có dữ liệu.";

            IEnumerable<JsonElement> selected =
                (filter.Date != null)
                    ? arr.Where(x => x.GetProperty("worK_DATE").GetString() == filter.Date)
                    : arr;

            if (!selected.Any())
                return $"❗Không có dữ liệu ngày **{filter.Date}**.";

            string output = string.Join("\n",
                selected.Select(x =>
                    $"📅 **{x.GetProperty("worK_DATE").GetString()}**\n" +
                    $"• Input: {x.GetProperty("input").GetInt32()}\n" +
                    $"• Pass: {x.GetProperty("pass").GetInt32()}\n" +
                    $"• First Fail: {x.GetProperty("firsT_FAIL").GetInt32()}\n" +
                    $"• Repair: {x.GetProperty("repaiR_QTY").GetInt32()}\n"
                ));

            return
                $"📊 **Tracking – {filter.ModelSerial}/{filter.GroupName}**\n\n" +
                output +
                Stamp();
        }

        // ================================
        // YIELD TREND
        // ================================
        public async Task<string> StationYieldTrendAsync(StationFilter filter)
        {
            var root = await CallApiAsync(filter);
            if (root == null) return "Không lấy được dữ liệu Station Dashboard.";

            var arr = root.Value.GetProperty("rateDatas").EnumerateArray().ToList();
            if (!arr.Any()) return "❗Không có dữ liệu.";

            IEnumerable<JsonElement> selected =
                (filter.Date != null)
                    ? arr.Where(x => x.GetProperty("worK_DATE").GetString() == filter.Date)
                    : arr;

            if (!selected.Any())
                return $"❗Không có dữ liệu ngày **{filter.Date}**.";

            string output = string.Join("\n",
                selected.Select(x =>
                    $"📅 **{x.GetProperty("worK_DATE").GetString()}**\n" +
                    $"• FPR: {x.GetProperty("fpr").GetDouble():0.00}%\n" +
                    $"• SPR: {x.GetProperty("spr").GetDouble():0.00}%\n" +
                    $"• YR:  {x.GetProperty("yr").GetDouble():0.00}%\n"
                ));

            return
                $"📈 **Yield Trend – {filter.ModelSerial}/{filter.GroupName}**\n\n" +
                output +
                Stamp();
        }
    }
}
