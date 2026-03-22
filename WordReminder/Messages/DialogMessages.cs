using WordReminder.Models;

namespace WordReminder.Messages;

/// <summary>
/// 添加单词确认消息
/// </summary>
public record AddWordConfirmedMessage(string WordText);

/// <summary>
/// 添加单词取消消息
/// </summary>
public record AddWordCancelledMessage;

/// <summary>
/// 显示添加单词错误消息
/// </summary>
public record ShowAddWordErrorMessage(string Message);

/// <summary>
/// 颜色选择器确认消息
/// </summary>
public record ColorPickerConfirmedMessage(string SelectedColor);

/// <summary>
/// 颜色选择器取消消息
/// </summary>
public record ColorPickerCancelledMessage;

/// <summary>
/// 显示颜色选择器错误消息
/// </summary>
public record ShowColorPickerErrorMessage(string Message);
