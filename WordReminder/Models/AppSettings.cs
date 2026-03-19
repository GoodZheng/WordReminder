namespace WordReminder.Models;

public class AppSettings
{
    public int IntervalSeconds { get; set; } = 10;
    public double Opacity { get; set; } = 1.0;
    public double WindowPositionX { get; set; } = 100;
    public double WindowPositionY { get; set; } = 100;
    public double WindowWidth { get; set; } = 400;
    public double WindowHeight { get; set; } = 200;
    public bool AlwaysOnTop { get; set; } = true;

    // 显示开关
    public bool ShowPhonetic { get; set; } = true;
    public bool ShowDefinition { get; set; } = true;
    public bool ShowExample { get; set; } = true;

    // 单词字体设置
    public int WordFontSize { get; set; } = 32;
    public string WordFontColor { get; set; } = "#FFFFFF";
    public string WordFontFamily { get; set; } = "Segoe UI";

    // 音标字体设置
    public int PhoneticFontSize { get; set; } = 16;
    public string PhoneticFontColor { get; set; } = "#CCCCCC";
    public string PhoneticFontFamily { get; set; } = "Segoe UI";

    // 释义字体设置
    public int DefinitionFontSize { get; set; } = 18;
    public string DefinitionFontColor { get; set; } = "#EEEEEE";
    public string DefinitionFontFamily { get; set; } = "Segoe UI";

    // 例句字体设置
    public int ExampleFontSize { get; set; } = 14;
    public string ExampleFontColor { get; set; } = "#AAAAAA";
    public string ExampleFontFamily { get; set; } = "Segoe UI";

    // AI 词典配置
    public AIDictionarySettings AIDictionary { get; set; } = new();

    // 是否已初始化过默认单词（首次启动后设为 true，用户删除单词后不再自动添加）
    public bool DefaultWordsInitialized { get; set; } = false;

    // 开机自启
    public bool AutoStart { get; set; } = false;

    // 自动更新
    public bool AutoUpdate { get; set; } = true;
}

public class AIDictionarySettings
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "dashscope";  // dashscope, zhipuai, openai
    public string ApiUrl { get; set; } = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "qwen-plus";
    public string SystemPrompt { get; set; } = "你是专业的英汉词典助手。请严格按照JSON格式返回单词信息。";
}
