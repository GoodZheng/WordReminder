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

    // AI 多厂商配置
    public List<AIProviderConfig> AIProviders { get; set; } = [];
    public string ActiveProviderName { get; set; } = "";
    public string ActiveModelId { get; set; } = "";

    // 是否已初始化过默认单词（首次启动后设为 true，用户删除单词后不再自动添加）
    public bool DefaultWordsInitialized { get; set; } = false;

    // 开机自启
    public bool AutoStart { get; set; } = false;

    // 自动更新
    public bool AutoUpdate { get; set; } = true;

    // 全局快捷键设置
    public HotKeySettings HotKeys { get; set; } = new();
}

/// <summary>
/// AI 厂商配置
/// </summary>
public class AIProviderConfig
{
    public string Name { get; set; } = "";
    public string ApiUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool IsBuiltin { get; set; }
    public List<AIModelItem> Models { get; set; } = [];

    public AIProviderConfig Clone()
    {
        return new AIProviderConfig
        {
            Name = Name,
            ApiUrl = ApiUrl,
            ApiKey = ApiKey,
            IsBuiltin = IsBuiltin,
            Models = Models.Select(m => new AIModelItem { ModelId = m.ModelId, DisplayName = m.DisplayName }).ToList()
        };
    }
}

/// <summary>
/// AI 模型条目
/// </summary>
public class AIModelItem
{
    public string ModelId { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
