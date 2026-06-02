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

    public AssistantListViewModel(AssistantService assistantService, IServiceProvider serviceProvider)
    {
        _assistantService = assistantService;
        _serviceProvider = serviceProvider;

        LoadAssistants();
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
    /// 当选中助手改变时，更新详情面板
    /// </summary>
    partial void OnSelectedAssistantChanged(AssistantItemViewModel? value)
    {
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
        var chatWindow = new ChatWindow(chatViewModel);
        chatWindow.Show();
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
