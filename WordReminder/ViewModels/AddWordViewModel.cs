using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;

namespace WordReminder.ViewModels;

/// <summary>
/// 添加单词窗口 ViewModel
/// </summary>
public partial class AddWordViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _wordText = string.Empty;

    [ObservableProperty]
    private bool _isOkEnabled = true;

    /// <summary>
    /// 确定命令
    /// </summary>
    [RelayCommand]
    private void Ok()
    {
        var text = WordText.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            // 通过消息显示错误
            WeakReferenceMessenger.Default.Send(new ShowAddWordErrorMessage("请输入单词"));
            return;
        }

        WeakReferenceMessenger.Default.Send(new AddWordConfirmedMessage(text));
    }

    /// <summary>
    /// 取消命令
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new AddWordCancelledMessage());
    }
}
