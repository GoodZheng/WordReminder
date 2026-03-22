using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WordReminder.Services;

namespace WordReminder;

public partial class TranslationWindow : Window
{
    private readonly AITranslationService _translationService;
    private readonly ConfigService _configService;

    public TranslationWindow(ConfigService configService)
    {
        InitializeComponent();
        _configService = configService;
        _translationService = new AITranslationService(configService);

        // 设置焦点到输入框
        Loaded += (_, _) => InputTextBox.Focus();
    }

    private async void TranslateButton_Click(object sender, RoutedEventArgs e)
    {
        var text = InputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            System.Windows.MessageBox.Show("请输入要翻译的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 检查 AI 配置
        var aiConfig = _configService.Settings.AIDictionary;
        if (!aiConfig.Enabled || string.IsNullOrEmpty(aiConfig.ApiKey) || aiConfig.ApiKey == "your-api-key-here")
        {
            System.Windows.MessageBox.Show("AI 词典未配置，请先在设置中配置 API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 显示加载状态
        ShowLoading();

        try
        {
            StatusText.Text = "正在翻译...";
            var result = await _translationService.TranslateAsync(text);
            DisplayResult(result);
            StatusText.Text = "翻译完成";
        }
        catch (Exception ex)
        {
            ShowError($"翻译失败: {ex.Message}");
            StatusText.Text = "翻译失败";
        }
    }

    private void ShowLoading()
    {
        ResultPanel.Children.Clear();
        var loadingText = new TextBlock
        {
            Text = "正在翻译，请稍候...",
            Foreground = new SolidColorBrush(Colors.Gray),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            FontSize = 14
        };
        ResultPanel.Children.Add(loadingText);
    }

    private void ShowError(string message)
    {
        ResultPanel.Children.Clear();
        var errorBlock = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(ColorHelper.FromRgb(220, 53, 69)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };
        ResultPanel.Children.Add(errorBlock);
    }

    private void DisplayResult(AITranslationService.TranslationResult? result)
    {
        ResultPanel.Children.Clear();

        if (result == null)
        {
            ShowError("翻译结果为空");
            return;
        }

        // 错误情况
        if (result.Type == "error")
        {
            ShowError(result.TranslatedText ?? "未知错误");
            return;
        }

        // 原文
        if (!string.IsNullOrEmpty(result.Text))
        {
            var originalHeader = CreateHeaderTextBlock("原文");
            ResultPanel.Children.Add(originalHeader);

            var originalText = new TextBlock
            {
                Text = result.Text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush(ColorHelper.FromRgb(50, 50, 50))
            };
            ResultPanel.Children.Add(originalText);
        }

        // 英文单词翻译为中文
        if (result.Direction == "en2zh" && result.Type == "word")
        {
            // 译文（单词释义）
            if (!string.IsNullOrEmpty(result.TranslatedText))
            {
                var translationHeader = CreateHeaderTextBlock("译文");
                ResultPanel.Children.Add(translationHeader);

                var translationText = new TextBlock
                {
                    Text = result.TranslatedText,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = new SolidColorBrush(ColorHelper.FromRgb(0, 123, 255))
                };
                ResultPanel.Children.Add(translationText);
            }

            // 单词详情
            if (result.WordDetails != null && result.WordDetails.Count > 0)
            {
                foreach (var word in result.WordDetails)
                {
                    AddWordDetail(word);
                }
            }
        }
        // 英文句子翻译为中文
        else if (result.Direction == "en2zh" && result.Type == "sentence")
        {
            // 译文
            if (!string.IsNullOrEmpty(result.TranslatedText))
            {
                var translationHeader = CreateHeaderTextBlock("译文");
                ResultPanel.Children.Add(translationHeader);

                var translationText = new TextBlock
                {
                    Text = result.TranslatedText,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 15,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = new SolidColorBrush(ColorHelper.FromRgb(0, 123, 255))
                };
                ResultPanel.Children.Add(translationText);
            }

            // 难词解释
            if (result.WordDetails != null && result.WordDetails.Count > 0)
            {
                var wordsHeader = CreateHeaderTextBlock("难词解释");
                ResultPanel.Children.Add(wordsHeader);

                foreach (var word in result.WordDetails)
                {
                    AddWordDetail(word);
                }
            }
        }
        // 中文句子翻译为英文
        else if (result.Direction == "zh2en")
        {
            var optionsHeader = CreateHeaderTextBlock("多种翻译");
            ResultPanel.Children.Add(optionsHeader);

            if (result.Options != null && result.Options.Count > 0)
            {
                for (int i = 0; i < result.Options.Count; i++)
                {
                    var option = result.Options[i];

                    var optionBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush(ColorHelper.FromRgb(220, 220, 220)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 12),
                        Background = new SolidColorBrush(ColorHelper.FromRgb(250, 250, 250))
                    };

                    var optionPanel = new StackPanel();

                    // 序号 + 译文
                    var optionText = new TextBlock
                    {
                        Text = $"{i + 1}. {option.Text}",
                        FontSize = 15,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 0, 0, 5),
                        Foreground = new SolidColorBrush(ColorHelper.FromRgb(0, 123, 255))
                    };
                    optionPanel.Children.Add(optionText);

                    // 适用场景
                    if (!string.IsNullOrEmpty(option.Scenario))
                    {
                        var scenarioText = new TextBlock
                        {
                            Text = $"场景: {option.Scenario}",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(ColorHelper.FromRgb(120, 120, 120)),
                            FontStyle = FontStyles.Italic
                        };
                        optionPanel.Children.Add(scenarioText);
                    }

                    optionBorder.Child = optionPanel;
                    ResultPanel.Children.Add(optionBorder);
                }
            }
        }
    }

    private TextBlock CreateHeaderTextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 15, 0, 8),
            Foreground = new SolidColorBrush(ColorHelper.FromRgb(100, 100, 100))
        };
    }

    private void AddWordDetail(AITranslationService.WordInfo word)
    {
        var wordBorder = new Border
        {
            BorderBrush = new SolidColorBrush(ColorHelper.FromRgb(220, 220, 220)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            Background = new SolidColorBrush(Colors.White)
        };

        var wordPanel = new StackPanel();

        // 单词 + 音标
        var wordHeader = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal
        };

        var wordText = new TextBlock
        {
            Text = word.Word ?? "",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(ColorHelper.FromRgb(0, 123, 255))
        };
        wordHeader.Children.Add(wordText);

        if (!string.IsNullOrEmpty(word.Phonetic))
        {
            var phoneticText = new TextBlock
            {
                Text = $" {word.Phonetic}",
                FontSize = 14,
                Foreground = new SolidColorBrush(ColorHelper.FromRgb(120, 120, 120)),
                Margin = new Thickness(8, 0, 0, 0)
            };
            wordHeader.Children.Add(phoneticText);
        }

        wordPanel.Children.Add(wordHeader);

        // 词性 + 释义
        if (!string.IsNullOrEmpty(word.PartOfSpeech) || !string.IsNullOrEmpty(word.Definition))
        {
            var pos = !string.IsNullOrEmpty(word.PartOfSpeech) ? $"[{word.PartOfSpeech}] " : "";
            var definitionText = new TextBlock
            {
                Text = $"{pos}{word.Definition}",
                FontSize = 14,
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            wordPanel.Children.Add(definitionText);
        }

        // 例句
        if (!string.IsNullOrEmpty(word.Example))
        {
            var examplePanel = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            // 英文例句
            var exampleText = new TextBlock
            {
                Text = $"例句: {word.Example}",
                FontSize = 12,
                Foreground = new SolidColorBrush(ColorHelper.FromRgb(100, 100, 100)),
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            examplePanel.Children.Add(exampleText);

            // 例句中文翻译
            if (!string.IsNullOrEmpty(word.ExampleTranslation))
            {
                var exampleTransText = new TextBlock
                {
                    Text = $"     {word.ExampleTranslation}",
                    FontSize = 12,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = new SolidColorBrush(ColorHelper.FromRgb(140, 140, 140)),
                    TextWrapping = TextWrapping.Wrap
                };
                examplePanel.Children.Add(exampleTransText);
            }

            wordPanel.Children.Add(examplePanel);
        }

        wordBorder.Child = wordPanel;
        ResultPanel.Children.Add(wordBorder);
    }

    // 支持回车键触发翻译
    private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter &&
            System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            TranslateButton_Click(sender, e!);
        }
    }
}

// 辅助类：创建颜色，避免命名空间冲突
internal static class ColorHelper
{
    public static System.Windows.Media.Color FromRgb(byte r, byte g, byte b) =>
        System.Windows.Media.Color.FromRgb(r, g, b);
}
