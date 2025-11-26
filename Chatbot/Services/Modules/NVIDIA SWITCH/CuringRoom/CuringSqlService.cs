using Chatbot.Services.Core;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

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


        // HÀM XỬ LÝ CÂU HỎI NGƯỜI DÙNG
        public async Task<string> HandleUserQueryAsync(string message)
        {
            message = message.ToLower().Trim();

            if (Regex.IsMatch(message, "nhiệt độ|temperature|cảm biến"))
                return await GetCuringTemperatureAsync();

            return "❓ Bạn có thể hỏi như:\n" +             
                   "- 'Nhiệt độ curing hiện tại là bao nhiêu?'";
        }


        // NHIỆT ĐỘ CẢM BIẾN TRẦN 

        public async Task<string> GetCuringTemperatureAsync()
        {
            try
            {
                using var conn = new SqlConnection(_config.GetConnectionString("CuringRoom"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("dbo.LatestSensorData", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Customer", "NVIDIA");
                cmd.Parameters.AddWithValue("@Factory", "F16");
                cmd.Parameters.AddWithValue("@Floor", "3F");
                cmd.Parameters.AddWithValue("@Area", "ROOM1");
                cmd.Parameters.AddWithValue("@Location", DBNull.Value);

                var table = new DataTable();
                using (var reader = await cmd.ExecuteReaderAsync()) table.Load(reader);

                if (table.Rows.Count == 0) return "Không có dữ liệu cảm biến nhiệt độ.";

                var sensors = table.AsEnumerable()
                    .Where(r => (r.Field<string>("SensorName") ?? "").StartsWith("PT"))
                    .OrderBy(r => r.Field<string>("SensorName"))
                    .Take(4)
                    .Select(r => new
                    {
                        Name = r.Field<string>("SensorName") ?? "Unknown",
                        Value = r["Value"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Value"]),
                        Timestamp = r["Timestamp"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["Timestamp"])
                    })
                    .ToList();

                if (!sensors.Any()) return "Không tìm thấy dữ liệu cảm biến PT.";

                var latestUpdate = sensors.Max(s => s.Timestamp);
                var temps = sensors.Select(s => $"{s.Name}: {s.Value:0.0}°C");

                return "🌡️ **Nhiệt độ 4 cảm biến trần:**\n" +
                       string.Join(" | ", temps) +
                       $"\n\n🕒 Cập nhật: {latestUpdate:HH:mm:ss dd/MM/yyyy}";
            }
            catch (Exception ex)
            {
                return $"❌ Lỗi khi đọc SQL Server: {ex.Message}";
            }
        }
    }
}
