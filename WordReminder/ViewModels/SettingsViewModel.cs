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

    // AI 配置
    [ObservableProperty]
    private string _apiUrl = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

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
    private bool _isCheckUpdateEnabled = true;

    [ObservableProperty]
    private string _checkUpdateButtonText = "检查更新";

    [ObservableProperty]
    private bool _isDownloadUpdateEnabled;

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
    private HotKey _toggleTopmostHotKey = new();

    public SettingsViewModel(
        ConfigService configService,
        DatabaseService databaseService,
        UpdateService updateService,
        IMessenger messenger,
        HotKeyService hotKeyService)
    {
        _configService = configService;
        _databaseService = databaseService;
        _updateService = updateService;
        _messenger = messenger;
        _hotKeyService = hotKeyService;

        LoadSettings();
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

        // AI 配置
        if (settings.AIDictionary != null)
        {
            ApiUrl = settings.AIDictionary.ApiUrl ?? "";
            Model = settings.AIDictionary.Model ?? "";
            ApiKey = settings.AIDictionary.ApiKey ?? "";
        }

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
            ToggleTopmostHotKey = settings.HotKeys.ToggleTopmost?.Clone() ?? new HotKey();
        }
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

            // AI 配置
            if (s.AIDictionary == null)
            {
                s.AIDictionary = new AIDictionarySettings();
            }
            s.AIDictionary.ApiUrl = ApiUrl;
            s.AIDictionary.Model = Model;
            s.AIDictionary.ApiKey = ApiKey;

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
            s.HotKeys.ToggleTopmost = ToggleTopmostHotKey.Clone();
        });

        // 应用开机自启设置
        SetAutoStart(AutoStart);

        // 发送设置更改消息
        _messenger.Send(new SettingsChangedMessage());

        // 关闭设置窗口
        _messenger.Send(new CloseSettingsMessage());
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
            var bingService = new BingDictionaryService();

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
    /// 检查更新命令
    /// </summary>
    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        IsCheckUpdateEnabled = false;
        CheckUpdateButtonText = "检查中...";

        try
        {
            var updateInfo = await _updateService.CheckForUpdateAsync();

            if (updateInfo != null)
            {
                _availableUpdate = updateInfo;
                LatestVersionText = updateInfo.VersionString;
                LatestVersionForegroundColor = new SolidColorBrush(Colors.Orange);

                CheckUpdateButtonText = "发现新版本";
                IsDownloadUpdateEnabled = true;

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

                CheckUpdateButtonText = "检查更新";
                IsDownloadUpdateEnabled = false;

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

            MessageBox.Show(
                $"检查更新失败：{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsCheckUpdateEnabled = true;
        }
    }

    /// <summary>
    /// 下载更新命令
    /// </summary>
    [RelayCommand]
    private async Task DownloadUpdateAsync()
    {
        if (_availableUpdate == null)
        {
            MessageBox.Show("请先检查更新！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsDownloadUpdateEnabled = false;
        ShowDownloadProgress = true;

        try
        {
            var downloadUrl = await _updateService.GetDownloadUrlAsync();
            if (string.IsNullOrEmpty(downloadUrl))
            {
                MessageBox.Show("无法获取下载链接，请手动前往 GitHub 下载。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateService.OpenReleasesPage("GoodZheng", "WordReminder");
                return;
            }

            var tempPath = Path.GetTempPath();
            var fileName = downloadUrl.Split('/').Last();
            var destinationPath = Path.Combine(tempPath, fileName);

            DownloadStatusText = "正在下载...";

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
            }
            else
            {
                DownloadStatusText = "下载失败";
                MessageBox.Show("下载更新失败，请稍后重试或手动下载。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            DownloadStatusText = "下载失败";
            MessageBox.Show($"下载更新失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsDownloadUpdateEnabled = true;
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
            { HotKeyAction.ToggleTopmost, ToggleTopmostHotKey }
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
            case "ToggleTopmost":
                ToggleTopmostHotKey = new HotKey { Enabled = false };
                break;
        }
    }
}
