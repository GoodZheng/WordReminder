using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WordReminder.Models;
using WordReminder.Services;

namespace WordReminder.ViewModels;

/// <summary>
/// 对话窗口 ViewModel - 管理聊天界面和消息流
/// </summary>
public partial class ChatViewModel : ViewModelBase
{
    private readonly ChatService _chatService;
    private readonly ChatAIService _chatAIService;
    private readonly AssistantService _assistantService;
    private readonly ILogger<ChatViewModel> _logger;
    private CancellationTokenSource? _sendMessageCts;
    private Assistant? _currentAssistant;
    private Conversation? _currentConversation;
    private bool _isLoadingConversations;

    [ObservableProperty]
    private string _assistantName = string.Empty;

    [ObservableProperty]
    private string _assistantIcon = "🤖";

    [ObservableProperty]
    private string _modelInfo = string.Empty;

    [ObservableProperty]
    private string _conversationTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ConversationItemViewModel> _conversations = new();

    [ObservableProperty]
    private ConversationItemViewModel? _selectedConversation;

    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private ChatMessageViewModel? _currentAssistantMessage;

    [ObservableProperty]
    private bool _showEmptyState = true;

    [ObservableProperty]
    private bool _isPanelVisible = true;


    public ChatViewModel(
        ChatService chatService,
        ChatAIService chatAIService,
        AssistantService assistantService,
        ILogger<ChatViewModel> logger)
    {
        _chatService = chatService;
        _chatAIService = chatAIService;
        _assistantService = assistantService;
        _logger = logger;
    }

    /// <summary>
    /// 初始化 ViewModel（从 AssistantListWindow 传递 Assistant 对象）
    /// </summary>
    public void Initialize(Assistant assistant, Conversation? conversation = null)
    {
        _currentAssistant = assistant;
        _currentConversation = conversation;

        AssistantName = assistant.Name;
        AssistantIcon = assistant.Icon;

        // 构建模型信息
        var providerInfo = !string.IsNullOrEmpty(assistant.ProviderName)
            ? $"{assistant.ProviderName} / {assistant.ModelId ?? "默认模型"}"
            : "使用默认模型";
        ModelInfo = $"{providerInfo} | 温度: {assistant.Temperature:F1}";


        LoadConversations();

        if (conversation != null && Conversations.Any())
        {
            // 如果指定了对话，选中它
            var item = Conversations.FirstOrDefault(c => c.Id == conversation.Id);
            if (item != null)
            {
                SelectedConversation = item;
            }
        }
        else if (Conversations.Count > 0)
        {
            // 否则选中最近的对话
            SelectedConversation = Conversations[0];
        }
    }

    /// <summary>
    /// 加载对话列表
    /// </summary>
    [RelayCommand]
    private void LoadConversations()
    {
        if (_currentAssistant == null) return;

        _isLoadingConversations = true;
        try
        {
            var selectedId = SelectedConversation?.Id;
            var conversations = _chatService.GetConversations(_currentAssistant.Id);
            Conversations.Clear();

            foreach (var conv in conversations)
            {
                var messageCount = _chatService.GetMessageCount(conv.Id);
                Conversations.Add(new ConversationItemViewModel(conv, messageCount));
            }

            if (selectedId.HasValue)
            {
                var item = Conversations.FirstOrDefault(c => c.Id == selectedId.Value);
                if (item != null)
                    SelectedConversation = item;
            }
        }
        finally
        {
            _isLoadingConversations = false;
        }
    }

    /// <summary>
    /// 当选中对话改变时，加载消息
    /// </summary>
    partial void OnSelectedConversationChanged(ConversationItemViewModel? value)
    {
        if (_isLoadingConversations) return;

        if (value == null)
        {
            Messages.Clear();
            ConversationTitle = string.Empty;
            ShowEmptyState = true;
            return;
        }

        _currentConversation = value.Conversation;
        ConversationTitle = value.Title;

        LoadMessages(value.Id);
    }

    /// <summary>
    /// 加载消息列表
    /// </summary>
    private void LoadMessages(int conversationId)
    {
        var messages = _chatService.GetMessages(conversationId);
        Messages.Clear();

        foreach (var msg in messages)
        {
            Messages.Add(new ChatMessageViewModel(msg));
        }

        ShowEmptyState = Messages.Count == 0;
    }

    /// <summary>
    /// 创建新对话
    /// </summary>
    [RelayCommand]
    private void NewConversation()
    {
        if (_currentAssistant == null) return;

        var conversation = _chatService.CreateConversation(_currentAssistant.Id);
        LoadConversations();

        // 选中新创建的对话
        var newItem = Conversations.FirstOrDefault(c => c.Id == conversation.Id);
        if (newItem != null)
        {
            SelectedConversation = newItem;
        }
    }

    /// <summary>
    /// 删除对话
    /// </summary>
    [RelayCommand]
    private void DeleteConversation(ConversationItemViewModel? item)
    {
        if (item == null) return;

        var result = System.Windows.MessageBox.Show(
            $"确定要删除对话「{item.Title}」吗？\n\n此操作不可恢复。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            _chatService.DeleteConversation(item.Id);

            // 如果删除的是当前对话，清空消息
            if (SelectedConversation?.Id == item.Id)
            {
                Messages.Clear();
                ShowEmptyState = true;
                ConversationTitle = string.Empty;
            }

            LoadConversations();
        }
    }

    /// <summary>
    /// 清空所有对话历史
    /// </summary>
    [RelayCommand]
    private void ClearAllConversations()
    {
        if (_currentAssistant == null || Conversations.Count == 0) return;

        var result = System.Windows.MessageBox.Show(
            $"确定要清空助手「{_currentAssistant.Name}」的所有对话历史吗？\n\n共 {Conversations.Count} 个对话将被删除，此操作不可恢复。",
            "确认清空",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            foreach (var conv in Conversations.ToList())
            {
                _chatService.DeleteConversation(conv.Id);
            }

            Messages.Clear();
            ShowEmptyState = true;
            ConversationTitle = string.Empty;
            _currentConversation = null;
            SelectedConversation = null;
            LoadConversations();
        }
    }

    /// <summary>
    /// 切换左侧面板显示/隐藏
    /// </summary>
    [RelayCommand]
    private void TogglePanel()
    {
        IsPanelVisible = !IsPanelVisible;
    }

    /// <summary>
    /// 取消发送消息
    /// </summary>
    [RelayCommand]
    private void CancelSend()
    {
        _sendMessageCts?.Cancel();
        IsSending = false;
    }

    /// <summary>
    /// 发送消息 - 核心方法
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (_currentAssistant == null)
        {
            System.Windows.MessageBox.Show("请先选择助手", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(InputText))
            return;

        if (IsSending)
            return;

        // 1. 确保有对话（如果没有则创建）
        if (_currentConversation == null)
        {
            _currentConversation = _chatService.CreateConversation(_currentAssistant.Id);
            LoadConversations();

            var newItem = Conversations.FirstOrDefault(c => c.Id == _currentConversation.Id);
            if (newItem != null)
            {
                SelectedConversation = newItem;
            }
        }

        var conversationId = _currentConversation.Id;
        var userMessageContent = InputText.Trim();
        InputText = string.Empty;

        try
        {
            IsSending = true;
            _sendMessageCts = new CancellationTokenSource();

            // 2. 添加用户消息到 UI 和数据库
            var userMsg = new ChatMessageViewModel
            {
                Role = "user",
                Content = userMessageContent,
                IsUser = true,
                IsAssistant = false
            };
            Messages.Add(userMsg);
            ShowEmptyState = false;

            var savedUserMsg = _chatService.SaveMessage(conversationId, "user", userMessageContent);
            userMsg.Id = savedUserMsg.Id;

            // 3. 如果是第一条消息，更新对话标题（截取到30字符）
            if (Messages.Count(m => m.Role == "user") == 1)
            {
                var title = userMessageContent.Length > 30
                    ? userMessageContent.Substring(0, 30) + "..."
                    : userMessageContent;
                _chatService.UpdateConversationTitle(conversationId, title);

                if (SelectedConversation != null)
                {
                    SelectedConversation.Title = title;
                }

                ConversationTitle = title;
            }

            // 4. 加载历史消息
            var history = _chatService.GetMessages(conversationId);

            // 5. 创建 AI 消息占位符（空内容）
            var aiMsg = new ChatMessageViewModel
            {
                Role = "assistant",
                Content = string.Empty,
                IsUser = false,
                IsAssistant = true
            };
            Messages.Add(aiMsg);
            CurrentAssistantMessage = aiMsg;

            // 6. 流式调用 AI API
            var fullContent = new System.Text.StringBuilder();
            _logger.LogInformation("开始发送消息到 AI，对话 ID: {ConversationId}", conversationId);

            await foreach (var chunk in _chatAIService.SendMessageAsync(
                _currentAssistant,
                history,
                userMessageContent,
                _sendMessageCts.Token))
            {
                // 7. 增量更新 AI 消息内容
                fullContent.Append(chunk);
                aiMsg.Content = fullContent.ToString();
                aiMsg.OnContentChanged(); // 通知 UI 更新
            }

            _logger.LogInformation("AI 响应完成，对话 ID: {ConversationId}", conversationId);

            // 8. 保存 AI 消息到数据库
            if (!string.IsNullOrEmpty(aiMsg.Content))
            {
                var savedAiMsg = _chatService.SaveMessage(conversationId, "assistant", aiMsg.Content);
                aiMsg.Id = savedAiMsg.Id;
            }

            // 刷新对话列表（更新时间）
            LoadConversations();
        }
        catch (OperationCanceledException)
        {
            // 9. 处理取消
            _logger.LogInformation("用户取消了发送消息");
            if (CurrentAssistantMessage != null && string.IsNullOrEmpty(CurrentAssistantMessage.Content))
            {
                Messages.Remove(CurrentAssistantMessage);
            }
        }
        catch (Exception ex)
        {
            // 9. 处理错误
            _logger.LogError(ex, "发送消息失败");

            if (CurrentAssistantMessage != null)
            {
                CurrentAssistantMessage.Content = $"❌ 发送失败：{ex.Message}";
                CurrentAssistantMessage.IsError = true;
                CurrentAssistantMessage.OnContentChanged();
            }
            else
            {
                var errorMsg = new ChatMessageViewModel
                {
                    Role = "assistant",
                    Content = $"❌ 发送失败：{ex.Message}",
                    IsUser = false,
                    IsAssistant = true,
                    IsError = true
                };
                Messages.Add(errorMsg);
            }
        }
        finally
        {
            IsSending = false;
            CurrentAssistantMessage = null;
            _sendMessageCts?.Dispose();
            _sendMessageCts = null;
        }

        // 10. 刷新对话列表（更新消息数）
        if (SelectedConversation != null)
        {
            var msgCount = _chatService.GetMessageCount(SelectedConversation.Id);
            SelectedConversation.MessageCount = msgCount;
        }
    }


    /// <summary>
    /// 对话列表项 ViewModel（内部类）
    /// </summary>
    public partial class ConversationItemViewModel : ObservableObject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }

        [ObservableProperty]
        private string _timeDisplay = string.Empty;

        public Conversation Conversation { get; set; } = null!;

        public ConversationItemViewModel(Conversation conversation, int messageCount)
        {
            Id = conversation.Id;
            Title = conversation.Title;
            UpdatedAt = conversation.UpdatedAt;
            MessageCount = messageCount;
            Conversation = conversation;
            TimeDisplay = FormatRelativeTime(conversation.UpdatedAt);
        }

        /// <summary>
        /// 格式化相对时间显示
        /// </summary>
        private static string FormatRelativeTime(DateTime time)
        {
            var span = DateTime.Now - time;
            if (span.TotalMinutes < 1)
            {
                return "刚刚";
            }
            else if (span.TotalHours < 1)
            {
                return $"{(int)span.TotalMinutes} 分钟前";
            }
            else if (span.TotalDays < 1)
            {
                return $"{(int)span.TotalHours} 小时前";
            }
            else if (span.TotalDays < 7)
            {
                return $"{(int)span.TotalDays} 天前";
            }
            else if (time.Year == DateTime.Now.Year)
            {
                return time.ToString("MM-dd");
            }
            else
            {
                return time.ToString("yyyy-MM-dd");
            }
        }
    }

    /// <summary>
    /// 聊天消息 ViewModel（内部类）
    /// </summary>
    public partial class ChatMessageViewModel : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty]
        private string _role = string.Empty;

        [ObservableProperty]
        private string _content = string.Empty;

        [ObservableProperty]
        private bool _isError;

        [ObservableProperty]
        private bool _isUser;

        [ObservableProperty]
        private bool _isAssistant;

        public ChatMessageViewModel() { }

        public ChatMessageViewModel(ChatMessage message)
        {
            Id = message.Id;
            Role = message.Role;
            Content = message.Content;
            IsUser = Role == "user";
            IsAssistant = Role == "assistant";
        }

        /// <summary>
        /// 当内容改变时通知 UI（用于流式更新）
        /// </summary>
        public void OnContentChanged()
        {
            OnPropertyChanged(nameof(Content));
        }
    }
}
