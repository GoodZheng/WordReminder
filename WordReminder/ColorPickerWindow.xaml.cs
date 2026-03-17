using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WordReminder;

public partial class ColorPickerWindow : Window
{
    public string SelectedColor { get; private set; }

    public ColorPickerWindow(string currentColor)
    {
        InitializeComponent();
        SelectedColor = currentColor;
        CustomColorTextBox.Text = currentColor;
        UpdatePreview(currentColor);

        // 绑定文本变化事件
        CustomColorTextBox.TextChanged += (s, e) =>
        {
            var text = CustomColorTextBox.Text.Trim();
            if (IsValidColor(text))
            {
                UpdatePreview(text);
            }
        };
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string color)
        {
            CustomColorTextBox.Text = color;
            UpdatePreview(color);
        }
    }

    private void UpdatePreview(string color)
    {
        try
        {
            var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
            PreviewBrush.Color = brush.Color;
        }
        catch { }
    }

    private bool IsValidColor(string color)
    {
        try
        {
            System.Windows.Media.ColorConverter.ConvertFromString(color);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var text = CustomColorTextBox.Text.Trim();
        if (IsValidColor(text))
        {
            SelectedColor = text;
            DialogResult = true;
            Close();
        }
        else
        {
            System.Windows.MessageBox.Show("请输入有效的颜色代码，例如：#FFFFFF", "无效的颜色", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
