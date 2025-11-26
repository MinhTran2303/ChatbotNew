namespace Chatbot.Services.Modules.NVIDIA_SWITCH.CuringRoom
{
    public static class CuringFilterParser
    {
        public class CuringFilter
        {
            public string Factory { get; set; } = "F16";
            public string Floor { get; set; } = "3F";
            public string Area { get; set; } = "ROOM1";
            public string Customer { get; set; } = "NVIDIA";
            public string ModelSerial { get; set; } = "SWITCH";
        }

        public static CuringFilter Parse(string text)
        {
            var f = new CuringFilter();

            if (text.Contains("f17", StringComparison.OrdinalIgnoreCase))
                f.Factory = "F17";
            if (text.Contains("f16", StringComparison.OrdinalIgnoreCase))
                f.Factory = "F16";

            // Floor
            if (text.Contains("3f", StringComparison.OrdinalIgnoreCase))
                f.Floor = "3F";

            // Area (room)
            if (text.Contains("room1", StringComparison.OrdinalIgnoreCase))
                f.Area = "ROOM1";

            return f;
        }
    }
}
