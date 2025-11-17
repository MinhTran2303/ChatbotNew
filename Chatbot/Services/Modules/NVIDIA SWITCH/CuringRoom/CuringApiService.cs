using Chatbot.Services.Core;
using System.Text.Json;

namespace Chatbot.Services.Modules.CuringRoom
{
    public class CuringApiService
    {
        private readonly ApiCaller _api;

        public CuringApiService(ApiCaller api)
        {
            _api = api;
        }

     
        /// Hàm tổng quan nhanh cho controller gọi trực tiếp.
      
        public async Task<string> GetSummaryAsync()
        {
            try
            {
                var response = await _api.PostAsync(
                    "https://10.220.130.117/newweb/api/nvidia/dashboard/CuringMonitor/GetCuringData",
                    new
                    {
                        modelSerial = "SWITCH",
                        customer = "NVIDIA",
                        factory = "F16",
                        floor = "3F",
                        area = "string",
                        tray = "string"
                    }
                );

                if (string.IsNullOrEmpty(response))
                    return "Không thể lấy dữ liệu từ hệ thống Curing Monitor.";

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                int wip = root.GetProperty("wip").GetInt32();
                int pass = root.GetProperty("pass").GetInt32();
                var passDetails = root.GetProperty("passDetails").EnumerateArray()
                    .Select(p => new
                    {
                        Model = p.GetProperty("modelName").GetString(),
                        Qty = p.GetProperty("qty").GetInt32()
                    }).ToList();

                return $"Phòng Curing hiện có {wip} sản phẩm đang xử lý (WIP) và {pass} sản phẩm đã PASS. " +
                       $"Chi tiết theo model: {string.Join(", ", passDetails.Select(d => $"{d.Model}: {d.Qty} pcs"))}.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CuringApiService] Lỗi GetSummaryAsync: {ex.Message}");
                return "Lỗi khi lấy dữ liệu tổng quan Curing.";
            }
        }

      
        /// Hàm xử lý linh hoạt theo câu hỏi (tổng quan, WIP, PASS, model cao nhất, v.v.)
     
        public async Task<string> HandleUserQueryAsync(string message)
        {
            message = message.ToLower();

            try
            {
                var response = await _api.PostAsync(
                    "https://10.220.130.117/newweb/api/nvidia/dashboard/CuringMonitor/GetCuringData",
                    new
                    {
                        modelSerial = "SWITCH",
                        customer = "NVIDIA",
                        factory = "F16",
                        floor = "3F",
                        area = "string",
                        tray = "string"
                    }
                );

                if (string.IsNullOrEmpty(response))
                    return "Không thể lấy dữ liệu từ hệ thống Curing Monitor.";

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                int wip = root.GetProperty("wip").GetInt32();
                int pass = root.GetProperty("pass").GetInt32();
                var passDetails = root.GetProperty("passDetails").EnumerateArray()
                    .Select(p => new
                    {
                        Model = p.GetProperty("modelName").GetString(),
                        Qty = p.GetProperty("qty").GetInt32()
                    }).ToList();

                // Tổng quan
                if (message.Contains("tổng quan") || message.Contains("overview") || message.Contains("tình hình"))
                {
                    return $"Phòng Curing hiện có {wip} sản phẩm đang xử lý (WIP) và {pass} sản phẩm đã PASS. " +
                           $"Chi tiết theo model: {string.Join(", ", passDetails.Select(d => $"{d.Model}: {d.Qty} pcs"))}.";
                }

                // WIP
                if (message.Contains("wip"))
                    return $"Hiện tại có {wip} sản phẩm đang trong quá trình sấy (WIP).";

                // PASS tổng
                if (message.Contains("pass tổng") || message.Contains("tổng pass"))
                    return $"Tổng số sản phẩm PASS hiện tại là {pass} pcs.";

                // Model nhiều nhất
                if (message.Contains("model") && (message.Contains("nhiều nhất") || message.Contains("cao nhất")))
                {
                    var top = passDetails.OrderByDescending(p => p.Qty).First();
                    return $"Model {top.Model} có số lượng PASS cao nhất với {top.Qty} pcs.";
                }

                // PASS từng model
                if (message.Contains("model") || message.Contains("pass từng"))
                    return $"Số lượng PASS theo model: {string.Join(", ", passDetails.Select(d => $"{d.Model}: {d.Qty} pcs"))}.";

                return "Xin lỗi, tôi chưa hiểu rõ yêu cầu về dashboard Curing.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CuringApiService] Lỗi HandleUserQueryAsync: {ex.Message}");
                return "Đã xảy ra lỗi khi truy vấn dữ liệu Curing.";
            }
        }
    }
}
