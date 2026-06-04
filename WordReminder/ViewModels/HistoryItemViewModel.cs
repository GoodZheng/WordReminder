using CommunityToolkit.Mvvm.ComponentModel;
using WordReminder.Models;

namespace WordReminder.ViewModels;

/// <summary>
/// 翻译历史列表项 ViewModel
/// </summary>
public partial class HistoryItemViewModel : ObservableObject
{
    public int Id { get; }
    public string InputText { get; }
    public string? TranslatedText { get; }
    public string? FullJson { get; }
    public DateTime CreatedAt { get; }

    /// <summary>
    /// 截断后的译文摘要（最多 50 字符）
    /// </summary>
    public string? TranslatedTextPreview =>
        string.IsNullOrEmpty(TranslatedText) ? null :
        TranslatedText.Length > 50 ? TranslatedText[..50] + "…" : TranslatedText;

    /// <summary>
    /// 格式化的创建时间（如 "2026-04-26 14:30"）
    /// </summary>
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm");

    public HistoryItemViewModel(TranslationHistoryEntry entry)
    {
        Id = entry.Id;
        InputText = entry.InputText;
        TranslatedText = entry.TranslatedText;
        FullJson = entry.FullJson;
        CreatedAt = entry.CreatedAt;
    }
}
