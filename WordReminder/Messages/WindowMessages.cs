namespace WordReminder.Messages;

/// <summary>
/// 打开设置窗口消息
/// </summary>
public record OpenSettingsMessage;

/// <summary>
/// 关闭设置窗口消息
/// </summary>
public record CloseSettingsMessage;

/// <summary>
/// 打开添加单词窗口消息
/// </summary>
public record OpenAddWordMessage;

/// <summary>
/// 打开所有单词窗口消息
/// </summary>
public record OpenAllWordsMessage;

/// <summary>
/// 关闭所有单词窗口消息
/// </summary>
public record CloseAllWordsMessage;

/// <summary>
/// 打开翻译窗口消息
/// </summary>
public record OpenTranslationMessage;

/// <summary>
/// 打开颜色选择器消息
/// </summary>
public record OpenColorPickerMessage(string Tag, string CurrentColor, Action<string> OnColorSelected);
