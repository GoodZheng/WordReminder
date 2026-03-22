using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using WordReminder.Messages;
using WordReminder.ViewModels;
using WordReminder.Views;

namespace WordReminder.Services;

/// <summary>
/// 窗口管理服务 - 统一管理窗口的打开和关闭
/// </summary>
public class WindowManagerService : IRecipient<OpenSettingsMessage>,
    IRecipient<OpenAddWordMessage>,
    IRecipient<OpenAllWordsMessage>,
    IRecipient<OpenTranslationMessage>,
    IRecipient<CloseSettingsMessage>,
    IRecipient<OpenColorPickerMessage>,
    IRecipient<CloseAllWordsMessage>,
    IRecipient<ColorPickerConfirmedMessage>,
    IRecipient<ColorPickerCancelledMessage>,
    IRecipient<AddWordConfirmedMessage>,
    IRecipient<AddWordCancelledMessage>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessenger _messenger;
    private Window? _settingsWindow;
    private Window? _addWordWindow;
    private Window? _allWordsWindow;
    private Window? _translationWindow;
    private Window? _colorPickerWindow;

    public WindowManagerService(IServiceProvider serviceProvider, IMessenger messenger)
    {
        _serviceProvider = serviceProvider;
        _messenger = messenger;
        // 注册所有消息接收
        messenger.Register<OpenSettingsMessage>(this);
        messenger.Register<OpenAddWordMessage>(this);
        messenger.Register<OpenAllWordsMessage>(this);
        messenger.Register<OpenTranslationMessage>(this);
        messenger.Register<CloseSettingsMessage>(this);
        messenger.Register<OpenColorPickerMessage>(this);
        messenger.Register<CloseAllWordsMessage>(this);
        messenger.Register<ColorPickerConfirmedMessage>(this);
        messenger.Register<ColorPickerCancelledMessage>(this);
        messenger.Register<AddWordConfirmedMessage>(this);
        messenger.Register<AddWordCancelledMessage>(this);
    }

    public void Receive(OpenSettingsMessage message)
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            var viewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
            _settingsWindow = new SettingsWindow
            {
                DataContext = viewModel
            };
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public void Receive(OpenAddWordMessage message)
    {
        if (_addWordWindow == null || !_addWordWindow.IsLoaded)
        {
            var viewModel = _serviceProvider.GetRequiredService<AddWordViewModel>();
            _addWordWindow = new AddWordWindow
            {
                DataContext = viewModel
            };
        }
        _addWordWindow.ShowDialog();
    }

    public void Receive(OpenAllWordsMessage message)
    {
        if (_allWordsWindow == null || !_allWordsWindow.IsLoaded)
        {
            var viewModel = _serviceProvider.GetRequiredService<AllWordsViewModel>();
            _allWordsWindow = new AllWordsWindow
            {
                DataContext = viewModel
            };
        }
        _allWordsWindow.ShowDialog();
    }

    public void Receive(CloseAllWordsMessage message)
    {
        _allWordsWindow?.Close();
        _allWordsWindow = null;
    }

    public void Receive(OpenTranslationMessage message)
    {
        if (_translationWindow == null || !_translationWindow.IsLoaded)
        {
            var viewModel = _serviceProvider.GetRequiredService<TranslationViewModel>();
            _translationWindow = new TranslationWindow(viewModel);
        }
        _translationWindow.ShowDialog();
    }

    public void Receive(OpenColorPickerMessage message)
    {
        var viewModel = new ColorPickerViewModel(message.CurrentColor, message.OnColorSelected);
        _colorPickerWindow = new ColorPickerWindow(message.CurrentColor)
        {
            DataContext = viewModel
        };
        _colorPickerWindow.ShowDialog();
    }

    public void Receive(CloseSettingsMessage message)
    {
        _settingsWindow?.Close();
        _settingsWindow = null;
    }

    public void Receive(ColorPickerConfirmedMessage message)
    {
        _colorPickerWindow = null;
    }

    public void Receive(ColorPickerCancelledMessage message)
    {
        _colorPickerWindow = null;
    }

    public void Receive(AddWordConfirmedMessage message)
    {
        _addWordWindow = null;
    }

    public void Receive(AddWordCancelledMessage message)
    {
        _addWordWindow = null;
    }
}
