using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using WordReminder.Models;

namespace WordReminder.Services;

public class ConfigService
{
    private readonly string _configPath;
    private AppSettings _settings;

    public ConfigService()
    {
        // 使用用户目录存储配置，避免权限问题
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordReminder");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        _configPath = Path.Combine(dataDir, "appsettings.json");
        _settings = LoadOrMigrateSettings();
    }

    public AppSettings Settings => _settings;

    /// <summary>
    /// 获取预置厂商列表（已废弃，保留仅用于旧版本迁移）
    /// </summary>
    public static List<AIProviderConfig> GetBuiltinProviders()
    {
        return [];
    }

    /// <summary>
    /// 获取当前激活的厂商配置
    /// </summary>
    public AIProviderConfig? GetActiveProvider()
    {
        if (string.IsNullOrEmpty(_settings.ActiveProviderName))
            return null;

        return _settings.AIProviders.FirstOrDefault(p => p.Name == _settings.ActiveProviderName);
    }

    /// <summary>
    /// 获取当前激活的模型 ID
    /// </summary>
    public string GetActiveModelId()
    {
        return _settings.ActiveModelId ?? "";
    }

    /// <summary>
    /// 重新从文件加载配置
    /// </summary>
    public void ReloadSettings()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    _settings = settings;
                }
            }
            catch
            {
                // 读取失败，保持现有配置
            }
        }
    }

    private AppSettings LoadOrMigrateSettings()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings != null)
                {
                    // 检查是否需要从旧版本迁移
                    var jsonNode = JsonNode.Parse(json);
                    if (jsonNode != null)
                    {
                        settings = MigrateFromOldVersion(settings, jsonNode);
                    }

                    // 确保新字段有默认值
                    EnsureDefaultValues(settings);
                    return settings;
                }
            }
            catch { }
        }

        // 创建新配置
        var newSettings = new AppSettings();
        _settings = newSettings;
        SaveSettings();
        return newSettings;
    }

    /// <summary>
    /// 从旧版本配置迁移到新版本
    /// </summary>
    private AppSettings MigrateFromOldVersion(AppSettings settings, JsonNode jsonNode)
    {
        var migrated = false;
        var defaultSettings = new AppSettings();

        // 旧版本只有 FontSize（通用字体大小），需要迁移到各个部分
        if (jsonNode["FontSize"] is JsonNode oldFontSizeNode &&
            jsonNode["WordFontSize"] is null)
        {
            if (oldFontSizeNode.GetValue<int>() is int oldFontSize)
            {
                settings.WordFontSize = oldFontSize;
                settings.PhoneticFontSize = (int)(oldFontSize * 0.6);
                settings.DefinitionFontSize = (int)(oldFontSize * 0.7);
                settings.ExampleFontSize = (int)(oldFontSize * 0.5);
                migrated = true;
            }
        }

        // 旧版本只有 FontColor（通用字体颜色）
        if (jsonNode["FontColor"] is JsonNode oldFontColorNode &&
            jsonNode["WordFontColor"] is null)
        {
            if (oldFontColorNode.GetValue<string>() is string oldFontColor)
            {
                settings.WordFontColor = oldFontColor;
                migrated = true;
            }
        }

        // 迁移旧的 AIDictionary 单配置到新的 AIProviders 列表
        if (jsonNode["AIDictionary"] is JsonNode oldAI && settings.AIProviders.Count == 0)
        {
            var provider = oldAI["Provider"]?.GetValue<string>() ?? "custom";
            var apiUrl = oldAI["ApiUrl"]?.GetValue<string>() ?? "";
            var apiKey = oldAI["ApiKey"]?.GetValue<string>() ?? "";
            var model = oldAI["Model"]?.GetValue<string>() ?? "";

            settings.AIProviders.Add(new AIProviderConfig
            {
                Name = provider,
                ApiUrl = apiUrl,
                ApiKey = apiKey,
                IsBuiltin = false,
                Models = [new AIModelItem { ModelId = model, DisplayName = model }]
            });
            settings.ActiveProviderName = provider;
            settings.ActiveModelId = model;

            migrated = true;
        }

        // 如果进行了迁移，保存新配置
        if (migrated)
        {
            _settings = settings;
            SaveSettings();
        }

        return settings;
    }

    /// <summary>
    /// 确保所有新字段都有默认值
    /// </summary>
    private void EnsureDefaultValues(AppSettings settings)
    {
        var migrated = false;

        // 单词字体设置
        if (settings.WordFontSize == 0)
        {
            settings.WordFontSize = 32;
            migrated = true;
        }
        if (string.IsNullOrEmpty(settings.WordFontColor))
        {
            settings.WordFontColor = "#FFFFFF";
            migrated = true;
        }
        if (string.IsNullOrEmpty(settings.WordFontFamily))
        {
            settings.WordFontFamily = "Segoe UI";
            migrated = true;
        }

        // 音标字体设置
        if (settings.PhoneticFontSize == 0)
        {
            settings.PhoneticFontSize = 16;
            migrated = true;
        }
        if (string.IsNullOrEmpty(settings.PhoneticFontColor))
        {
            settings.PhoneticFontColor = "#CCCCCC";
            migrated = true;
        }
        if (string.IsNullOrEmpty(settings.PhoneticFontFamily))
        {
            settings.PhoneticFontFamily = "Segoe UI";
            migrated = true;
        }

        // 释义字体设置
        if (settings.DefinitionFontSize == 0)
        {
            settings.DefinitionFontSize = 18;
            migrated = true;
        }
        if (string.IsNullOrEmpty(settings.DefinitionFontColor))
        {
            settings.DefinitionFontColor = "#EEEEEE";
            migrated = true;
        }
        if (string.IsNullOrEmpty(settings.DefinitionFontFamily))
        {
            settings.DefinitionFontFamily = "Segoe UI";
            migrated = true;
        }

        // 例句字体设置
        if (settings.ExampleFontSize == 0)
        {
            settings.ExampleFontSize = 14;
            migrated = true;
        }
        if (string.IsNullOrEmpty(settings.ExampleFontColor))
        {
            settings.ExampleFontColor = "#AAAAAA";
            migrated = true;
        }
        if (string.IsNullOrEmpty(settings.ExampleFontFamily))
        {
            settings.ExampleFontFamily = "Segoe UI";
            migrated = true;
        }

        // 显示开关
        if (!jsonNodeHasValue(nameof(settings.ShowExample)))
        {
            settings.ShowExample = true;
            migrated = true;
        }

        // 确保 AIProviders 列表已初始化（不再自动添加默认厂商）
        // 保持空列表，由用户自行添加

        // 确保有激活的厂商
        if (string.IsNullOrEmpty(settings.ActiveProviderName) && settings.AIProviders.Count > 0)
        {
            settings.ActiveProviderName = settings.AIProviders[0].Name;
            migrated = true;
        }

        if (string.IsNullOrEmpty(settings.ActiveModelId))
        {
            var activeProvider = settings.AIProviders.FirstOrDefault(p => p.Name == settings.ActiveProviderName);
            if (activeProvider?.Models.Count > 0)
            {
                settings.ActiveModelId = activeProvider.Models[0].ModelId;
            }
            migrated = true;
        }

        if (migrated)
        {
            _settings = settings;
            SaveSettings();
        }
    }

    private bool jsonNodeHasValue(string propertyName)
    {
        if (!File.Exists(_configPath))
            return false;

        try
        {
            var json = File.ReadAllText(_configPath);
            var jsonNode = JsonNode.Parse(json);
            return jsonNode?[propertyName] != null;
        }
        catch
        {
            return false;
        }
    }

    public void SaveSettings()
    {
        // 清理无效数值（NaN、Infinity）
        CleanInvalidValues();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        var json = JsonSerializer.Serialize(_settings, options);
        File.WriteAllText(_configPath, json);
    }

    private void CleanInvalidValues()
    {
        // 确保所有 double 值都是有效数字
        if (double.IsNaN(_settings.WindowPositionX) || double.IsInfinity(_settings.WindowPositionX))
            _settings.WindowPositionX = 100;
        if (double.IsNaN(_settings.WindowPositionY) || double.IsInfinity(_settings.WindowPositionY))
            _settings.WindowPositionY = 100;
        if (double.IsNaN(_settings.WindowWidth) || double.IsInfinity(_settings.WindowWidth) || _settings.WindowWidth <= 0)
            _settings.WindowWidth = 400;
        if (double.IsNaN(_settings.WindowHeight) || double.IsInfinity(_settings.WindowHeight) || _settings.WindowHeight <= 0)
            _settings.WindowHeight = 200;
        if (double.IsNaN(_settings.Opacity) || double.IsInfinity(_settings.Opacity))
            _settings.Opacity = 1.0;
    }

    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        updateAction(_settings);
        SaveSettings();
    }
}
