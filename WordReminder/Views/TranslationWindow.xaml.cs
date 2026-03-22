using System.Windows;
using System.Windows.Input;
using WordReminder.Services;
using WordReminder.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace WordReminder.Views;

public partial class TranslationWindow : Window
{
    public TranslationWindow(TranslationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // 设置焦点到输入框
        Loaded += (_, _) => Focus();
    }

    // 输入框回车键触发翻译，Shift+Enter 换行
    private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            if (DataContext is TranslationViewModel viewModel)
            {
                viewModel.TranslateCommand.Execute(null);
            }
            e.Handled = true;
        }
    }
}
