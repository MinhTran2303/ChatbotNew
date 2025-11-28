using System.Text.Json;

namespace Chatbot.Services.Core
{
    public class ModulePromptLoader
    {
        private readonly string _configPath = "Services/Config/modules.json";

        public class ModuleConfigItem
        {
            public string Module { get; set; } = "";
            public string Name { get; set; } = "";
            public string PromptFile { get; set; } = "";
            public string IntentFile { get; set; } = "";
            public List<string> Keywords { get; set; } = new();
        }

        private readonly List<ModuleConfigItem> _modules = new();

        public ModulePromptLoader()
        {
            if (!File.Exists(_configPath))
                throw new Exception($"Không tìm thấy modules.json tại {_configPath}");

            var json = File.ReadAllText(_configPath);
            _modules = JsonSerializer.Deserialize<List<ModuleConfigItem>>(json) ?? new();
        }

        // ======================================================
        // FALLBACK keyword detect (không dùng trong Hybrid mới)
        // ======================================================
        public ModuleConfigItem? DetectModuleByKeyword(string message)
        {
            string text = message.ToLower().Trim();

            foreach (var m in _modules)
                foreach (var kw in m.Keywords)
                    if (text.Contains(kw.ToLower()))
                        return m;

            return null;
        }

//        // ======================================================
//        // BUILD PROMPT FOR SINGLE MODULE (giữ lại, không dùng)
//        // ======================================================
//        public string BuildIntentPrompt(ModuleConfigItem module, string userMessage)
//        {
//            string promptText = File.Exists(module.PromptFile)
//                ? File.ReadAllText(module.PromptFile)
//                : "";

//            string rawIntentJson = File.Exists(module.IntentFile)
//                ? File.ReadAllText(module.IntentFile)
//                : "{}";

//            var intents = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(rawIntentJson)
//                         ?? new Dictionary<string, List<string>>();

//            string flattened = string.Join("\n", intents.Select(i =>
//                $"{i.Key}: {string.Join(", ", i.Value)}"
//            ));

//            return $@"
//Bạn là Intent Router của Smart Factory AIOT.

//Nhiệm vụ:
//- Chỉ tìm intent trong MODULE `{module.Module}`
//- Không đoán module khác
//- Luôn trả về JSON:
//{{
//  ""module"": ""{module.Module}"",
//  ""intent"": ""TênIntentHoặcRỗng""
//}}

//📘 MÔ TẢ MODULE:
//{promptText}

//📄 DANH SÁCH INTENT:
//{flattened}

//❓ Câu hỏi người dùng:
//{userMessage}

//KHÔNG GIẢI THÍCH. CHỈ TRẢ JSON.
//";
//        }

        // ======================================================
        // HYBRID ROUTING — BUILD GLOBAL PROMPT
        // ======================================================
        public string BuildIntentPromptALL(string userMessage)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("Bạn là ROUTER AI của hệ thống Smart Factory AIOT.");
            sb.AppendLine("Nhiệm vụ của bạn:");
            sb.AppendLine("1) Xác định MODULE đúng:");
            sb.AppendLine("   - CuringRoom");
            sb.AppendLine("   - RackMonitoring");
            sb.AppendLine("   - Station");
            sb.AppendLine("2) Xác định intent đúng trong module đó.");
            sb.AppendLine("3) Chỉ trả về JSON đúng mẫu:");
            sb.AppendLine("{");
            sb.AppendLine("  \"module\": \"TênModule\",");
            sb.AppendLine("  \"intent\": \"TênIntent\"");
            sb.AppendLine("}");
            sb.AppendLine("Không giải thích thêm.");

            sb.AppendLine("\n===========================");
            sb.AppendLine("📌 DANH SÁCH MODULE & INTENT");
            sb.AppendLine("===========================");

            foreach (var module in _modules)
            {
                sb.AppendLine($"\n▌ MODULE: {module.Module}");

                string promptText = File.Exists(module.PromptFile)
                    ? File.ReadAllText(module.PromptFile)
                    : "";

                string intentJson = File.Exists(module.IntentFile)
                    ? File.ReadAllText(module.IntentFile)
                    : "{}";

                var intents = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(intentJson)
                            ?? new Dictionary<string, List<string>>();

                sb.AppendLine($"Mô tả: {promptText}");
                sb.AppendLine("Intent:");

                foreach (var kv in intents)
                    sb.AppendLine($"- {kv.Key}: {string.Join(", ", kv.Value)}");
            }

            sb.AppendLine("\n===========================");
            sb.AppendLine("❓ CÂU HỎI NGƯỜI DÙNG:");
            sb.AppendLine(userMessage);
            sb.AppendLine("===========================");

            sb.AppendLine("\nCHỈ TRẢ JSON.");

            return sb.ToString();
        }

        public ModuleConfigItem? GetModule(string moduleName) =>
            _modules.FirstOrDefault(x => x.Module == moduleName);
    }
}
