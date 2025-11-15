namespace Chatbot.Models
{
    public class ModuleConfig
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public string? Database { get; set; }
        public string? Api { get; set; }
        public string? Prompt { get; set; }
    }
}
