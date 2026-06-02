namespace WordReminder.Models;

public class Assistant
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "🤖";
    public string SystemPrompt { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public string ModelId { get; set; } = "";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public bool IsBuiltin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
