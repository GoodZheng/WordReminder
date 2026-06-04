using WordReminder.Models;

namespace WordReminder.Messages;

/// <summary>
/// 打开助手列表消息
/// </summary>
public record OpenAssistantListMessage;

/// <summary>
/// 打开助手编辑消息
/// </summary>
public record OpenAssistantEditMessage(Assistant? Assistant);

/// <summary>
/// 打开聊天消息
/// </summary>
public record OpenChatMessage(Assistant Assistant, Conversation? Conversation);

/// <summary>
/// 助手保存完成消息
/// </summary>
public record AssistantSavedMessage;
