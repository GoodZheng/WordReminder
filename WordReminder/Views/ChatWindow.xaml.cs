using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WordReminder.Controls;
using WordReminder.ViewModels;

namespace WordReminder.Views;

/// <summary>
/// ChatWindow.xaml 的交互逻辑
/// </summary>
public partial class ChatWindow : WindowBase
{
    private readonly ChatViewModel _viewModel;

    public ChatWindow(ChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 订阅消息集合变化事件，自动滚动到底部
        _viewModel.Messages.CollectionChanged += (s, e) =>
        {
            // 使用 Dispatcher 异步滚动，确保 UI 已更新
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MessagesScrollViewer != null)
                {
                    MessagesScrollViewer.ScrollToBottom();
                }
            }));
        };
    }

    /// <summary>
    /// 输入框按键事件处理
    /// </summary>
    private void InputTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            // Enter 键发送消息
            e.Handled = true;
            if (_viewModel.SendMessageCommand.CanExecute(null))
            {
                _viewModel.SendMessageCommand.Execute(null);
            }
        }
        else if (e.Key == Key.Escape)
        {
            // Esc 键取消发送（如果正在发送）
            if (_viewModel.IsSending && _viewModel.CancelSendCommand.CanExecute(null))
            {
                e.Handled = true;
                _viewModel.CancelSendCommand.Execute(null);
            }
        }
    }
}
