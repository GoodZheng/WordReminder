using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;
using WordReminder.ViewModels;

namespace WordReminder.Views;

public partial class SettingsWindow : Controls.WindowBase
{
    private SettingsViewModel? _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();

        // 注册消息监听
        WeakReferenceMessenger.Default.Register<CloseSettingsMessage>(this, (_, _) => Close());
        WeakReferenceMessenger.Default.Register<OpenColorPickerMessage>(this, Receive);
        WeakReferenceMessenger.Default.Register<AddProviderMessage>(this, (_, _) => OnAddProvider());
        WeakReferenceMessenger.Default.Register<AddModelMessage>(this, (_, _) => OnAddModel());

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as SettingsViewModel;

        // 同步 PasswordBox 的值（双向绑定不支持 PasswordBox）
        if (_viewModel != null)
        {
            ApiKeyPasswordBox.Password = _viewModel.ApiKey;

            // 监听 PasswordBox 变化，实时同步到 ViewModel
            ApiKeyPasswordBox.PasswordChanged += (_, _) =>
            {
                _viewModel.ApiKey = ApiKeyPasswordBox.Password;
            };

            // 监听厂商切换，同步 PasswordBox
            _viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(SettingsViewModel.ApiKey))
                {
                    if (ApiKeyPasswordBox.Password != _viewModel.ApiKey)
                    {
                        ApiKeyPasswordBox.Password = _viewModel.ApiKey ?? "";
                    }
                }
            };
        }
    }

    private void OnAddProvider()
    {
        var dialog = new InputDialog("添加厂商", "请输入厂商名称：", "厂商名称");
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var name = dialog.InputText;
            var urlDialog = new InputDialog("API URL", $"请输入 {name} 的 API URL：", "https://");
            urlDialog.Owner = this;
            if (urlDialog.ShowDialog() == true)
            {
                _viewModel?.CompleteAddProvider(name, urlDialog.InputText);
            }
        }
    }

    private void OnAddModel()
    {
        var dialog = new InputDialog("添加模型", "请输入模型 ID：", "模型 ID");
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var modelId = dialog.InputText;
            _viewModel?.CompleteAddModel(modelId, modelId);
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
