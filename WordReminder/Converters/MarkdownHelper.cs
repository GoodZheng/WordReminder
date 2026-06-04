using System.Windows;

namespace WordReminder.Converters;

public static class MarkdownHelper
{
    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.RegisterAttached(
            "Markdown",
            typeof(string),
            typeof(MarkdownHelper),
            new PropertyMetadata("", OnMarkdownChanged));

    public static string GetMarkdown(DependencyObject obj) => (string)obj.GetValue(MarkdownProperty);
    public static void SetMarkdown(DependencyObject obj, string value) => obj.SetValue(MarkdownProperty, value);

    private static readonly MdXaml.Markdown _engine = new();

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is System.Windows.Controls.RichTextBox rtb)
        {
            try
            {
                var markdown = e.NewValue as string ?? "";
                rtb.Document = _engine.Transform(markdown);
            }
            catch
            {
                // MdXaml 解析失败时使用默认空文档
            }
        }
    }
}
