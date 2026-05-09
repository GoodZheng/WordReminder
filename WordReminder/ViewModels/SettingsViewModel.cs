using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using WordReminder.Messages;
using WordReminder.Models;
using WordReminder.Services;
using WordReminder.Controls;
using System.Diagnostics;
using System.IO;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace WordReminder.ViewModels;

/// <summary>
/// 设置窗口 ViewModel - 管理应用配置
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly DatabaseService _databaseService;
    private readonly UpdateService _updateService;
    private readonly IMessenger _messenger;
    private readonly HotKeyService _hotKeyService;
    private readonly BingDictionaryService _bingDictionaryService;
    private readonly AIConnectivityTestService _connectivityTestService;
    private UpdateInfo? _availableUpdate;

    // 窗口设置
    [ObservableProperty]
    private int _intervalSeconds;

    [ObservableProperty]
    private double _opacity;

    [ObservableProperty]
    private bool _alwaysOnTop;

    [ObservableProperty]
    private bool _autoStart;

    // 显示开关
    [ObservableProperty]
    private bool _showPhonetic;

    [ObservableProperty]
    private bool _showDefinition;

    [ObservableProperty]
    private bool _showExample;

    // 字体大小
    [ObservableProperty]
    private int _wordFontSize;

    [ObservableProperty]
    private int _phoneticFontSize;

    [ObservableProperty]
    private int _definitionFontSize;

    [ObservableProperty]
    private int _exampleFontSize;

    // 颜色
    [ObservableProperty]
    private string _wordColor = "#FFFFFF";

    [ObservableProperty]
    private string _phoneticColor = "#CCCCCC";

    [ObservableProperty]
    private string _definitionColor = "#EEEEEE";

    [ObservableProperty]
    private string _exampleColor = "#AAAAAA";

    // 单词数据
    [ObservableProperty]
    private string _wordCountText = "当前单词数: 0";

    // 更新
    [ObservableProperty]
    private bool _autoUpdate;

    [ObservableProperty]
    private string _currentVersionText = "1.0.0";

    [ObservableProperty]
    private string _latestVersionText = "点击检查更新";

    [ObservableProperty]
    private bool _isUpdateButtonEnabled = true;

    [ObservableProperty]
    private string _updateButtonText = "检查更新";

    [ObservableProperty]
    private bool _showDownloadProgress;

    [ObservableProperty]
    private string _downloadStatusText = "正在下载...";

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private bool _isReloadWordsEnabled = true;

    [ObservableProperty]
    private string _reloadWordsButtonText = "重新加载单词数据";

    // 快捷键设置
    [ObservableProperty]
    private HotKey _previousHotKey = new();

    [ObservableProperty]
    private HotKey _nextHotKey = new();

    [ObservableProperty]
    private HotKey _playPauseHotKey = new();

    [ObservableProperty]
    private HotKey _translationHotKey = new();

    [ObservableProperty]
    private HotKey _bringToFrontHotKey = new();

    // ==================== AI 多厂商配置 ====================

    /// <summary>
    /// 所有厂商配置（包括预置和自定义）
    /// </summary>
    public ObservableCollection<AIProviderConfig> Providers { get; } = [];

    [ObservableProperty]
    private AIProviderConfig? _selectedProvider;

    [ObservableProperty]
    private string _apiUrl = string.Empty;

    [ObservableProperty]
    private string _modelId = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    /// <summary>
    /// 当前选中厂商的模型 ID 列表
    /// </summary>
    public ObservableCollection<string> CurrentModels { get; } = [];

    [ObservableProperty]
    private string _selectedModel = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _testResultText = string.Empty;

    [ObservableProperty]
    private bool _testResultSuccess;

    public Brush TestResultBrush => TestResultSuccess
        ? new SolidColorBrush(Colors.Green)
        : new SolidColorBrush(Colors.Red);

    partial void OnTestResultSuccessChanged(bool value)
    {
        OnPropertyChanged(nameof(TestResultBrush));
    }

    public SettingsViewModel(
        ConfigService configService,
        DatabaseService databaseService,
        UpdateService updateService,
        IMessenger messenger,
        HotKeyService hotKeyService,
        BingDictionaryService bingDictionaryService,
        AIConnectivityTestService connectivityTestService)
    {
        _configService = configService;
        _databaseService = databaseService;
        _updateService = updateService;
        _messenger = messenger;
        _hotKeyService = hotKeyService;
        _bingDictionaryService = bingDictionaryService;
        _connectivityTestService = connectivityTestService;

        LoadSettings();
    }

    partial void OnSelectedProviderChanged(AIProviderConfig? value)
    {
        if (value == null)
        {
            ApiUrl = string.Empty;
            ApiKey = string.Empty;
            ModelId = string.Empty;
            SelectedModel = string.Empty;
            CurrentModels.Clear();
            return;
        }

        ApiUrl = value.ApiUrl;
        ApiKey = value.ApiKey;

        CurrentModels.Clear();
        foreach (var model in value.Models)
        {
            CurrentModels.Add(model.ModelId);
        }

        // 切换厂商后清空模型，让用户重新选择
        SelectedModel = string.Empty;
        ModelId = string.Empty;

        TestResultText = string.Empty;
    }

    partial void OnSelectedModelChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            ModelId = value;
        }
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        var settings = _configService.Settings;

        // 窗口设置
        IntervalSeconds = settings.IntervalSeconds;
        Opacity = settings.Opacity;
        AlwaysOnTop = settings.AlwaysOnTop;
        AutoStart = settings.AutoStart;

        // 加载 AI 厂商配置
        LoadAIProviders();

        // 自动更新
        AutoUpdate = settings.AutoUpdate;

        // 显示开关
        ShowPhonetic = settings.ShowPhonetic;
        ShowDefinition = settings.ShowDefinition;
        ShowExample = settings.ShowExample;

        // 字体大小
        WordFontSize = settings.WordFontSize;
        PhoneticFontSize = settings.PhoneticFontSize;
        DefinitionFontSize = settings.DefinitionFontSize;
        ExampleFontSize = settings.ExampleFontSize;

        // 颜色
        WordColor = settings.WordFontColor;
        PhoneticColor = settings.PhoneticFontColor;
        DefinitionColor = settings.DefinitionFontColor;
        ExampleColor = settings.ExampleFontColor;

        // 显示单词数
        var words = _databaseService.GetAllWords();
        WordCountText = $"当前单词数: {words.Count}";

        // 版本信息
        CurrentVersionText = UpdateService.GetCurrentVersionString();
        LatestVersionText = "点击检查更新";

        // 快捷键设置
        if (settings.HotKeys != null)
        {
            PreviousHotKey = settings.HotKeys.Previous?.Clone() ?? new HotKey();
            NextHotKey = settings.HotKeys.Next?.Clone() ?? new HotKey();
            PlayPauseHotKey = settings.HotKeys.PlayPause?.Clone() ?? new HotKey();
            TranslationHotKey = settings.HotKeys.Translation?.Clone() ?? new HotKey();
            BringToFrontHotKey = settings.HotKeys.BringToFront?.Clone() ?? new HotKey();
        }
    }

    private void LoadAIProviders()
    {
        var settings = _configService.Settings;

        Providers.Clear();

        // 加载已保存的厂商配置
        foreach (var provider in settings.AIProviders)
        {
            Providers.Add(provider);
        }

        // 选中激活的厂商
        var activeProvider = settings.ActiveProviderName;
        SelectedProvider = Providers.FirstOrDefault(p => p.Name == activeProvider)
                           ?? Providers.FirstOrDefault();
    }

    // ==================== Commands ====================

    /// <summary>
    /// 保存设置命令
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        _configService.UpdateSettings(s =>
        {
            // 窗口设置
            s.IntervalSeconds = IntervalSeconds;
            s.Opacity = Opacity;
            s.AlwaysOnTop = AlwaysOnTop;
            s.AutoStart = AutoStart;
            s.AutoUpdate = AutoUpdate;

            // AI 配置 - 保存所有厂商配置
            SyncCurrentProviderToCollection();
            s.AIProviders = Providers.Select(p => p.Clone()).ToList();
            s.ActiveProviderName = SelectedProvider?.Name ?? "";
            s.ActiveModelId = ModelId;

            // 显示开关
            s.ShowPhonetic = ShowPhonetic;
            s.ShowDefinition = ShowDefinition;
            s.ShowExample = ShowExample;

            // 字体大小
            s.WordFontSize = WordFontSize;
            s.PhoneticFontSize = PhoneticFontSize;
            s.DefinitionFontSize = DefinitionFontSize;
            s.ExampleFontSize = ExampleFontSize;

            // 颜色
            s.WordFontColor = WordColor;
            s.PhoneticFontColor = PhoneticColor;
            s.DefinitionFontColor = DefinitionColor;
            s.ExampleFontColor = ExampleColor;

            // 快捷键设置
            if (s.HotKeys == null)
            {
                s.HotKeys = new HotKeySettings();
            }
            s.HotKeys.Previous = PreviousHotKey.Clone();
            s.HotKeys.Next = NextHotKey.Clone();
            s.HotKeys.PlayPause = PlayPauseHotKey.Clone();
            s.HotKeys.Translation = TranslationHotKey.Clone();
            s.HotKeys.BringToFront = BringToFrontHotKey.Clone();
        });

        // 应用开机自启设置
        SetAutoStart(AutoStart);

        // 发送设置更改消息
        _messenger.Send(new SettingsChangedMessage());

        // 关闭设置窗口
        _messenger.Send(new CloseSettingsMessage());
    }

    /// <summary>
    /// 将当前编辑中的厂商配置同步回集合
    /// </summary>
    private void SyncCurrentProviderToCollection()
    {
        if (SelectedProvider == null) return;

        SelectedProvider.ApiUrl = ApiUrl;
        SelectedProvider.ApiKey = ApiKey;

        // 同步模型列表
        var currentModelIds = CurrentModels.ToList();
        if (!currentModelIds.Contains(ModelId) && !string.IsNullOrEmpty(ModelId))
        {
            CurrentModels.Add(ModelId);
        }
        SelectedProvider.Models = CurrentModels.Select(m => new AIModelItem
        {
            ModelId = m,
            DisplayName = m
        }).ToList();
    }

    /// <summary>
    /// 添加自定义厂商
    /// </summary>
    [RelayCommand]
    private void AddProvider()
    {
        // 通过消息请求 View 弹出输入对话框
        _messenger.Send(new AddProviderMessage());
    }

    /// <summary>
    /// 由 View 调用，完成添加厂商
    /// </summary>
    public void CompleteAddProvider(string name, string apiUrl)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        // 检查是否重名
        if (Providers.Any(p => p.Name == name))
        {
            MessageBox.Show($"厂商 \"{name}\" 已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var provider = new AIProviderConfig
        {
            Name = name.Trim(),
            ApiUrl = apiUrl.Trim(),
            IsBuiltin = false,
            Models = []
        };

        Providers.Add(provider);
        SelectedProvider = provider;
    }

    /// <summary>
    /// 删除厂商
    /// </summary>
    [RelayCommand]
    private void DeleteProvider()
    {
        if (SelectedProvider == null) return;

        // 自定义厂商：删除
        var deleteResult = MessageBox.Show(
            $"确定删除厂商 \"{SelectedProvider.Name}\"？",
            "删除厂商",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (deleteResult != MessageBoxResult.Yes) return;

        var index = Providers.IndexOf(SelectedProvider);
        Providers.Remove(SelectedProvider);

        if (Providers.Count > 0)
        {
            SelectedProvider = Providers[Math.Min(index, Providers.Count - 1)];
        }
        else
        {
            SelectedProvider = null;
        }
    }

    /// <summary>
    /// 为当前厂商添加模型
    /// </summary>
    [RelayCommand]
    private void AddModel()
    {
        if (SelectedProvider == null) return;

        // 通过消息请求 View 弹出输入对话框
        _messenger.Send(new AddModelMessage());
    }

    /// <summary>
    /// 由 View 调用，完成添加模型
    /// </summary>
    public void CompleteAddModel(string modelId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(modelId) || SelectedProvider == null) return;

        modelId = modelId.Trim();

        if (CurrentModels.Contains(modelId))
        {
            MessageBox.Show($"模型 \"{modelId}\" 已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CurrentModels.Add(modelId);
        SelectedModel = modelId;
        ModelId = modelId;
    }

    /// <summary>
    /// 删除当前选中的模型
    /// </summary>
    [RelayCommand]
    private void DeleteModel()
    {
        if (SelectedProvider == null || string.IsNullOrEmpty(SelectedModel)) return;

        CurrentModels.Remove(SelectedModel);
        SelectedModel = string.Empty;
        ModelId = string.Empty;
    }

    /// <summary>
    /// 测试 AI 连通性
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (SelectedProvider == null)
        {
            TestResultText = "请先选择厂商";
            TestResultSuccess = false;
            return;
        }

        // 同步当前编辑内容到 provider
        SyncCurrentProviderToCollection();

        IsTesting = true;
        TestResultText = "测试中...";

        try
        {
            var testProvider = new AIProviderConfig
            {
                Name = SelectedProvider.Name,
                ApiUrl = ApiUrl,
                ApiKey = ApiKey,
                IsBuiltin = SelectedProvider.IsBuiltin,
                Models = CurrentModels.Select(m => new AIModelItem { ModelId = m, DisplayName = m }).ToList()
            };

            var result = await _connectivityTestService.TestConnectionAsync(testProvider, ModelId);
            TestResultSuccess = result.Success;
            TestResultText = result.Message;
        }
        catch (Exception ex)
        {
            TestResultSuccess = false;
            TestResultText = $"测试失败: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>
    /// 重新加载单词命令
    /// </summary>
    [RelayCommand]
    private async Task ReloadWordsAsync()
    {
        IsReloadWordsEnabled = false;
        ReloadWordsButtonText = "正在加载...";

        try
        {
            // 清空现有数据
            _databaseService.ClearAllWords();

            // 预置单词
            var defaultWords = new[] { "ability" };
            var bingService = _bingDictionaryService;

            foreach (var wordText in defaultWords)
            {
                var word = await bingService.GetWordInfoAsync(wordText);
                if (word != null)
                {
                    _databaseService.InsertWord(word);
                }
            }

            var words = _databaseService.GetAllWords();
            WordCountText = $"当前单词数: {words.Count}";

            // 发送单词更改消息
            _messenger.Send(new WordsChangedMessage());

            MessageBox.Show("单词数据已重新加载！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsReloadWordsEnabled = true;
            ReloadWordsButtonText = "重新加载单词数据";
        }
    }

    /// <summary>
    /// 统一更新按钮：检查更新 / 下载更新 二合一
    /// </summary>
    [RelayCommand]
    private async Task UpdateAsync()
    {
        // 如果已有可用更新，直接下载
        if (_availableUpdate != null)
        {
            await DownloadUpdateInternalAsync();
            return;
        }

        // 否则执行检查更新
        IsUpdateButtonEnabled = false;
        UpdateButtonText = "检查中...";

        try
        {
            var updateInfo = await _updateService.CheckForUpdateAsync();

            if (updateInfo != null)
            {
                _availableUpdate = updateInfo;
                LatestVersionText = updateInfo.VersionString;
                LatestVersionForegroundColor = new SolidColorBrush(Colors.Orange);
                UpdateButtonText = $"下载 {updateInfo.VersionString}";

                MessageBox.Show(
                    $"发现新版本 {updateInfo.VersionString}！\n\n{updateInfo.ReleaseNotes}",
                    "发现新版本",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                LatestVersionText = "已是最新版本";
                LatestVersionForegroundColor = new SolidColorBrush(Colors.Green);
                UpdateButtonText = "检查更新";

                MessageBox.Show(
                    "当前已是最新版本！",
                    "检查更新",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            LatestVersionText = "检查失败";
            LatestVersionForegroundColor = new SolidColorBrush(Colors.Red);
            UpdateButtonText = "检查更新";

            MessageBox.Show(
                $"检查更新失败：{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsUpdateButtonEnabled = true;
        }
    }

    /// <summary>
    /// 内部：下载更新
    /// </summary>
    private async Task DownloadUpdateInternalAsync()
    {
        IsUpdateButtonEnabled = false;
        UpdateButtonText = "准备下载...";
        ShowDownloadProgress = true;

        try
        {
            var downloadUrl = await _updateService.GetDownloadUrlAsync();
            if (string.IsNullOrEmpty(downloadUrl))
            {
                MessageBox.Show("无法获取下载链接，请手动前往 GitHub 下载。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateService.OpenReleasesPage("GoodZheng", "WordReminder");
                UpdateButtonText = "检查更新";
                _availableUpdate = null;
                return;
            }

            var tempPath = Path.GetTempPath();
            var fileName = downloadUrl.Split('/').Last();
            var destinationPath = Path.Combine(tempPath, fileName);

            DownloadStatusText = "正在下载...";
            UpdateButtonText = "下载中...";

            var progress = new Progress<double>(percent =>
            {
                DownloadProgress = percent;
                DownloadStatusText = $"正在下载... {percent:F0}%";
            });

            var success = await _updateService.DownloadUpdateAsync(downloadUrl, destinationPath, progress);

            if (success && File.Exists(destinationPath))
            {
                DownloadStatusText = "下载完成！";

                var result = MessageBox.Show(
                    "更新下载完成！是否立即安装？\n\n选择「是」将启动安装程序并关闭应用。",
                    "下载完成",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    UpdateService.StartInstaller(destinationPath);
                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    UpdateButtonText = "检查更新";
                    _availableUpdate = null;
                }
            }
            else
            {
                DownloadStatusText = "下载失败";
                UpdateButtonText = "检查更新";
                _availableUpdate = null;
                MessageBox.Show("下载更新失败，请稍后重试或手动下载。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            DownloadStatusText = "下载失败";
            UpdateButtonText = "检查更新";
            _availableUpdate = null;
            MessageBox.Show($"下载更新失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsUpdateButtonEnabled = true;
            ShowDownloadProgress = false;
        }
    }

    /// <summary>
    /// 打开 GitHub 链接命令
    /// </summary>
    [RelayCommand]
    private void OpenGitHubLink()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/GoodZheng/WordReminder",
                UseShellExecute = true
            });
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 设置开机自启
    /// </summary>
    private void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (key != null)
            {
                var appName = "WordReminder";
                if (enable)
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"开机自启设置失败：{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // 辅助属性（用于 XAML 绑定颜色）
    [ObservableProperty]
    private Brush _latestVersionForegroundColor = new SolidColorBrush(Colors.Gray);

    [ObservableProperty]
    private Brush _wordColorBrush = new SolidColorBrush(Colors.White);

    [ObservableProperty]
    private Brush _phoneticColorBrush = new SolidColorBrush(Colors.LightGray);

    [ObservableProperty]
    private Brush _definitionColorBrush = new SolidColorBrush(Colors.White);

    [ObservableProperty]
    private Brush _exampleColorBrush = new SolidColorBrush(Colors.Gray);

    /// <summary>
    /// 颜色按钮点击命令
    /// </summary>
    [RelayCommand]
    private void SelectColor(string tag)
    {
        string currentColor = tag switch
        {
            "Word" => WordColor,
            "Phonetic" => PhoneticColor,
            "Definition" => DefinitionColor,
            "Example" => ExampleColor,
            _ => "#FFFFFF"
        };

        // 发送颜色选择消息
        _messenger.Send(new OpenColorPickerMessage(tag, currentColor, selectedColor =>
        {
            switch (tag)
            {
                case "Word":
                    WordColor = selectedColor;
                    break;
                case "Phonetic":
                    PhoneticColor = selectedColor;
                    break;
                case "Definition":
                    DefinitionColor = selectedColor;
                    break;
                case "Example":
                    ExampleColor = selectedColor;
                    break;
            }
            UpdateColorButtons();
        }));
    }

    /// <summary>
    /// 更新颜色按钮显示
    /// </summary>
    private void UpdateColorButtons()
    {
        WordColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(WordColor));
        PhoneticColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(PhoneticColor));
        DefinitionColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(DefinitionColor));
        ExampleColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ExampleColor));
    }

    // ==================== 快捷键相关命令 ====================

    /// <summary>
    /// 检查快捷键是否与其他设置冲突
    /// </summary>
    private HotKeyAction? CheckHotKeyConflict(HotKey hotKey, HotKeyAction currentAction)
    {
        if (!hotKey.Enabled || hotKey.Key == 0)
            return null;

        // 检查与其他动作的快捷键冲突
        var hotKeys = new Dictionary<HotKeyAction, HotKey>
        {
            { HotKeyAction.Previous, PreviousHotKey },
            { HotKeyAction.Next, NextHotKey },
            { HotKeyAction.PlayPause, PlayPauseHotKey },
            { HotKeyAction.Translation, TranslationHotKey },
            { HotKeyAction.BringToFront, BringToFrontHotKey }
        };

        foreach (var kvp in hotKeys)
        {
            if (kvp.Key == currentAction)
                continue;

            var other = kvp.Value;
            if (other.Enabled && other.Key == hotKey.Key && other.Modifiers == hotKey.Modifiers)
            {
                return kvp.Key;
            }
        }

        return null;
    }

    /// <summary>
    /// 清除快捷键
    /// </summary>
    [RelayCommand]
    private void ClearHotKey(string action)
    {
        switch (action)
        {
            case "Previous":
                PreviousHotKey = new HotKey { Enabled = false };
                break;
            case "Next":
                NextHotKey = new HotKey { Enabled = false };
                break;
            case "PlayPause":
                PlayPauseHotKey = new HotKey { Enabled = false };
                break;
            case "Translation":
                TranslationHotKey = new HotKey { Enabled = false };
                break;
            case "BringToFront":
                BringToFrontHotKey = new HotKey { Enabled = false };
                break;
        }
    }
}
