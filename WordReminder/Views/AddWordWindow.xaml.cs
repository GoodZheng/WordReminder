using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;
using MessageBox = System.Windows.MessageBox;

namespace WordReminder.Views;

public partial class AddWordWindow : Window
{
    public string WordText { get; private set; } = string.Empty;

    public AddWordWindow()
    {
        InitializeComponent();

        // 注册消息监听
        WeakReferenceMessenger.Default.Register<AddWordConfirmedMessage>(this, ReceiveConfirmed);
        WeakReferenceMessenger.Default.Register<AddWordCancelledMessage>(this, (_, _) => CloseDialog(false));
        WeakReferenceMessenger.Default.Register<ShowAddWordErrorMessage>(this, ReceiveError);

        Loaded += (_, _) => WordInput.Focus();
    }

    private void ReceiveConfirmed(object recipient, AddWordConfirmedMessage message)
    {
        WordText = message.WordText;
        CloseDialog(true);
    }

    private void ReceiveError(object recipient, ShowAddWordErrorMessage message)
    {
        MessageBox.Show(message.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void CloseDialog(bool result)
    {
        DialogResult = result;
        Close();
    }
}
