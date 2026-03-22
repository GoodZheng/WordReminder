using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;
using WordReminder.ViewModels;

namespace WordReminder.Views;

public partial class SettingsWindow : Window
{
    private SettingsViewModel? _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();

        // 注册消息监听
        WeakReferenceMessenger.Default.Register<CloseSettingsMessage>(this, (_, _) => Close());
        WeakReferenceMessenger.Default.Register<OpenColorPickerMessage>(this, Receive);

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as SettingsViewModel;

        // 同步 PasswordBox 的值（双向绑定不支持 PasswordBox）
        if (_viewModel != null)
        {
            ApiKeyPasswordBox.Password = _viewModel.ApiKey;

            // 监听 PasswordBox 变化
            ApiKeyPasswordBox.PasswordChanged += (_, _) =>
            {
                // 这里需要通过反射或另一种方式更新 ViewModel
                // 由于 PasswordBox 不支持双向绑定，我们将在保存时处理
            };
        }
    }

    /// <summary>
    /// 接收颜色选择消息
    /// </summary>
    private void Receive(object recipient, OpenColorPickerMessage message)
    {
        var colorPicker = new ColorPickerWindow(message.CurrentColor);
        colorPicker.Owner = this;

        if (colorPicker.ShowDialog() == true)
        {
            message.OnColorSelected(colorPicker.SelectedColor);
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 在关闭前同步 PasswordBox 的值
        if (_viewModel != null)
        {
            _viewModel.ApiKey = ApiKeyPasswordBox.Password;
        }
        base.OnClosing(e);
    }
}
