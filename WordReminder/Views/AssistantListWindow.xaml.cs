using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WordReminder.Controls;
using WordReminder.ViewModels;

namespace WordReminder.Views;

/// <summary>
/// 助手列表窗口
/// </summary>
public partial class AssistantListWindow : WindowBase
{
    private ChatViewModel? _subscribedChat;
    private double _lastConvWidth = 200;
    private double _convPanelWidthBeforeDrag;
    private const double MinConvPanelWidth = 80;

    public AssistantListWindow(AssistantListViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var vm = (AssistantListViewModel)DataContext;

        // 从配置恢复面板宽度
        var settings = vm.GetSettings();
        var listWidth = settings.AssistantListWidth > 0 ? settings.AssistantListWidth : 280;
        AssistantListColumn.Width = new GridLength(listWidth);

        // 如果从配置恢复了 CurrentChat，需要手动订阅事件
        // （因为 RestoreLayout 在 VM 构造函数中执行，早于窗口订阅 PropertyChanged）
        if (vm.CurrentChat != null && _subscribedChat == null)
        {
            _subscribedChat = vm.CurrentChat;
            _subscribedChat.Messages.CollectionChanged += OnMessagesCollectionChanged;
            _subscribedChat.PropertyChanged += OnChatPropertyChanged;
        }

        if (vm.IsChatMode)
        {
            _lastConvWidth = settings.AssistantConvPanelWidth > 0 ? settings.AssistantConvPanelWidth : 200;
            if (settings.AssistantConvPanelCollapsed)
            {
                SetConvPanelWidth(0);
            }
            else
            {
                SetConvPanelWidth(_lastConvWidth);
            }
        }

        // 注册 GridSplitter 拖拽事件
        ConvGridSplitter.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnConvSplitterDragStarted));
        ConvGridSplitter.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnConvSplitterDragCompleted));
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        var vm = (AssistantListViewModel)DataContext;
        var isCollapsed = ConvColumn.Width.Value <= 0;
        var convWidth = ConvColumn.Width.Value > 0 ? ConvColumn.Width.Value : _lastConvWidth;
        vm.SaveLayout(AssistantListColumn.Width.Value, convWidth, isCollapsed);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssistantListViewModel.CurrentChat))
        {
            var vm = (AssistantListViewModel)DataContext;

            if (_subscribedChat != null)
            {
                _subscribedChat.Messages.CollectionChanged -= OnMessagesCollectionChanged;
                _subscribedChat.PropertyChanged -= OnChatPropertyChanged;
            }

            _subscribedChat = vm.CurrentChat;

            if (_subscribedChat != null)
            {
                _subscribedChat.Messages.CollectionChanged += OnMessagesCollectionChanged;
                _subscribedChat.PropertyChanged += OnChatPropertyChanged;

                if (_subscribedChat.IsPanelVisible)
                {
                    SetConvPanelWidth(_lastConvWidth);
                }
                else
                {
                    SetConvPanelWidth(0);
                }
            }
        }
    }

    private void OnChatPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatViewModel.IsPanelVisible) && _subscribedChat != null)
        {
            if (_subscribedChat.IsPanelVisible)
            {
                SetConvPanelWidth(_lastConvWidth);
            }
            else
            {
                if (ConvColumn.ActualWidth > 0)
                {
                    _lastConvWidth = ConvColumn.ActualWidth;
                }
                SetConvPanelWidth(0);
            }
        }
    }

    private void SetConvPanelWidth(double width)
    {
        ConvColumn.Width = new GridLength(width);
        ConvSplitterColumn.Width = new GridLength(width > 0 ? 4 : 0);
    }

    private void OnConvSplitterDragStarted(object sender, DragStartedEventArgs e)
    {
        _convPanelWidthBeforeDrag = ConvColumn.ActualWidth;
    }

    private void OnConvSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (ConvColumn.ActualWidth > 0 && ConvColumn.ActualWidth < MinConvPanelWidth)
        {
            // 保存拖拽前的宽度（如果它大于最小宽度）
            if (_convPanelWidthBeforeDrag >= MinConvPanelWidth)
            {
                _lastConvWidth = _convPanelWidthBeforeDrag;
            }
            else
            {
                _lastConvWidth = ConvColumn.ActualWidth;
            }

            if (_subscribedChat != null)
            {
                _subscribedChat.IsPanelVisible = false;
            }
            SetConvPanelWidth(0);
        }
    }

    private void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ChatMessagesScrollViewer?.ScrollToBottom();
        }));
    }

    private void ChatInputTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var vm = (AssistantListViewModel)DataContext;
        var chat = vm.CurrentChat;
        if (chat == null) return;

        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            if (chat.SendMessageCommand.CanExecute(null))
            {
                chat.SendMessageCommand.Execute(null);
            }
        }
        else if (e.Key == Key.Escape)
        {
            if (chat.IsSending && chat.CancelSendCommand.CanExecute(null))
            {
                e.Handled = true;
                chat.CancelSendCommand.Execute(null);
            }
        }
    }
}
