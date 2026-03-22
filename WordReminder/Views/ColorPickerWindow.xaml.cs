using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;
using WordReminder.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace WordReminder.Views;

public partial class ColorPickerWindow : Window
{
    public string SelectedColor { get; private set; } = string.Empty;

    public ColorPickerWindow(string currentColor)
    {
        InitializeComponent();

        var viewModel = new ColorPickerViewModel(currentColor);
        DataContext = viewModel;

        // 注册消息监听
        WeakReferenceMessenger.Default.Register<ColorPickerConfirmedMessage>(this, ReceiveConfirmed);
        WeakReferenceMessenger.Default.Register<ColorPickerCancelledMessage>(this, (_, _) => CloseDialog(false));
        WeakReferenceMessenger.Default.Register<ShowColorPickerErrorMessage>(this, ReceiveError);
    }

    private void ReceiveConfirmed(object recipient, ColorPickerConfirmedMessage message)
    {
        SelectedColor = message.SelectedColor;
        CloseDialog(true);
    }

    private void ReceiveError(object recipient, ShowColorPickerErrorMessage message)
    {
        MessageBox.Show(message.Message, "无效的颜色", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void CloseDialog(bool result)
    {
        DialogResult = result;
        Close();
    }
}
