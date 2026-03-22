using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using WordReminder.Messages;
using WordReminder.Models;
using WordReminder.Services;
using WordReminder.Views;
using Screen = System.Windows.Forms.Screen;
using MessageBox = System.Windows.MessageBox;

namespace WordReminder.ViewModels;

/// <summary>
/// 主窗口 ViewModel - 管理单词展示和用户交互
/// </summary>
public partial class MainViewModel : ViewModelBase,
    IRecipient<SettingsChangedMessage>,
    IRecipient<WordsChangedMessage>
{
    private readonly DatabaseService _databaseService;
    private readonly ConfigService _configService;
    private readonly AIDictionaryService _aiService;
    private readonly IMessenger _messenger;
    private readonly IServiceProvider _serviceProvider;
    private readonly HotKeyService _hotKeyService;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _savePositionTimer;

    [ObservableProperty]
    private List<Word> _words = new();

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private bool _isPlaying = true;

    [ObservableProperty]
    private bool _isInitializing;

    [ObservableProperty]
    private string _wordText = "Loading...";

    [ObservableProperty]
    private string _phoneticText = string.Empty;

    [ObservableProperty]
    private string _definitionText = string.Empty;

    [ObservableProperty]
    private string _exampleText = string.Empty;

    [ObservableProperty]
    private bool _showPhonetic;

    [ObservableProperty]
    private bool _showDefinition;

    [ObservableProperty]
    private bool _showExample;

    // 字体和颜色属性
    [ObservableProperty]
    private int _wordFontSize = 32;

    [ObservableProperty]
    private string _wordFontFamily = "Segoe UI";

    [ObservableProperty]
    private string _wordFontColor = "#FFFFFF";

    [ObservableProperty]
    private int _phoneticFontSize = 16;

    [ObservableProperty]
    private string _phoneticFontFamily = "Segoe UI";

    [ObservableProperty]
    private string _phoneticFontColor = "#CCCCCC";

    [ObservableProperty]
    private int _definitionFontSize = 18;

    [ObservableProperty]
    private string _definitionFontFamily = "Segoe UI";

    [ObservableProperty]
    private string _definitionFontColor = "#EEEEEE";

    [ObservableProperty]
    private int _exampleFontSize = 14;

    [ObservableProperty]
    private string _exampleFontFamily = "Segoe UI";

    [ObservableProperty]
    private string _exampleFontColor = "#AAAAAA";

    // 窗口属性
    [ObservableProperty]
    private double _windowLeft;

    [ObservableProperty]
    private double _windowTop;

    [ObservableProperty]
    private double _windowWidth = 400;

    [ObservableProperty]
    private double _windowHeight = 200;

    [ObservableProperty]
    private bool _alwaysOnTop = true;

    [ObservableProperty]
    private double _windowOpacity = 1.0;

    // 预置单词列表
    private readonly string[] _defaultWords = new[] { "ability" };

    public MainViewModel(
        DatabaseService databaseService,
        ConfigService configService,
        AIDictionaryService aiService,
        IMessenger messenger,
        IServiceProvider serviceProvider,
        HotKeyService hotKeyService)
    {
        _databaseService = databaseService;
        _configService = configService;
        _aiService = aiService;
        _messenger = messenger;
        _serviceProvider = serviceProvider;
        _hotKeyService = hotKeyService;

        // 注册消息接收
        _messenger.Register<SettingsChangedMessage>(this);
        _messenger.Register<WordsChangedMessage>(this);

        // 订阅快捷键事件
        _hotKeyService.HotKeyPressed += OnHotKeyPressed;

        // 初始化定时器
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_configService.Settings.IntervalSeconds)
        };
        _timer.Tick += (_, _) => NextWord();

        // 位置保存防抖定时器
        _savePositionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _savePositionTimer.Tick += OnSavePositionTimerTick;

        // 初始化
        InitializeAsync();
    }

    /// <summary>
    /// 异步初始化
    /// </summary>
    private async void InitializeAsync()
    {
        // 应用配置
        ApplySettings();

        // 检查并初始化默认单词
        await InitializeDefaultWordsAsync();

        // 加载单词
        Words = _databaseService.GetAllWords();

        // 如果没有单词，显示提示
        if (Words.Count == 0)
        {
            WordText = "No words found";
            return;
        }

        // 显示第一个单词
        ShowCurrentWord();

        // 启动定时器
        StartTimer();
    }

    /// <summary>
    /// 初始化默认单词
    /// </summary>
    private async Task InitializeDefaultWordsAsync()
    {
        // 只在首次启动时初始化默认单词
        if (_configService.Settings.DefaultWordsInitialized)
        {
            return;
        }

        // 检查 AI 是否可用
        var aiConfig = _configService.Settings.AIDictionary;
        bool useAI = aiConfig.Enabled && !string.IsNullOrEmpty(aiConfig.ApiKey) && aiConfig.ApiKey != "your-api-key-here";

        if (useAI)
        {
            // 使用 AI 获取完整单词信息
            foreach (var wordText in _defaultWords)
            {
                try
                {
                    var word = await _aiService.GetWordInfoAsync(wordText);
                    if (word != null && !string.IsNullOrEmpty(word.Definition))
                    {
                        _databaseService.InsertWord(word);
                    }
                    else
                    {
                        // AI 失败，使用预置数据
                        var fallbackWord = DefaultWordData.GetWord(wordText);
                        _databaseService.InsertWord(fallbackWord);
                    }
                }
                catch
                {
                    // 出错，使用预置数据
                    var fallbackWord = DefaultWordData.GetWord(wordText);
                    _databaseService.InsertWord(fallbackWord);
                }
            }
        }
        else
        {
            // AI 未配置，使用预置的单词数据
            foreach (var wordText in _defaultWords)
            {
                var word = DefaultWordData.GetWord(wordText);
                _databaseService.InsertWord(word);
            }
        }

        // 标记已初始化
        _configService.UpdateSettings(s => s.DefaultWordsInitialized = true);
    }

    /// <summary>
    /// 应用配置设置
    /// </summary>
    private void ApplySettings()
    {
        IsInitializing = true;
        try
        {
            var settings = _configService.Settings;

            // 检查窗口位置是否在有效屏幕范围内
            var savedRect = new Rect(settings.WindowPositionX, settings.WindowPositionY,
                                      settings.WindowWidth, settings.WindowHeight);

            bool isOnScreen = false;
            foreach (var screen in Screen.AllScreens)
            {
                var screenRect = new Rect(screen.WorkingArea.Left, screen.WorkingArea.Top,
                                           screen.WorkingArea.Width, screen.WorkingArea.Height);
                if (screenRect.IntersectsWith(savedRect))
                {
                    isOnScreen = true;
                    break;
                }
            }

            if (isOnScreen)
            {
                WindowLeft = settings.WindowPositionX;
                WindowTop = settings.WindowPositionY;
                WindowWidth = settings.WindowWidth;
                WindowHeight = settings.WindowHeight;
            }
            else
            {
                // 窗口不在任何屏幕内，重置到主屏幕中央
                var primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen != null)
                {
                    WindowWidth = settings.WindowWidth;
                    WindowHeight = settings.WindowHeight;
                    WindowLeft = (primaryScreen.WorkingArea.Width - WindowWidth) / 2 + primaryScreen.WorkingArea.Left;
                    WindowTop = (primaryScreen.WorkingArea.Height - WindowHeight) / 2 + primaryScreen.WorkingArea.Top;
                }
                else
                {
                    WindowLeft = 100;
                    WindowTop = 100;
                    WindowWidth = settings.WindowWidth;
                    WindowHeight = settings.WindowHeight;
                }
            }

            AlwaysOnTop = settings.AlwaysOnTop;
            WindowOpacity = settings.Opacity;

            // 单词字体设置
            WordFontSize = settings.WordFontSize;
            WordFontFamily = settings.WordFontFamily;
            WordFontColor = settings.WordFontColor;

            // 音标字体设置
            PhoneticFontSize = settings.PhoneticFontSize;
            PhoneticFontFamily = settings.PhoneticFontFamily;
            PhoneticFontColor = settings.PhoneticFontColor;

            // 释义字体设置
            DefinitionFontSize = settings.DefinitionFontSize;
            DefinitionFontFamily = settings.DefinitionFontFamily;
            DefinitionFontColor = settings.DefinitionFontColor;

            // 例句字体设置
            ExampleFontSize = settings.ExampleFontSize;
            ExampleFontFamily = settings.ExampleFontFamily;
            ExampleFontColor = settings.ExampleFontColor;

            // 显示开关
            ShowPhonetic = settings.ShowPhonetic;
            ShowDefinition = settings.ShowDefinition;
            ShowExample = settings.ShowExample;
        }
        finally
        {
            IsInitializing = false;
        }
    }

    /// <summary>
    /// 启动定时器
    /// </summary>
    private void StartTimer()
    {
        _timer.Stop();
        _timer.Interval = TimeSpan.FromSeconds(_configService.Settings.IntervalSeconds);
        _timer.Start();
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    private void StopTimer()
    {
        _timer.Stop();
    }

    /// <summary>
    /// 显示当前单词
    /// </summary>
    private void ShowCurrentWord()
    {
        if (Words.Count == 0 || CurrentIndex >= Words.Count) return;

        var word = Words[CurrentIndex];
        var settings = _configService.Settings;

        WordText = word.Text;

        // 显示音标
        if (settings.ShowPhonetic && !string.IsNullOrEmpty(word.Phonetic))
        {
            PhoneticText = word.Phonetic;
            ShowPhonetic = true;
        }
        else
        {
            ShowPhonetic = false;
        }

        // 显示释义
        if (settings.ShowDefinition && !string.IsNullOrEmpty(word.Definition))
        {
            var partOfSpeech = string.IsNullOrEmpty(word.PartOfSpeech) ? "" : $"[{word.PartOfSpeech}] ";
            DefinitionText = partOfSpeech + word.Definition;
            ShowDefinition = true;
        }
        else
        {
            ShowDefinition = false;
        }

        // 显示例句
        if (settings.ShowExample && !string.IsNullOrEmpty(word.Example))
        {
            ExampleText = word.Example;
            ShowExample = true;
        }
        else
        {
            ShowExample = false;
        }
    }

    // ==================== Commands ====================

    /// <summary>
    /// 下一个单词命令
    /// </summary>
    [RelayCommand]
    private void NextWord()
    {
        if (Words.Count == 0) return;
        CurrentIndex = (CurrentIndex + 1) % Words.Count;
        ShowCurrentWord();
    }

    /// <summary>
    /// 上一个单词命令
    /// </summary>
    [RelayCommand]
    private void PreviousWord()
    {
        if (Words.Count == 0) return;
        CurrentIndex = (CurrentIndex - 1 + Words.Count) % Words.Count;
        ShowCurrentWord();
    }

    /// <summary>
    /// 播放/暂停命令
    /// </summary>
    [RelayCommand]
    private void PlayPause()
    {
        IsPlaying = !IsPlaying;
        if (IsPlaying)
        {
            StartTimer();
        }
        else
        {
            StopTimer();
        }
    }

    /// <summary>
    /// 打开设置命令
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        var viewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
        var window = new SettingsWindow
        {
            DataContext = viewModel
        };
        window.ShowDialog();

        // 设置窗口关闭后刷新数据
        Words = _databaseService.GetAllWords();
        if (CurrentIndex >= Words.Count) CurrentIndex = 0;
        ShowCurrentWord();
    }

    /// <summary>
    /// 添加单词命令
    /// </summary>
    [RelayCommand]
    private async Task AddWordAsync()
    {
        var viewModel = _serviceProvider.GetRequiredService<AddWordViewModel>();
        var window = new AddWordWindow
        {
            DataContext = viewModel
        };

        if (window.ShowDialog() == true)
        {
            // 处理添加单词的逻辑
            var wordText = viewModel.WordText;
            if (!string.IsNullOrWhiteSpace(wordText))
            {
                // 检查单词是否已存在
                if (_databaseService.WordExists(wordText))
                {
                    MessageBox.Show($"单词 '{wordText}' 已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 显示正在查询的提示
                WordText = $"正在查询 '{wordText}'...";

                try
                {
                    // 从 AI 词典查询单词信息
                    var word = await _aiService.GetWordInfoAsync(wordText);

                    if (word != null && !string.IsNullOrEmpty(word.Definition))
                    {
                        // 保存到数据库
                        _databaseService.InsertWord(word);

                        // 重新加载单词列表
                        Words = _databaseService.GetAllWords();

                        // 跳转到新添加的单词
                        CurrentIndex = Words.FindIndex(w => w.Text.Equals(wordText, StringComparison.OrdinalIgnoreCase));
                        if (CurrentIndex < 0) CurrentIndex = Words.Count - 1;

                        ShowCurrentWord();

                        MessageBox.Show($"已添加单词: {word.Text}\n{word.PartOfSpeech} {word.Definition}",
                            "添加成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ShowCurrentWord();
                        MessageBox.Show($"未找到单词 '{wordText}' 的释义，请检查拼写是否正确",
                            "查询失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    ShowCurrentWord();
                    MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    /// <summary>
    /// 所有单词命令
    /// </summary>
    [RelayCommand]
    private void ShowAllWords()
    {
        var viewModel = _serviceProvider.GetRequiredService<AllWordsViewModel>();
        var window = new AllWordsWindow
        {
            DataContext = viewModel
        };
        window.ShowDialog();

        // 窗口关闭后刷新数据
        Words = _databaseService.GetAllWords();
        if (CurrentIndex >= Words.Count) CurrentIndex = 0;
        ShowCurrentWord();
    }

    /// <summary>
    /// 翻译命令
    /// </summary>
    [RelayCommand]
    private void OpenTranslation()
    {
        var viewModel = _serviceProvider.GetRequiredService<TranslationViewModel>();
        var window = new TranslationWindow(viewModel);
        window.ShowDialog();
    }

    /// <summary>
    /// 退出命令
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        _messenger.Send(new ExitApplicationMessage());
    }

    /// <summary>
    /// 窗口大小改变时保存位置
    /// </summary>
    [RelayCommand]
    private void WindowSizeChanged()
    {
        SaveWindowPosition();
    }

    /// <summary>
    /// 窗口位置改变时保存位置
    /// </summary>
    [RelayCommand]
    private void WindowLocationChanged()
    {
        SaveWindowPosition();
    }

    /// <summary>
    /// 窗口关闭时保存位置
    /// </summary>
    [RelayCommand]
    private void WindowClosing()
    {
        SaveWindowPositionImmediate();
    }

    /// <summary>
    /// 保存窗口位置（防抖）
    /// </summary>
    private void SaveWindowPosition()
    {
        if (IsInitializing) return;
        _savePositionTimer.Stop();
        _savePositionTimer.Start();
    }

    /// <summary>
    /// 防抖定时器触发
    /// </summary>
    private void OnSavePositionTimerTick(object? sender, EventArgs e)
    {
        _savePositionTimer.Stop();
        SaveWindowPositionImmediate();
    }

    /// <summary>
    /// 立即保存窗口位置
    /// </summary>
    private void SaveWindowPositionImmediate()
    {
        if (IsInitializing) return;

        // 检查值是否有效
        if (double.IsNaN(WindowLeft) || double.IsInfinity(WindowLeft) ||
            double.IsNaN(WindowTop) || double.IsInfinity(WindowTop) ||
            double.IsNaN(WindowWidth) || double.IsInfinity(WindowWidth) || WindowWidth <= 0 ||
            double.IsNaN(WindowHeight) || double.IsInfinity(WindowHeight) || WindowHeight <= 0)
            return;

        try
        {
            _configService.UpdateSettings(s =>
            {
                s.WindowPositionX = WindowLeft;
                s.WindowPositionY = WindowTop;
                s.WindowWidth = WindowWidth;
                s.WindowHeight = WindowHeight;
            });
        }
        catch
        {
            // 忽略保存错误
        }
    }

    // ==================== Message Handlers ====================

    /// <summary>
    /// 接收设置更改消息
    /// </summary>
    public void Receive(SettingsChangedMessage message)
    {
        ApplySettings();
        StartTimer();
        Words = _databaseService.GetAllWords();
        if (CurrentIndex >= Words.Count) CurrentIndex = 0;
        ShowCurrentWord();

        // 重新注册快捷键
        if (_configService.Settings.HotKeys != null)
        {
            _hotKeyService.RegisterAllHotKeys(_configService.Settings.HotKeys);
        }
    }

    /// <summary>
    /// 接收单词更改消息
    /// </summary>
    public void Receive(WordsChangedMessage message)
    {
        Words = _databaseService.GetAllWords();
        if (CurrentIndex >= Words.Count) CurrentIndex = 0;
        ShowCurrentWord();
    }

    /// <summary>
    /// 快捷键按下事件处理
    /// </summary>
    private void OnHotKeyPressed(object? sender, HotKeyAction action)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            switch (action)
            {
                case HotKeyAction.Previous:
                    PreviousWordCommand.Execute(null);
                    break;
                case HotKeyAction.Next:
                    NextWordCommand.Execute(null);
                    break;
                case HotKeyAction.PlayPause:
                    PlayPauseCommand.Execute(null);
                    break;
                case HotKeyAction.Translation:
                    OpenTranslationCommand.Execute(null);
                    break;
                case HotKeyAction.ToggleTopmost:
                    AlwaysOnTop = !AlwaysOnTop;
                    break;
            }
        });
    }
}
