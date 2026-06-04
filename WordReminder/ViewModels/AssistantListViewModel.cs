using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using WordReminder.Models;
using WordReminder.Services;
using WordReminder.Views;

namespace WordReminder.ViewModels;

/// <summary>
/// 助手列表窗口 ViewModel
/// </summary>
public partial class AssistantListViewModel : ViewModelBase
{
    private readonly AssistantService _assistantService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConfigService _configService;

    [ObservableProperty]
    private ObservableCollection<AssistantItemViewModel> _assistants = new();

    [ObservableProperty]
    private AssistantItemViewModel? _selectedAssistant;

    [ObservableProperty]
    private string _selectedAssistantDetail = string.Empty;

    [ObservableProperty]
    private int _selectedConversationCount;

    [ObservableProperty]
    private string _selectedLastActive = "从未对话";

    [ObservableProperty]
    private bool _isChatMode;

    [ObservableProperty]
    private ChatViewModel? _currentChat;

    public AssistantListViewModel(AssistantService assistantService, IServiceProvider serviceProvider, ConfigService configService)
    {
        _assistantService = assistantService;
        _serviceProvider = serviceProvider;
        _configService = configService;

        LoadAssistants();
        RestoreLayout();
    }

    /// <summary>
    /// 从服务加载助手列表
    /// </summary>
    private void LoadAssistants()
    {
        var assistants = _assistantService.GetAllAssistants();
        Assistants.Clear();

        foreach (var assistant in assistants)
        {
            var conversationCount = _assistantService.GetConversationCount(assistant.Id);
            var lastActive = _assistantService.GetLastActiveTime(assistant.Id);

            Assistants.Add(new AssistantItemViewModel
            {
                Id = assistant.Id,
                Icon = assistant.Icon,
                Name = assistant.Name,
                ConversationCount = conversationCount,
                LastActive = lastActive,
                Assistant = assistant
            });
        }

        // 默认选中第一个
        if (Assistants.Count > 0)
        {
            SelectedAssistant = Assistants[0];
        }
    }

    /// <summary>
    /// 恢复上次关闭时的布局状态
    /// </summary>
    private void RestoreLayout()
    {
        var settings = _configService.Settings;

        // 恢复选中的助手
        if (settings.AssistantSelectedId > 0)
        {
            var item = Assistants.FirstOrDefault(a => a.Id == settings.AssistantSelectedId);
            if (item != null)
            {
                SelectedAssistant = item;
            }
        }

        // 恢复聊天模式
        if (settings.AssistantIsChatMode && SelectedAssistant != null)
        {
            var chatViewModel = _serviceProvider.GetRequiredService<ChatViewModel>();

            if (settings.AssistantConversationId > 0)
            {
                var chatService = _serviceProvider.GetRequiredService<ChatService>();
                var conversation = chatService.GetConversations(SelectedAssistant.Assistant.Id)
                    .FirstOrDefault(c => c.Id == settings.AssistantConversationId);
                chatViewModel.Initialize(SelectedAssistant.Assistant, conversation);
            }
            else
            {
                chatViewModel.Initialize(SelectedAssistant.Assistant);
            }

            CurrentChat = chatViewModel;
            IsChatMode = true;

            // 恢复面板折叠状态
            if (settings.AssistantConvPanelCollapsed)
            {
                chatViewModel.IsPanelVisible = false;
            }
        }
    }

    /// <summary>
    /// 获取当前配置（供 code-behind 读取布局参数）
    /// </summary>
    public AppSettings GetSettings() => _configService.Settings;

    /// <summary>
    /// 保存当前布局状态
    /// </summary>
    public void SaveLayout(double assistantListWidth, double convPanelWidth, bool convPanelCollapsed)
    {
        _configService.UpdateSettings(settings =>
        {
            settings.AssistantSelectedId = SelectedAssistant?.Id ?? 0;
            settings.AssistantIsChatMode = IsChatMode;
            settings.AssistantConversationId = CurrentChat?.SelectedConversation?.Id ?? 0;
            settings.AssistantListWidth = assistantListWidth > 0 ? assistantListWidth : 280;
            settings.AssistantConvPanelWidth = convPanelWidth > 0 ? convPanelWidth : 200;
            settings.AssistantConvPanelCollapsed = convPanelCollapsed;
        });
    }

    /// <summary>
    /// 当选中助手改变时，更新详情面板
    /// </summary>
    partial void OnSelectedAssistantChanged(AssistantItemViewModel? value)
    {
        if (IsChatMode)
        {
            IsChatMode = false;
            CurrentChat = null;
        }

        if (value == null)
        {
            SelectedAssistantDetail = string.Empty;
            SelectedConversationCount = 0;
            SelectedLastActive = "从未对话";
            return;
        }

        var assistant = value.Assistant;
        var providerInfo = !string.IsNullOrEmpty(assistant.ProviderName)
            ? $"{assistant.ProviderName} / {assistant.ModelId ?? "默认模型"}"
            : "使用默认模型";

        SelectedAssistantDetail = $"{providerInfo} | 温度: {assistant.Temperature:F1} | 最大Token: {assistant.MaxTokens}";
        SelectedConversationCount = value.ConversationCount;
        SelectedLastActive = value.LastActiveFormatted;
    }

    /// <summary>
    /// 新建助手命令
    /// </summary>
    [RelayCommand]
    private void NewAssistant()
    {
        var viewModel = _serviceProvider.GetRequiredService<AssistantEditViewModel>();
        var dialog = new AssistantEditDialog(viewModel);
        var result = dialog.ShowDialog();

        if (result == true)
        {
            LoadAssistants();
        }
    }

    /// <summary>
    /// 编辑助手命令
    /// </summary>
    [RelayCommand]
    private void EditAssistant()
    {
        if (SelectedAssistant == null)
        {
            return;
        }

        var viewModel = _serviceProvider.GetRequiredService<AssistantEditViewModel>();
        var editViewModel = new AssistantEditViewModel(
            _serviceProvider.GetRequiredService<AssistantService>(),
            _serviceProvider.GetRequiredService<ConfigService>(),
            SelectedAssistant.Assistant
        );

        var dialog = new AssistantEditDialog(editViewModel);
        var result = dialog.ShowDialog();

        if (result == true)
        {
            LoadAssistants();
        }
    }

    /// <summary>
    /// 删除助手命令
    /// </summary>
    [RelayCommand]
    private void DeleteAssistant()
    {
        if (SelectedAssistant == null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"确定要删除助手「{SelectedAssistant.Name}」吗？\n\n删除助手将同时删除其所有对话记录，此操作不可恢复。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            _assistantService.DeleteAssistant(SelectedAssistant.Id);
            LoadAssistants();
        }
    }

    /// <summary>
    /// 开始对话命令
    /// </summary>
    [RelayCommand]
    private void StartChat()
    {
        if (SelectedAssistant == null)
        {
            return;
        }

        var chatViewModel = _serviceProvider.GetRequiredService<ChatViewModel>();
        chatViewModel.Initialize(SelectedAssistant.Assistant);
        CurrentChat = chatViewModel;
        IsChatMode = true;
    }

    [RelayCommand]
    private void BackToDetail()
    {
        IsChatMode = false;
        CurrentChat = null;
        LoadAssistants();
    }

    /// <summary>
    /// 助手列表项 ViewModel（内部类）
    /// </summary>
    public class AssistantItemViewModel : ObservableObject
    {
        public int Id { get; set; }
        public string Icon { get; set; } = "🤖";
        public string Name { get; set; } = string.Empty;
        public int ConversationCount { get; set; }
        public DateTime? LastActive { get; set; }
        public Assistant Assistant { get; set; } = null!;

        /// <summary>
        /// 格式化的最后活跃时间
        /// </summary>
        public string LastActiveFormatted
        {
            get
            {
                if (!LastActive.HasValue)
                {
                    return "从未对话";
                }

                var span = DateTime.Now - LastActive.Value;
                if (span.TotalMinutes < 1)
                {
                    return "刚刚活跃";
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
                else
                {
                    return LastActive.Value.ToString("yyyy-MM-dd");
                }
            }
        }

        /// <summary>
        /// 对话数量显示文本
        /// </summary>
        public string ConversationCountText => ConversationCount == 0 ? "暂无对话" : $"{ConversationCount} 个对话";
    }
}
