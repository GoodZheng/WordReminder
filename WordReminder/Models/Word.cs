namespace WordReminder.Models;

public class Word
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Phonetic { get; set; }
    public string? PartOfSpeech { get; set; }
    public string? Definition { get; set; }
    public string? Example { get; set; }
    public int DisplayOrder { get; set; }
}

public class WordDefinition
{
    public string PartOfSpeech { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
}
