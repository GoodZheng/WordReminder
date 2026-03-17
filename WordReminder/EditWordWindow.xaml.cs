using System.Windows;
using WordReminder.Models;

namespace WordReminder;

public partial class EditWordWindow : Window
{
    public Word EditedWord { get; private set; }

    public EditWordWindow(Word word)
    {
        InitializeComponent();
        EditedWord = new Word
        {
            Id = word.Id,
            Text = word.Text,
            Phonetic = word.Phonetic,
            PartOfSpeech = word.PartOfSpeech,
            Definition = word.Definition,
            Example = word.Example,
            DisplayOrder = word.DisplayOrder
        };

        WordTextBox.Text = EditedWord.Text;
        PhoneticTextBox.Text = EditedWord.Phonetic ?? "";
        PartOfSpeechTextBox.Text = EditedWord.PartOfSpeech ?? "";
        DefinitionTextBox.Text = EditedWord.Definition ?? "";
        ExampleTextBox.Text = EditedWord.Example ?? "";

        WordTextBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var text = WordTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            System.Windows.MessageBox.Show("单词不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        EditedWord.Text = text;
        EditedWord.Phonetic = string.IsNullOrWhiteSpace(PhoneticTextBox.Text) ? null : PhoneticTextBox.Text.Trim();
        EditedWord.PartOfSpeech = string.IsNullOrWhiteSpace(PartOfSpeechTextBox.Text) ? null : PartOfSpeechTextBox.Text.Trim();
        EditedWord.Definition = string.IsNullOrWhiteSpace(DefinitionTextBox.Text) ? null : DefinitionTextBox.Text.Trim();
        EditedWord.Example = string.IsNullOrWhiteSpace(ExampleTextBox.Text) ? null : ExampleTextBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}