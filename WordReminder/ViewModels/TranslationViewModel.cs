using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordReminder.Services;

namespace WordReminder.ViewModels;

/// <summary>
/// 翻译窗口 ViewModel
/// </summary>
public partial class TranslationViewModel : ViewModelBase
{
    private readonly AITranslationService _translationService;
    private readonly ConfigService _configService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _isTranslating;

    [ObservableProperty]
    private string _loadingText = "请输入文本后点击翻译";

    [ObservableProperty]
    private bool _showLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _showError;

    [ObservableProperty]
    private TranslationResultViewModel? _translationResult;

    public TranslationViewModel(ConfigService configService, AITranslationService translationService)
    {
        _configService = configService;
        _translationService = translationService;

        ShowLoading = true;
    }

    /// <summary>
    /// 翻译命令
    /// </summary>
    [RelayCommand]
    private async Task TranslateAsync()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            StatusText = "请输入要翻译的文本";
            return;
        }

        // 检查 AI 配置
        var aiConfig = _configService.Settings.AIDictionary;
        if (!aiConfig.Enabled || string.IsNullOrEmpty(aiConfig.ApiKey) || aiConfig.ApiKey == "your-api-key-here")
        {
            StatusText = "AI 词典未配置";
            ErrorMessage = "AI 词典未配置，请先在设置中配置 API Key";
            ShowError = true;
            ShowLoading = false;
            TranslationResult = null;
            return;
        }

        IsTranslating = true;
        ShowLoading = true;
        ShowError = false;
        ErrorMessage = null;
        StatusText = "正在翻译...";

        try
        {
            var result = await _translationService.TranslateAsync(text);

            if (result != null)
            {
                TranslationResult = new TranslationResultViewModel(result);
                ShowLoading = false;
                ShowError = false;
                StatusText = "翻译完成";
            }
            else
            {
                ErrorMessage = "翻译结果为空";
                ShowError = true;
                ShowLoading = false;
                StatusText = "翻译失败";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"翻译失败: {ex.Message}";
            ShowError = true;
            ShowLoading = false;
            StatusText = "翻译失败";
        }
        finally
        {
            IsTranslating = false;
        }
    }
}

/// <summary>
/// 翻译结果 ViewModel
/// </summary>
public class TranslationResultViewModel
{
    public string? Type { get; }
    public string? Direction { get; }
    public string? Text { get; }
    public string? TranslatedText { get; }
    public List<WordDetailViewModel> WordDetails { get; }
    public List<TranslationOptionViewModel> Options { get; }

    public TranslationResultViewModel(AITranslationService.TranslationResult result)
    {
        Type = result.Type;
        Direction = result.Direction;
        Text = result.Text;
        TranslatedText = result.TranslatedText;

        WordDetails = result.WordDetails?.Select(w => new WordDetailViewModel(w)).ToList() ?? new List<WordDetailViewModel>();
        Options = result.Options?.Select(o => new TranslationOptionViewModel(o)).ToList() ?? new List<TranslationOptionViewModel>();
    }

    public bool IsError => Type == "error";
    public bool IsEnToZhWord => Direction == "en2zh" && Type == "word";
    public bool IsEnToZhSentence => Direction == "en2zh" && Type == "sentence";
    public bool IsZhToEn => Direction == "zh2en";
}

/// <summary>
/// 单词详情 ViewModel
/// </summary>
public class WordDetailViewModel
{
    public string? Word { get; }
    public string? Phonetic { get; }
    public string? PartOfSpeech { get; }
    public string? Definition { get; }
    public string? Example { get; }
    public string? ExampleTranslation { get; }

    public WordDetailViewModel(AITranslationService.WordInfo word)
    {
        Word = word.Word;
        Phonetic = word.Phonetic;
        PartOfSpeech = word.PartOfSpeech;
        Definition = word.Definition;
        Example = word.Example;
        ExampleTranslation = word.ExampleTranslation;
    }

    public string DisplayHeader => !string.IsNullOrEmpty(Phonetic) ? $"{Word} {Phonetic}" : Word ?? "";
    public string DisplayDefinition => !string.IsNullOrEmpty(PartOfSpeech) ? $"[{PartOfSpeech}] {Definition}" : Definition ?? "";
}

/// <summary>
/// 翻译选项 ViewModel
/// </summary>
public class TranslationOptionViewModel
{
    public string? Text { get; }
    public string? Scenario { get; }

    public TranslationOptionViewModel(AITranslationService.TranslationOption option)
    {
        Text = option.Text;
        Scenario = option.Scenario;
    }
}
