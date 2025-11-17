using Chatbot.Services.Core;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace Chatbot.Services.Modules.CuringRoom
{
    public class CuringSqlService
    {
        private readonly SqlExecutor _sql;
        private readonly IConfiguration _config;

        public CuringSqlService(SqlExecutor sql, IConfiguration config)
        {
            _sql = sql;
            _config = config;
        }

        // 🧠 ROUTER — Xử lý câu hỏi người dùng
        public async Task<string> HandleUserQueryAsync(string message)
        {
            message = message.ToLower().Trim();

            if (Regex.IsMatch(message, "rack|tray|đang chạy|running"))
                return await GetRackStatusAsync();

            if (Regex.IsMatch(message, "nhiệt độ|temperature|cảm biến"))
                return await GetCuringTemperatureAsync();

            return "❓ Bạn có thể hỏi như:\n" +
                   "- 'Những rack nào đang chạy trong curing?'\n" +
                   "- 'Nhiệt độ curing hiện tại là bao nhiêu?'\n" +
                   "- 'Rack nào đã hoàn thành curing?'";
        }

        // =====================================================================
        // 1️⃣ LẤY DANH SÁCH RACK ĐANG CHẠY / ĐÃ HOÀN THÀNH (Oracle)
        // =====================================================================
        private async Task<string> GetRackStatusAsync()
        {
            try
            {
                using var conn = new OracleConnection(_config.GetConnectionString("SFIS_NVIDIA"));
                await conn.OpenAsync();

                // Lấy toàn bộ CURING_IN + CURING_OUT trong ngày
                string sql = @"
                    SELECT 
                        TRAY_NO,
                        MODEL_NAME,
                        COUNT(SERIAL_NUMBER) AS QTY,
                        MIN(IN_STATION_TIME) AS START_TIME,
                        MAX(WIP_GROUP) AS LAST_STATUS
                    FROM SFISM4.R_WIP_TRACKING_T
                    WHERE WIP_GROUP IN ('CURING_IN','CURING_OUT')
                    GROUP BY TRAY_NO, MODEL_NAME
                    ORDER BY TRAY_NO";

                using var cmd = new OracleCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var racks = new List<(string Tray, string Model, int Qty, DateTime Start, string Status)>();
                while (await reader.ReadAsync())
                {
                    string tray = reader["TRAY_NO"]?.ToString() ?? "N/A";
                    string model = reader["MODEL_NAME"]?.ToString() ?? "N/A";
                    int qty = Convert.ToInt32(reader["QTY"]);
                    DateTime start = Convert.ToDateTime(reader["START_TIME"]);
                    string status = reader["LAST_STATUS"]?.ToString() ?? "UNKNOWN";

                    racks.Add((tray, model, qty, start, status));
                }

                if (!racks.Any())
                    return "Hiện tại không có rack nào đang hoạt động trong curing.";

                var sb = new StringBuilder();

                // 🟡 Rack đang chạy
                var running = racks.Where(r => r.Status == "CURING_IN").ToList();
                if (running.Any())
                {
                    sb.AppendLine("🟡 **Các rack đang chạy trong curing:**");
                    foreach (var r in running)
                    {
                        var (time, statusText, percent) = CalcCuringTime(r.Start);
                        sb.AppendLine($"• {r.Tray} ({r.Model}) - {r.Qty} pcs ⏱ {time} ({percent:0.0}% hoàn thành)");
                    }
                    sb.AppendLine();
                }

                // ✅ Rack đã hoàn thành
                var finished = racks.Where(r => r.Status == "CURING_OUT").ToList();
                if (finished.Any())
                {
                    sb.AppendLine("✅ **Các rack đã hoàn thành curing:**");
                    foreach (var r in finished)
                    {
                        var (time, statusText, percent) = CalcCuringTime(r.Start, true);
                        sb.AppendLine($"• {r.Tray} ({r.Model}) - {r.Qty} pcs ✅ {time}");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Lỗi khi đọc Oracle: {ex.Message}";
            }
        }

        // =====================================================================
        // 2️⃣ LẤY NHIỆT ĐỘ CẢM BIẾN TRẦN (SQL SERVER)
        // =====================================================================
        private async Task<string> GetCuringTemperatureAsync()
        {
            try
            {
                string sql = @"
                    WITH LatestData AS (
                        SELECT 
                            s.Name AS SensorName,
                            sd.Value,
                            sd.Timestamp,
                            ROW_NUMBER() OVER (PARTITION BY s.Name ORDER BY sd.Timestamp DESC) AS rn
                        FROM SensorData sd
                        JOIN Sensor s ON sd.IdSensor = s.Id
                        WHERE s.Name LIKE 'PT%'
                    )
                    SELECT TOP 4 SensorName, Value, Timestamp
                    FROM LatestData
                    WHERE rn = 1
                    ORDER BY SensorName;";

                var rows = await _sql.ExecuteQueryAsync("CuringRoom", sql);

                if (rows == null || !rows.Any())
                    return "Không có dữ liệu nhiệt độ cảm biến.";

                var sb = new StringBuilder("🌡️ **Nhiệt độ 4 cảm biến trần mới nhất:**\n");

                foreach (var row in rows)
                {
                    var dict = (IDictionary<string, object>)row;
                    string name = dict["SensorName"]?.ToString() ?? "Unknown";
                    string val = dict["Value"]?.ToString() ?? "N/A";
                    sb.AppendLine($"• {name}: {val} °C");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Lỗi khi đọc SQL Server: {ex.Message}";
            }
        }

        // =====================================================================
        // 3️⃣ HÀM TÍNH THỜI GIAN VÀ PHẦN TRĂM CURING (chuẩn y web)
        // =====================================================================
        private (string Duration, string Status, double Percent) CalcCuringTime(DateTime startTime, bool isFinished = false)
        {
            const double curingDurationHours = 8.0; // Chu kỳ chuẩn 8 tiếng
            DateTime now = DateTime.Now;

            TimeSpan elapsed = isFinished ? TimeSpan.FromHours(curingDurationHours) : (now - startTime);
            double hours = elapsed.TotalHours;
            double percent = Math.Min(100, (hours / curingDurationHours) * 100);

            string durationText = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}";
            string status = isFinished ? "FINISHED" : "RUNNING";

            return (durationText, status, percent);
        }
    }
}
