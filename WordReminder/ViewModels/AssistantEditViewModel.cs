using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WordReminder.Models;
using WordReminder.Services;

namespace WordReminder.ViewModels;

/// <summary>
/// 助手编辑对话框 ViewModel
/// </summary>
public partial class AssistantEditViewModel : ViewModelBase
{
    private readonly AssistantService _assistantService;
    private readonly ConfigService _configService;
    private readonly Assistant? _originalAssistant;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _icon = "🤖";

    [ObservableProperty]
    private string _systemPrompt = string.Empty;

    [ObservableProperty]
    private string _selectedProviderName = string.Empty;

    [ObservableProperty]
    private string _selectedModelId = string.Empty;

    [ObservableProperty]
    private double _temperature = 0.7;

    [ObservableProperty]
    private int _maxTokens = 2000;

    [ObservableProperty]
    private ObservableCollection<AIProviderConfig> _providers = new();

    [ObservableProperty]
    private ObservableCollection<AIModelItem> _models = new();

    /// <summary>
    /// 是否为编辑模式（false 为新建模式）
    /// </summary>
    public bool IsEditing => _originalAssistant != null;

    /// <summary>
    /// 可用图标列表
    /// </summary>
    public static string[] AvailableIcons => new[]
    {
        "🤖", "📖", "🗣️", "✍️", "💡", "🎯", "🔬", "📝", "🌟", "🚀",
        "💻", "🎨", "📊", "🔧", "🎓", "💼", "🌍", "🎮", "📱", "⚡",
        "🔥", "💎", "🎵", "🎬", "📸", "🏆", "🎁", "🔮", "🧠", "❤️"
    };

    public AssistantEditViewModel(AssistantService assistantService, ConfigService configService, Assistant? assistant = null)
    {
        _assistantService = assistantService;
        _configService = configService;
        _originalAssistant = assistant;

        // 加载厂商列表
        Providers = new ObservableCollection<AIProviderConfig>(_configService.Settings.AIProviders);

        if (assistant != null)
        {
            // 编辑模式：加载现有数据
            Name = assistant.Name;
            Icon = assistant.Icon;
            SystemPrompt = assistant.SystemPrompt;
            SelectedProviderName = assistant.ProviderName ?? "";
            SelectedModelId = assistant.ModelId ?? "";
            Temperature = assistant.Temperature;
            MaxTokens = assistant.MaxTokens;

            // 加载模型列表
            LoadModels(SelectedProviderName);
        }
        else
        {
            // 新建模式：使用当前激活的厂商和模型
            var activeProvider = _configService.GetActiveProvider();
            if (activeProvider != null)
            {
                SelectedProviderName = activeProvider.Name;
                LoadModels(activeProvider.Name);

                var activeModelId = _configService.GetActiveModelId();
                if (!string.IsNullOrEmpty(activeModelId))
                {
                    SelectedModelId = activeModelId;
                }
                else if (Models.Count > 0)
                {
                    SelectedModelId = Models[0].ModelId;
                }
            }
        }
    }

    /// <summary>
    /// 当选中的厂商改变时，加载对应的模型列表
    /// </summary>
    partial void OnSelectedProviderNameChanged(string value)
    {
        LoadModels(value);
    }

    /// <summary>
    /// 加载指定厂商的模型列表
    /// </summary>
    private void LoadModels(string providerName)
    {
        Models.Clear();

        var provider = Providers.FirstOrDefault(p => p.Name == providerName);
        if (provider != null && provider.Models.Count > 0)
        {
            foreach (var model in provider.Models)
            {
                Models.Add(model);
            }

            // 如果当前选中的模型不在新列表中，则选择第一个
            if (!string.IsNullOrEmpty(SelectedModelId) && Models.All(m => m.ModelId != SelectedModelId))
            {
                SelectedModelId = Models[0].ModelId;
            }
        }
    }

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    /// <summary>
    /// 验证并保存，返回是否成功
    /// </summary>
    public bool TrySave()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Name)) missing.Add("名称");
        if (string.IsNullOrWhiteSpace(SystemPrompt)) missing.Add("系统提示词");

        if (missing.Count > 0)
        {
            ValidationMessage = $"请填写必填项：{string.Join("、", missing)}";
            return false;
        }

        ValidationMessage = string.Empty;

        var assistant = _originalAssistant ?? new Assistant();
        assistant.Name = Name.Trim();
        assistant.Icon = Icon;
        assistant.SystemPrompt = SystemPrompt.Trim();
        assistant.ProviderName = string.IsNullOrEmpty(SelectedProviderName) ? "" : SelectedProviderName;
        assistant.ModelId = string.IsNullOrEmpty(SelectedModelId) ? "" : SelectedModelId;
        assistant.Temperature = Temperature;
        assistant.MaxTokens = MaxTokens;

        if (IsEditing)
        {
            _assistantService.UpdateAssistant(assistant);
        }
        else
        {
            _assistantService.CreateAssistant(assistant);
        }

        return true;
    }

    partial void OnNameChanged(string value) => ClearValidation();
    partial void OnSystemPromptChanged(string value) => ClearValidation();

    private void ClearValidation()
    {
        if (!string.IsNullOrEmpty(ValidationMessage))
            ValidationMessage = string.Empty;
    }
}
