using WordReminder.ViewModels;

namespace WordReminder.Views;

/// <summary>
/// 助手列表窗口
/// </summary>
public partial class AssistantListWindow : Controls.WindowBase
{
    public AssistantListWindow(AssistantListViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
