namespace WordReminder.Models;

/// <summary>
/// 翻译历史记录数据库实体
/// </summary>
public class TranslationHistoryEntry
{
    public int Id { get; set; }
    public string InputText { get; set; } = string.Empty;
    public string? TranslatedText { get; set; }
    public string? FullJson { get; set; }
    public string? TextType { get; set; }
    public string? Direction { get; set; }
    public DateTime CreatedAt { get; set; }
}
