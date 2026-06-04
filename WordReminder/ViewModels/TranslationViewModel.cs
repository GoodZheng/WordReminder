using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordReminder.Models;
using WordReminder.Services;

namespace WordReminder.ViewModels;

/// <summary>
/// 翻译窗口 ViewModel
/// </summary>
public partial class TranslationViewModel : ViewModelBase
{
    private readonly AITranslationService _translationService;
    private readonly ConfigService _configService;
    private readonly DatabaseService _databaseService;
    private readonly TranslationHistoryService _historyService;  // new

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _isTranslating;

    [ObservableProperty]
    private string _translateButtonText = "翻译";

    partial void OnIsTranslatingChanged(bool value)
    {
        TranslateButtonText = value ? "翻译中" : "翻译";
    }

    [ObservableProperty]
    private string _loadingText = "请输入文本后点击翻译";

    [ObservableProperty]
    private string _translationDuration = string.Empty;

    [ObservableProperty]
    private bool _showLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _showError;

    [ObservableProperty]
    private TranslationResultViewModel? _translationResult;

    // History properties
    [ObservableProperty]
    private ObservableCollection<HistoryItemViewModel> _historyItems = new();

    [ObservableProperty]
    private HistoryItemViewModel? _selectedHistoryItem;

    [ObservableProperty]
    private int _totalHistoryCount;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    public List<int> PageSizeOptions { get; } = [10, 20, 50, 100];

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        LoadHistory();
    }

    [ObservableProperty]
    private string _activeModelInfo = string.Empty;

    private void UpdateActiveModelInfo()
    {
        var provider = _configService.GetActiveProvider();
        var modelId = _configService.GetActiveModelId();
        if (provider != null && !string.IsNullOrEmpty(modelId))
            ActiveModelInfo = $"{provider.Name}@{modelId}";
        else
            ActiveModelInfo = string.Empty;
    }

    public TranslationViewModel(ConfigService configService, AITranslationService translationService, DatabaseService databaseService, TranslationHistoryService historyService)
    {
        _configService = configService;
        _translationService = translationService;
        _databaseService = databaseService;
        _historyService = historyService;

        UpdateActiveModelInfo();
        ShowLoading = true;
        LoadHistory();
    }

    /// <summary>
    /// 加载历史列表（当前页）
    /// </summary>
    private void LoadHistory()
    {
        var (items, total) = _historyService.GetPaged(CurrentPage, PageSize);
        HistoryItems = new ObservableCollection<HistoryItemViewModel>(items.Select(i => new HistoryItemViewModel(i)));
        TotalHistoryCount = total;
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(ShowHistoryEmpty));
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 总页数
    /// </summary>
    public int PageCount => (int)Math.Ceiling((double)TotalHistoryCount / PageSize);

    /// <summary>
    /// 是否没有历史记录
    /// </summary>
    public bool ShowHistoryEmpty => HistoryItems.Count == 0;

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
        var aiConfig = _configService.GetActiveProvider();
        if (aiConfig == null || string.IsNullOrEmpty(aiConfig.ApiKey) || aiConfig.ApiKey == "your-api-key-here")
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
        TranslationDuration = string.Empty;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await _translationService.TranslateAsync(text);
            sw.Stop();

            // 显示翻译耗时
            TranslationDuration = sw.ElapsedMilliseconds >= 1000
                ? $"耗时 {sw.Elapsed.TotalSeconds:F1}s"
                : $"耗时 {sw.ElapsedMilliseconds}ms";

            if (result != null)
            {
                TranslationResult = new TranslationResultViewModel(result);
                ShowLoading = false;
                ShowError = false;
                StatusText = "翻译完成";

                // Save translation history
                try
                {
                    var fullJson = SerializeTranslationResult(result);
                    var historyId = _historyService.Insert(
                        inputText: text,
                        translatedText: result.TranslatedText,
                        fullJson: fullJson,
                        textType: result.Type,
                        direction: result.Direction);

                    // 仅在新记录插入成功时刷新列表
                    if (historyId > 0)
                    {
                        CurrentPage = 1;
                        LoadHistory();
                    }
                }
                catch (Exception ex)
                {
                    // History save failure doesn't affect translation display
                    StatusText = $"翻译完成，但历史保存失败: {ex.Message}";
                }
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

    /// <summary>
    /// 将翻译结果中的单词加入单词列表
    /// </summary>
    [RelayCommand]
    private void AddWord(WordDetailViewModel wordDetail)
    {
        if (wordDetail == null || string.IsNullOrWhiteSpace(wordDetail.Word))
            return;

        // 检查是否已存在
        if (_databaseService.WordExists(wordDetail.Word))
        {
            StatusText = $"单词 \"{wordDetail.Word}\" 已在列表中";
            return;
        }

        try
        {
            // 构建 Word 模型并写入
            var word = new Word
            {
                Text = wordDetail.Word,
                Phonetic = wordDetail.Phonetic,
                PartOfSpeech = wordDetail.PartOfSpeech,
                Definition = wordDetail.Definition,
                Example = wordDetail.Example
            };

            _databaseService.InsertWord(word);

            // 触发反馈动画（View 层通过 Storyboard 处理淡出）
            wordDetail.ShowFeedback = true;
            StatusText = $"已添加 \"{wordDetail.Word}\"";
        }
        catch (Exception ex)
        {
            StatusText = $"添加失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 检查单词是否已在列表中（供 View 层调用以更新菜单状态）
    /// </summary>
    public bool IsWordInList(string word)
    {
        return _databaseService.WordExists(word);
    }

    /// <summary>
    /// 选中历史项
    /// </summary>
    [RelayCommand]
    private void SelectHistory(HistoryItemViewModel item)
    {
        if (item == null || string.IsNullOrEmpty(item.FullJson))
            return;

        try
        {
            var jsonDoc = JsonDocument.Parse(item.FullJson);
            var result = AITranslationService.DeserializeTranslationResult(jsonDoc.RootElement, item.InputText);

            TranslationResult = new TranslationResultViewModel(result);
            ShowLoading = false;
            ShowError = false;
            StatusText = "查看历史记录";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"历史记录数据损坏: {ex.Message}";
            ShowError = true;
            ShowLoading = false;
            StatusText = "查看历史失败";
        }
    }

    /// <summary>
    /// Called when SelectedHistoryItem changes (generated by CommunityToolkit.Mvvm)
    /// </summary>
    partial void OnSelectedHistoryItemChanged(HistoryItemViewModel? value)
    {
        if (value != null)
        {
            SelectHistoryCommand.Execute(value);
        }
        else
        {
            TranslationResult = null;
        }
    }

    partial void OnHistoryItemsChanged(ObservableCollection<HistoryItemViewModel> value)
    {
        OnPropertyChanged(nameof(ShowHistoryEmpty));
        OnPropertyChanged(nameof(CanPreviousPage));
        OnPropertyChanged(nameof(CanNextPage));
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 删除历史项
    /// </summary>
    [RelayCommand]
    private void DeleteHistory(HistoryItemViewModel item)
    {
        if (item == null) return;

        _historyService.Delete(item.Id);

        // If deleting currently selected item, clear the right side
        if (SelectedHistoryItem?.Id == item.Id)
        {
            SelectedHistoryItem = null;
            TranslationResult = null;
            ShowLoading = true;
            StatusText = "就绪";
        }

        LoadHistory();
        StatusText = "已删除历史记录";
    }

    /// <summary>
    /// 上一页
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPreviousPage))]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            SelectedHistoryItem = null;
            LoadHistory();
        }
    }

    /// <summary>
    /// 下一页
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNextPage))]
    private void NextPage()
    {
        if (CurrentPage < PageCount)
        {
            CurrentPage++;
            SelectedHistoryItem = null;
            LoadHistory();
        }
    }

    /// <summary>
    /// CanExecute for PreviousPageCommand
    /// </summary>
    public bool CanPreviousPage => CurrentPage > 1;

    /// <summary>
    /// CanExecute for NextPageCommand
    /// </summary>
    public bool CanNextPage => CurrentPage < PageCount;

    /// <summary>
    /// 将翻译结果序列化为 JSON 存储到数据库
    /// </summary>
    private static string SerializeTranslationResult(AITranslationService.TranslationResult result)
    {
        return JsonSerializer.Serialize(result);
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
public partial class WordDetailViewModel : ObservableObject
{
    public string? Word { get; }
    public string? Phonetic { get; }
    public string? PartOfSpeech { get; }
    public string? Definition { get; }
    public string? Example { get; }
    public string? ExampleTranslation { get; }

    [ObservableProperty]
    private bool _showFeedback;

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
