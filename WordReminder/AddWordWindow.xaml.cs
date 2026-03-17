using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WordReminder;

public partial class AddWordWindow : Window
{
    public string WordText { get; private set; } = string.Empty;

    public AddWordWindow()
    {
        InitializeComponent();
        WordInput.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var text = WordInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            System.Windows.MessageBox.Show("请输入单词", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        WordText = text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void WordInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Ok_Click(sender, e);
        }
    }
}