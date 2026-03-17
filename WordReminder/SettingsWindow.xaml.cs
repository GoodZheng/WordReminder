using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using WordReminder.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace WordReminder;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly DatabaseService _databaseService;
    private readonly UpdateService _updateService;
    private UpdateInfo? _availableUpdate;

    public event EventHandler? SettingsChanged;

    // 当前颜色值
    private string _wordColor = "#FFFFFF";
    private string _phoneticColor = "#CCCCCC";
    private string _definitionColor = "#EEEEEE";
    private string _exampleColor = "#AAAAAA";

    public SettingsWindow(ConfigService configService, DatabaseService databaseService)
    {
        InitializeComponent();
        _configService = configService;
        _databaseService = databaseService;
        // GitHub 仓库信息
        _updateService = new UpdateService("GoodZheng", "WordReminder");

        Loaded += (_, _) => LoadSettings();

        // 绑定滑块值变化事件
        IntervalSlider.ValueChanged += (_, _) => IntervalValue.Text = $"{(int)IntervalSlider.Value}秒";
        OpacitySlider.ValueChanged += (_, _) => OpacityValue.Text = $"{(int)(OpacitySlider.Value * 100)}%";

        WordFontSizeSlider.ValueChanged += (_, _) => WordFontSizeValue.Text = $"{(int)WordFontSizeSlider.Value}";
        PhoneticFontSizeSlider.ValueChanged += (_, _) => PhoneticFontSizeValue.Text = $"{(int)PhoneticFontSizeSlider.Value}";
        DefinitionFontSizeSlider.ValueChanged += (_, _) => DefinitionFontSizeValue.Text = $"{(int)DefinitionFontSizeSlider.Value}";
        ExampleFontSizeSlider.ValueChanged += (_, _) => ExampleFontSizeValue.Text = $"{(int)ExampleFontSizeSlider.Value}";
    }

    private void LoadSettings()
    {
        var settings = _configService.Settings;

        // 窗口设置
        IntervalSlider.Value = settings.IntervalSeconds;
        IntervalValue.Text = $"{settings.IntervalSeconds}秒";

        OpacitySlider.Value = settings.Opacity;
        OpacityValue.Text = $"{(int)(settings.Opacity * 100)}%";

        AlwaysOnTopCheckBox.IsChecked = settings.AlwaysOnTop;
        AutoStartCheckBox.IsChecked = settings.AutoStart;

        // 自动更新
        AutoUpdateCheckBox.IsChecked = settings.AutoUpdate;

        // 显示开关
        ShowPhoneticCheckBox.IsChecked = settings.ShowPhonetic;
        ShowDefinitionCheckBox.IsChecked = settings.ShowDefinition;
        ShowExampleCheckBox.IsChecked = settings.ShowExample;

        // 字体大小设置
        WordFontSizeSlider.Value = settings.WordFontSize;
        WordFontSizeValue.Text = settings.WordFontSize.ToString();

        PhoneticFontSizeSlider.Value = settings.PhoneticFontSize;
        PhoneticFontSizeValue.Text = settings.PhoneticFontSize.ToString();

        DefinitionFontSizeSlider.Value = settings.DefinitionFontSize;
        DefinitionFontSizeValue.Text = settings.DefinitionFontSize.ToString();

        ExampleFontSizeSlider.Value = settings.ExampleFontSize;
        ExampleFontSizeValue.Text = settings.ExampleFontSize.ToString();

        // 颜色设置
        _wordColor = settings.WordFontColor;
        _phoneticColor = settings.PhoneticFontColor;
        _definitionColor = settings.DefinitionFontColor;
        _exampleColor = settings.ExampleFontColor;

        UpdateColorButtons();

        // 显示单词数
        var words = _databaseService.GetAllWords();
        WordCountText.Text = $"当前单词数: {words.Count}";

        // 版本信息
        LoadVersionInfo();
    }

    private void UpdateColorButtons()
    {
        WordColorButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_wordColor));
        PhoneticColorButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_phoneticColor));
        DefinitionColorButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_definitionColor));
        ExampleColorButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_exampleColor));
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string tag)
        {
            string currentColor = tag switch
            {
                "Word" => _wordColor,
                "Phonetic" => _phoneticColor,
                "Definition" => _definitionColor,
                "Example" => _exampleColor,
                _ => "#FFFFFF"
            };

            var colorPicker = new ColorPickerWindow(currentColor);
            colorPicker.Owner = this;

            if (colorPicker.ShowDialog() == true)
            {
                switch (tag)
                {
                    case "Word":
                        _wordColor = colorPicker.SelectedColor;
                        break;
                    case "Phonetic":
                        _phoneticColor = colorPicker.SelectedColor;
                        break;
                    case "Definition":
                        _definitionColor = colorPicker.SelectedColor;
                        break;
                    case "Example":
                        _exampleColor = colorPicker.SelectedColor;
                        break;
                }
                UpdateColorButtons();
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _configService.UpdateSettings(s =>
        {
            // 窗口设置
            s.IntervalSeconds = (int)IntervalSlider.Value;
            s.Opacity = OpacitySlider.Value;
            s.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked ?? true;
            s.AutoStart = AutoStartCheckBox.IsChecked ?? false;
            s.AutoUpdate = AutoUpdateCheckBox.IsChecked ?? true;

            // 显示开关
            s.ShowPhonetic = ShowPhoneticCheckBox.IsChecked ?? true;
            s.ShowDefinition = ShowDefinitionCheckBox.IsChecked ?? true;
            s.ShowExample = ShowExampleCheckBox.IsChecked ?? true;

            // 字体大小设置
            s.WordFontSize = (int)WordFontSizeSlider.Value;
            s.PhoneticFontSize = (int)PhoneticFontSizeSlider.Value;
            s.DefinitionFontSize = (int)DefinitionFontSizeSlider.Value;
            s.ExampleFontSize = (int)ExampleFontSizeSlider.Value;

            // 颜色设置
            s.WordFontColor = _wordColor;
            s.PhoneticFontColor = _phoneticColor;
            s.DefinitionFontColor = _definitionColor;
            s.ExampleFontColor = _exampleColor;
        });

        // 应用开机自启设置
        SetAutoStart(AutoStartCheckBox.IsChecked ?? false);

        SettingsChanged?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private async void ReloadWords_Click(object sender, RoutedEventArgs e)
    {
        ReloadWordsButton.IsEnabled = false;
        ReloadWordsButton.Content = "正在加载...";

        try
        {
            // 清空现有数据
            _databaseService.ClearAllWords();

            // 预置单词
            var defaultWords = new[]
            {
                "ability"
            };

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
            WordCountText.Text = $"当前单词数: {words.Count}";

            SettingsChanged?.Invoke(this, EventArgs.Empty);

            System.Windows.MessageBox.Show("单词数据已重新加载！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ReloadWordsButton.IsEnabled = true;
            ReloadWordsButton.Content = "重新加载单词数据";
        }
    }

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
                    // 获取当前可执行文件路径
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    // 删除注册表项
                    key.DeleteValue(appName, false);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"开机自启设置失败：{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void LoadVersionInfo()
    {
        CurrentVersionText.Text = UpdateService.GetCurrentVersionString();
        LatestVersionText.Text = "点击检查更新";
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        CheckUpdateButton.Content = "检查中...";

        try
        {
            var updateInfo = await _updateService.CheckForUpdateAsync();

            if (updateInfo != null)
            {
                _availableUpdate = updateInfo;
                LatestVersionText.Text = updateInfo.VersionString;
                LatestVersionText.Foreground = new SolidColorBrush(Colors.Orange);

                CheckUpdateButton.Content = "发现新版本";

                // 显示下载按钮
                DownloadUpdateButton.Visibility = Visibility.Visible;
                DownloadUpdateButton.IsEnabled = true;

                System.Windows.MessageBox.Show(
                    $"发现新版本 {updateInfo.VersionString}！\n\n{updateInfo.ReleaseNotes}",
                    "发现新版本",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                LatestVersionText.Text = "已是最新版本";
                LatestVersionText.Foreground = new SolidColorBrush(Colors.Green);

                CheckUpdateButton.Content = "检查更新";
                DownloadUpdateButton.Visibility = Visibility.Collapsed;

                System.Windows.MessageBox.Show(
                    "当前已是最新版本！",
                    "检查更新",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            LatestVersionText.Text = "检查失败";
            LatestVersionText.Foreground = new SolidColorBrush(Colors.Red);

            System.Windows.MessageBox.Show(
                $"检查更新失败：{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
        }
    }

    private async void DownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_availableUpdate == null)
        {
            System.Windows.MessageBox.Show("请先检查更新！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DownloadUpdateButton.IsEnabled = false;
        DownloadProgressPanel.Visibility = Visibility.Visible;

        try
        {
            var downloadUrl = await _updateService.GetDownloadUrlAsync();
            if (string.IsNullOrEmpty(downloadUrl))
            {
                System.Windows.MessageBox.Show("无法获取下载链接，请手动前往 GitHub 下载。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateService.OpenReleasesPage("GoodZheng", "WordReminder");
                return;
            }

            var tempPath = Path.GetTempPath();
            var fileName = downloadUrl.Split('/').Last();
            var destinationPath = Path.Combine(tempPath, fileName);

            DownloadStatusText.Text = "正在下载...";

            var progress = new Progress<double>(percent =>
            {
                DownloadProgressBar.Value = percent;
                DownloadStatusText.Text = $"正在下载... {percent:F0}%";
            });

            var success = await _updateService.DownloadUpdateAsync(downloadUrl, destinationPath, progress);

            if (success && File.Exists(destinationPath))
            {
                DownloadStatusText.Text = "下载完成！";

                var result = System.Windows.MessageBox.Show(
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
                DownloadStatusText.Text = "下载失败";
                System.Windows.MessageBox.Show("下载更新失败，请稍后重试或手动下载。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            DownloadStatusText.Text = "下载失败";
            System.Windows.MessageBox.Show($"下载更新失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            DownloadUpdateButton.IsEnabled = true;
            DownloadProgressPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void OpenGitHubLink_MouseDown(object sender, MouseButtonEventArgs e)
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
}
