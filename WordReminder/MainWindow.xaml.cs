using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WordReminder.Models;
using WordReminder.Services;
using System.Linq;
using System.Windows.Interop;
using Screen = System.Windows.Forms.Screen;

namespace WordReminder;

public partial class MainWindow : Window
{
    private readonly DatabaseService _databaseService;
    private readonly ConfigService _configService;
    private readonly AIDictionaryService _aiService;
    private List<Word> _words = new();
    private int _currentIndex = 0;
    private DispatcherTimer? _timer;
    private bool _isPlaying = true;
    private HwndSource? _hwndSource;

    // 预置单词列表
    private readonly string[] _defaultWords = new[]
    {
        "ability"
    };

    // 禁用 Windows 11 的 Snap Layouts
    private const int WM_NCHITTEST = 0x0084;
    private const int HTCLIENT = 1;

    public MainWindow()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _configService = new ConfigService();
        _aiService = new AIDictionaryService(_configService);

        // 禁用 Windows 11 的 Snap Layouts
        this.SourceInitialized += (s, e) =>
        {
            _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
            }
        };

        Loaded += async (_, _) => await InitializeAsync();
    }

    // 窗口过程处理，禁用 Snap Layouts
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            // 始终返回 HTCLIENT，禁用窗口边缘的布局功能
            handled = true;
            return new IntPtr(HTCLIENT);
        }
        return IntPtr.Zero;
    }

    private async Task InitializeAsync()
    {
        // 应用配置
        ApplySettings();

        // 检查并初始化默认单词
        await InitializeDefaultWordsAsync();

        // 加载单词
        _words = _databaseService.GetAllWords();

        // 如果没有单词，显示提示
        if (_words.Count == 0)
        {
            WordText.Text = "No words found";
            return;
        }

        // 显示第一个单词
        ShowCurrentWord();

        // 启动定时器
        StartTimer();
    }

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

    private void ApplySettings()
    {
        var settings = _configService.Settings;

        // 检查窗口位置是否在有效屏幕范围内（处理外接显示器断开的情况）
        var savedRect = new Rect(settings.WindowPositionX, settings.WindowPositionY,
                                  settings.WindowWidth, settings.WindowHeight);

        // 检查是否在任何屏幕内
        bool isOnScreen = false;
        foreach (var screen in Screen.AllScreens)
        {
            var screenRect = new Rect(screen.WorkingArea.Left, screen.WorkingArea.Top,
                                       screen.WorkingArea.Width, screen.WorkingArea.Height);
            // 只要窗口有一部分在屏幕内就算有效
            if (screenRect.IntersectsWith(savedRect))
            {
                isOnScreen = true;
                break;
            }
        }

        if (isOnScreen)
        {
            // 使用保存的位置
            Left = settings.WindowPositionX;
            Top = settings.WindowPositionY;
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
        }
        else
        {
            // 窗口不在任何屏幕内（外接显示器断开），重置到主屏幕中央
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
                Left = (primaryScreen.WorkingArea.Width - Width) / 2 + primaryScreen.WorkingArea.Left;
                Top = (primaryScreen.WorkingArea.Height - Height) / 2 + primaryScreen.WorkingArea.Top;
            }
            else
            {
                // 备用方案
                Left = 100;
                Top = 100;
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
            }
        }

        Topmost = settings.AlwaysOnTop;
        Opacity = settings.Opacity;

        // 单词字体设置
        WordText.FontSize = settings.WordFontSize;
        WordText.FontFamily = new System.Windows.Media.FontFamily(settings.WordFontFamily);
        WordText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.WordFontColor));

        // 音标字体设置
        PhoneticText.FontSize = settings.PhoneticFontSize;
        PhoneticText.FontFamily = new System.Windows.Media.FontFamily(settings.PhoneticFontFamily);
        PhoneticText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.PhoneticFontColor));

        // 释义字体设置
        DefinitionText.FontSize = settings.DefinitionFontSize;
        DefinitionText.FontFamily = new System.Windows.Media.FontFamily(settings.DefinitionFontFamily);
        DefinitionText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.DefinitionFontColor));

        // 例句字体设置
        ExampleText.FontSize = settings.ExampleFontSize;
        ExampleText.FontFamily = new System.Windows.Media.FontFamily(settings.ExampleFontFamily);
        ExampleText.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.ExampleFontColor));
    }

    private void StartTimer()
    {
        _timer?.Stop();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_configService.Settings.IntervalSeconds)
        };
        _timer.Tick += (_, _) => NextWord();
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
    }

    private void NextWord()
    {
        if (_words.Count == 0) return;

        _currentIndex = (_currentIndex + 1) % _words.Count;
        ShowCurrentWord();
    }

    private void PreviousWord()
    {
        if (_words.Count == 0) return;

        _currentIndex = (_currentIndex - 1 + _words.Count) % _words.Count;
        ShowCurrentWord();
    }

    private void ShowCurrentWord()
    {
        if (_words.Count == 0 || _currentIndex >= _words.Count) return;

        var word = _words[_currentIndex];
        var settings = _configService.Settings;

        // 调试输出
        System.Diagnostics.Debug.WriteLine($"显示单词: {word.Text}");
        System.Diagnostics.Debug.WriteLine($"音标: {word.Phonetic}");
        System.Diagnostics.Debug.WriteLine($"词性: {word.PartOfSpeech}");
        System.Diagnostics.Debug.WriteLine($"释义: {word.Definition}");

        WordText.Text = word.Text;

        // 显示音标
        if (settings.ShowPhonetic && !string.IsNullOrEmpty(word.Phonetic))
        {
            PhoneticText.Text = word.Phonetic;
            PhoneticText.Visibility = Visibility.Visible;
        }
        else
        {
            PhoneticText.Visibility = Visibility.Collapsed;
        }

        // 显示释义
        if (settings.ShowDefinition && !string.IsNullOrEmpty(word.Definition))
        {
            var partOfSpeech = string.IsNullOrEmpty(word.PartOfSpeech) ? "" : $"[{word.PartOfSpeech}] ";
            DefinitionText.Text = partOfSpeech + word.Definition;
            DefinitionText.Visibility = Visibility.Visible;
        }
        else
        {
            DefinitionText.Visibility = Visibility.Collapsed;
        }

        // 显示例句
        if (settings.ShowExample && !string.IsNullOrEmpty(word.Example))
        {
            ExampleText.Text = word.Example;
            ExampleText.Visibility = Visibility.Visible;
        }
        else
        {
            ExampleText.Visibility = Visibility.Collapsed;
        }
    }

    // 窗口拖动
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // 双击打开设置
            OpenSettings();
        }
        else
        {
            DragMove();
        }
    }

    // 右键菜单事件
    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        _isPlaying = !_isPlaying;
        if (_isPlaying)
        {
            StartTimer();
        }
        else
        {
            StopTimer();
        }
    }

    private void Previous_Click(object sender, RoutedEventArgs e)
    {
        PreviousWord();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        NextWord();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AllWords_Click(object sender, RoutedEventArgs e)
    {
        var allWordsWindow = new AllWordsWindow();
        allWordsWindow.WordsChanged += (_, _) =>
        {
            _words = _databaseService.GetAllWords();
            if (_currentIndex >= _words.Count) _currentIndex = 0;
            ShowCurrentWord();
        };
        allWordsWindow.ShowDialog();
    }

    private async void AddWord_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddWordWindow();
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.WordText))
        {
            var wordText = dialog.WordText.ToLower().Trim();

            // 检查单词是否已存在
            if (_databaseService.WordExists(wordText))
            {
                System.Windows.MessageBox.Show($"单词 '{wordText}' 已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 显示正在查询的提示
            WordText.Text = $"正在查询 '{wordText}'...";

            try
            {
                // 从 AI 词典查询单词信息
                var word = await _aiService.GetWordInfoAsync(wordText);

                if (word != null && !string.IsNullOrEmpty(word.Definition))
                {
                    // 保存到数据库
                    _databaseService.InsertWord(word);

                    // 重新加载单词列表
                    _words = _databaseService.GetAllWords();

                    // 跳转到新添加的单词
                    _currentIndex = _words.FindIndex(w => w.Text.Equals(wordText, StringComparison.OrdinalIgnoreCase));
                    if (_currentIndex < 0) _currentIndex = _words.Count - 1;

                    ShowCurrentWord();

                    System.Windows.MessageBox.Show($"已添加单词: {word.Text}\n{word.PartOfSpeech} {word.Definition}",
                        "添加成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ShowCurrentWord();
                    System.Windows.MessageBox.Show($"未找到单词 '{wordText}' 的释义，请检查拼写是否正确",
                        "查询失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowCurrentWord();
                System.Windows.MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_configService, _databaseService);
        settingsWindow.SettingsChanged += (_, _) =>
        {
            ApplySettings();
            StartTimer();
            _words = _databaseService.GetAllWords();
            if (_currentIndex >= _words.Count) _currentIndex = 0;
            ShowCurrentWord();
        };
        settingsWindow.ShowDialog();
    }

    private async void Translate_Click(object sender, RoutedEventArgs e)
    {
        if (_words.Count == 0 || _currentIndex >= _words.Count)
        {
            System.Windows.MessageBox.Show("当前没有可翻译的单词", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var currentWord = _words[_currentIndex];

        // 直接使用数据库中的释义
        if (!string.IsNullOrEmpty(currentWord.Definition))
        {
            var partOfSpeech = !string.IsNullOrEmpty(currentWord.PartOfSpeech) ? $"[{currentWord.PartOfSpeech}] " : "";
            System.Windows.MessageBox.Show($"{currentWord.Text}\n\n{partOfSpeech}{currentWord.Definition}", "翻译结果", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            System.Windows.MessageBox.Show("该单词暂无释义", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // 窗口关闭时保存位置
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
        SaveWindowPosition();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // 窗口大小变化时实时保存
        if (e.WidthChanged || e.HeightChanged)
        {
            SaveWindowPosition();
        }
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        // 位置变化时实时保存
        SaveWindowPosition();
    }

    private void SaveWindowPosition()
    {
        // 只有当窗口正常显示时才保存位置
        if (WindowState == WindowState.Normal)
        {
            // 检查值是否有效（非NaN、非Infinity）
            if (!double.IsNaN(Left) && !double.IsInfinity(Left) &&
                !double.IsNaN(Top) && !double.IsInfinity(Top) &&
                !double.IsNaN(Width) && !double.IsInfinity(Width) && Width > 0 &&
                !double.IsNaN(Height) && !double.IsInfinity(Height) && Height > 0)
            {
                _configService.UpdateSettings(s =>
                {
                    s.WindowPositionX = Left;
                    s.WindowPositionY = Top;
                    s.WindowWidth = Width;
                    s.WindowHeight = Height;
                });
            }
        }
    }
}
