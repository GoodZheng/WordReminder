using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace WordReminder.ViewModels;

/// <summary>
/// 颜色选择器 ViewModel
/// </summary>
public partial class ColorPickerViewModel : ViewModelBase
{
    private readonly Action<string>? _onColorSelected;

    [ObservableProperty]
    private string _selectedColor;

    [ObservableProperty]
    private string _customColorText;

    [ObservableProperty]
    private Color _previewColor;

    public ColorPickerViewModel(string currentColor, Action<string>? onColorSelected = null)
    {
        _selectedColor = currentColor;
        _customColorText = currentColor;
        _onColorSelected = onColorSelected;
        _previewColor = Colors.White;

        UpdatePreview(currentColor);
    }

    /// <summary>
    /// 颜色按钮点击命令
    /// </summary>
    [RelayCommand]
    private void SelectColor(string color)
    {
        CustomColorText = color;
        UpdatePreview(color);
    }

    /// <summary>
    /// 自定义颜色文本变化
    /// </summary>
    partial void OnCustomColorTextChanged(string value)
    {
        if (IsValidColor(value))
        {
            UpdatePreview(value);
        }
    }

    /// <summary>
    /// 确定命令
    /// </summary>
    [RelayCommand]
    private void Ok()
    {
        var text = CustomColorText.Trim();
        if (IsValidColor(text))
        {
            SelectedColor = text;
            WeakReferenceMessenger.Default.Send(new ColorPickerConfirmedMessage(text));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new ShowColorPickerErrorMessage("请输入有效的颜色代码，例如：#FFFFFF"));
        }
    }

    /// <summary>
    /// 取消命令
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new ColorPickerCancelledMessage());
    }

    private void UpdatePreview(string color)
    {
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            PreviewColor = brush.Color;
        }
        catch { }
    }

    private bool IsValidColor(string color)
    {
        try
        {
            ColorConverter.ConvertFromString(color);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
