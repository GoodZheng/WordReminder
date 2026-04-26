using System.Windows;

namespace WordReminder.Views;

public partial class InputDialog : Controls.WindowBase
{
    public string InputText { get; private set; } = "";

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeComponent();
        TitleText = title;
        PromptText.Text = prompt;
        InputTextBox.Text = defaultValue;
        InputTextBox.SelectAll();
        InputTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var text = InputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            ErrorText.Text = "输入不能为空";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        InputText = text;
        DialogResult = true;
    }
}
