using System.Text.RegularExpressions;

namespace Chatbot.Services.Rack
{
    public class RackFilter
    {
        public string Factory { get; set; } = "F16";
        public string Floor { get; set; } = "3F";
        public string Room { get; set; } = "ALL";
        public string Group { get; set; } = "J_TAG";
        public string Model { get; set; } = "ALL";
        public string NickName { get; set; } = "ALL"; 
    }

    public static class RackFilterParser
    {
        public static RackFilter Parse(string userMessage)
        {
            var text = userMessage.ToUpper();
            var f = new RackFilter();

            // FACTORY
            if (text.Contains("F17")) f.Factory = "F17";
            else if (text.Contains("F16")) f.Factory = "F16";

            // FLOOR (hiện tại chỉ 3F)
            if (text.Contains("3F")) f.Floor = "3F";

            // ROOM
            if (text.Contains("ROOM 1")) f.Room = "ROOM 1";
            else if (text.Contains("ROOM 2")) f.Room = "ROOM 2";
            else if (text.Contains("ROOM 3")) f.Room = "ROOM 3";
            else if (text.Contains("ROOM")) f.Room = "ROOM";

            // GROUP
            if (text.Contains("CTO")) f.Group = "CTO";
            else if (text.Contains("FT")) f.Group = "FT";
            else if (text.Contains("JTAG") || text.Contains("J_TAG")) f.Group = "J_TAG";

            // MODEL (GB200 / GB300)
            if (text.Contains("GB200")) f.Model = "GB200";
            else if (text.Contains("GB300")) f.Model = "GB300";

            return f;
        }
    }
}
