namespace WordReminder.Models;

public class Conversation
{
    public int Id { get; set; }
    public int AssistantId { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
