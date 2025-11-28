using System;
using System.Globalization;

namespace Chatbot.Services.Modules.Station
{
    public static class StationFilterParser
    {
        public static StationFilter Parse(string text) =>
            StationFilter.Parse(text);

        public class StationFilter
        {
            public string ModelSerial { get; set; } = "SWITCH";
            public string GroupName { get; set; } = "ICT";
            public string? Date { get; set; } = null;

            public static StationFilter Parse(string text)
            {
                var f = new StationFilter();
                text = text.ToLower();

                // MODEL SERIAL
                if (text.Contains("adapter")) f.ModelSerial = "ADAPTER";
                if (text.Contains("switch")) f.ModelSerial = "SWITCH";

                // GROUP NAME
                if (text.Contains("ct0") || text.Contains("cto")) f.GroupName = "CTO";
                if (text.Contains("ft")) f.GroupName = "FT";
                if (text.Contains("ict")) f.GroupName = "ICT";

                // NGÀY
                f.Date = ExtractDate(text);

                return f;
            }

            private static string? ExtractDate(string text)
            {
                text = text.ToLower().Trim();

                if (text.Contains("hôm nay") || text.Contains("today"))
                    return DateTime.Now.ToString("yyyy/MM/dd");

                if (text.Contains("hôm qua") || text.Contains("yesterday"))
                    return DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd");

                string[] fmts =
                {
                    "dd/MM/yyyy", "dd-MM-yyyy",
                    "yyyy/MM/dd", "yyyy-MM-dd"
                };

                foreach (var word in text.Split(' '))
                {
                    foreach (var fmt in fmts)
                    {
                        if (DateTime.TryParseExact(word, fmt, CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out DateTime dt))
                        {
                            return dt.ToString("yyyy/MM/dd");
                        }
                    }
                }

                return null;
            }
        }
    }
}
