using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WordReminder.Models;
using WordReminder.Services;

namespace WordReminder;

public partial class AllWordsWindow : Window
{
    private readonly DatabaseService _databaseService;
    private readonly AIDictionaryService _aiService;
    private readonly ConfigService _configService;
    private List<Word> _words = new();
    private bool _isDragging = false;
    private int _dragStartIndex = -1;

    public event EventHandler? WordsChanged;

    public AllWordsWindow()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _configService = new ConfigService();
        _aiService = new AIDictionaryService(_configService);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadWords();
    }

    private void LoadWords()
    {
        _words = _databaseService.GetAllWords();
        WordsDataGrid.ItemsSource = null;
        WordsDataGrid.ItemsSource = _words;
        UpdateSelectedCount();
    }

    private void WordsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        var count = WordsDataGrid.SelectedItems.Count;
        SelectedCountText.Text = $"已选择：{count} 项";

        // 没有选中时禁用批量操作按钮
        BatchDeleteButton.IsEnabled = count > 0;
        BatchUpdateButton.IsEnabled = count > 0;
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        WordsDataGrid.SelectAll();
    }

    private void InvertSelection_Click(object sender, RoutedEventArgs e)
    {
        if (WordsDataGrid.ItemsSource == null) return;

        var allItems = WordsDataGrid.ItemsSource.Cast<object>().ToList();
        var selectedItems = WordsDataGrid.SelectedItems.Cast<object>().ToList();

        WordsDataGrid.SelectedItems.Clear();
        foreach (var item in allItems)
        {
            if (!selectedItems.Contains(item))
            {
                WordsDataGrid.SelectedItems.Add(item);
            }
        }
    }

    private void CancelSelection_Click(object sender, RoutedEventArgs e)
    {
        WordsDataGrid.SelectedItems.Clear();
    }

    private async void BatchDelete_Click(object sender, RoutedEventArgs e)
    {
        var selectedWords = WordsDataGrid.SelectedItems.Cast<Word>().ToList();
        if (selectedWords.Count == 0) return;

        var wordList = string.Join(", ", selectedWords.Take(10).Select(w => $"'{w.Text}'"));
        var moreText = selectedWords.Count > 10 ? $"等 {selectedWords.Count} 个单词" : "";

        var result = System.Windows.MessageBox.Show(
            $"确定要删除 {wordList}{moreText} 吗？\n\n此操作不可恢复！",
            "确认批量删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                foreach (var word in selectedWords)
                {
                    _databaseService.DeleteWord(word.Id);
                }
                LoadWords();
                WordsChanged?.Invoke(this, EventArgs.Empty);

                System.Windows.MessageBox.Show($"成功删除 {selectedWords.Count} 个单词！", "删除成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"批量删除失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BatchUpdate_Click(object sender, RoutedEventArgs e)
    {
        var selectedWords = WordsDataGrid.SelectedItems.Cast<Word>().ToList();
        if (selectedWords.Count == 0) return;

        BatchUpdateButton.IsEnabled = false;
        BatchUpdateButton.Content = "更新中...";

        try
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var word in selectedWords)
            {
                try
                {
                    // 从 AI 获取单词信息
                    var updatedWord = await _aiService.GetWordInfoAsync(word.Text);

                    if (updatedWord != null && !string.IsNullOrEmpty(updatedWord.Definition))
                    {
                        // 保留原 ID 和显示顺序，更新其他字段
                        updatedWord.Id = word.Id;
                        updatedWord.DisplayOrder = word.DisplayOrder;

                        _databaseService.UpdateWord(updatedWord);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch
                {
                    failCount++;
                }
            }

            LoadWords();
            WordsChanged?.Invoke(this, EventArgs.Empty);

            var message = $"更新完成！\n成功：{successCount} 个\n失败：{failCount} 个";
            System.Windows.MessageBox.Show(message, "批量更新完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"批量更新失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BatchUpdateButton.IsEnabled = true;
            BatchUpdateButton.Content = "批量更新";
        }
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is Word word)
        {
            button.IsEnabled = false;
            button.Content = "获取中...";

            try
            {
                // 从 AI 获取单词信息
                var updatedWord = await _aiService.GetWordInfoAsync(word.Text);

                if (updatedWord != null && !string.IsNullOrEmpty(updatedWord.Definition))
                {
                    // 保留原 ID 和显示顺序，更新其他字段
                    updatedWord.Id = word.Id;
                    updatedWord.DisplayOrder = word.DisplayOrder;

                    _databaseService.UpdateWord(updatedWord);
                    LoadWords();
                    WordsChanged?.Invoke(this, EventArgs.Empty);

                    System.Windows.MessageBox.Show($"单词 '{word.Text}' 更新成功！", "更新成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"未能从网络获取单词 '{word.Text}' 的信息，请检查网络连接。",
                        "更新失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"更新单词时出错：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                button.Content = "更新";
            }
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is Word word)
        {
            var result = System.Windows.MessageBox.Show(
                $"确定要删除单词 '{word.Text}' 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _databaseService.DeleteWord(word.Id);
                LoadWords();
                WordsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // 拖动排序相关
    private void WordsDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = GetDataGridRowFromMouse(e);
        if (row != null)
        {
            _dragStartIndex = WordsDataGrid.Items.IndexOf(row.Item);
            _isDragging = false;
        }
    }

    private void WordsDataGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _dragStartIndex >= 0)
        {
            var row = GetDataGridRowFromMouse(e);
            if (row != null && !_isDragging)
            {
                _isDragging = true;
                System.Windows.DragDrop.DoDragDrop(row, row.Item, System.Windows.DragDropEffects.Move);
                _isDragging = false;
                _dragStartIndex = -1;
            }
        }
    }

    private void WordsDataGrid_DragEnter(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = System.Windows.DragDropEffects.Move;
    }

    private void WordsDataGrid_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (_dragStartIndex < 0) return;

        var row = GetDataGridRowFromDrag(e);
        if (row != null)
        {
            var dropIndex = WordsDataGrid.Items.IndexOf(row.Item);
            if (dropIndex >= 0 && dropIndex != _dragStartIndex)
            {
                // 重新排列列表
                var item = _words[_dragStartIndex];
                _words.RemoveAt(_dragStartIndex);
                _words.Insert(dropIndex, item);

                // 更新数据库中的顺序
                var wordIds = _words.Select(w => w.Id).ToList();
                _databaseService.ReorderWords(wordIds);

                // 刷新显示
                LoadWords();
                WordsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private DataGridRow? GetDataGridRowFromMouse(System.Windows.Input.MouseEventArgs e)
    {
        var element = e.OriginalSource as DependencyObject;
        while (element != null)
        {
            if (element is DataGridRow row)
                return row;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private DataGridRow? GetDataGridRowFromDrag(System.Windows.DragEventArgs e)
    {
        var element = e.OriginalSource as DependencyObject;
        while (element != null)
        {
            if (element is DataGridRow row)
                return row;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return null;
    }
}
