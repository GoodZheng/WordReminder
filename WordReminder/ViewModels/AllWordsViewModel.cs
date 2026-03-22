using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;
using WordReminder.Models;
using WordReminder.Services;
using MessageBox = System.Windows.MessageBox;

namespace WordReminder.ViewModels;

/// <summary>
/// 所有单词窗口 ViewModel - 管理单词列表和批量操作
/// </summary>
public partial class AllWordsViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly AIDictionaryService _aiService;
    private readonly ConfigService _configService;
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private List<Word> _words = new();

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private bool _isBatchDeleteEnabled;

    [ObservableProperty]
    private bool _isBatchUpdateEnabled;

    [ObservableProperty]
    private string _batchUpdateButtonText = "批量更新";

    [ObservableProperty]
    private string _selectedCountText = "已选择：0 项";

    // 拖动相关
    [ObservableProperty]
    private bool _isDragging;

    [ObservableProperty]
    private int _dragStartIndex = -1;

    public AllWordsViewModel(
        DatabaseService databaseService,
        ConfigService configService,
        AIDictionaryService aiService,
        IMessenger messenger)
    {
        _databaseService = databaseService;
        _configService = configService;
        _aiService = aiService;
        _messenger = messenger;

        LoadWords();
    }

    /// <summary>
    /// 加载单词
    /// </summary>
    public void LoadWords()
    {
        Words = _databaseService.GetAllWords();
    }

    /// <summary>
    /// 更新选择计数
    /// </summary>
    public void UpdateSelectedCount(int count)
    {
        SelectedCount = count;
        SelectedCountText = $"已选择：{count} 项";
        IsBatchDeleteEnabled = count > 0;
        IsBatchUpdateEnabled = count > 0;
    }

    /// <summary>
    /// 选择全部命令
    /// </summary>
    [RelayCommand]
    public void SelectAll()
    {
        // 通过 View 执行
        _messenger.Send(new SelectAllWordsMessage());
    }

    /// <summary>
    /// 反选命令
    /// </summary>
    [RelayCommand]
    public void InvertSelection()
    {
        _messenger.Send(new InvertSelectionMessage());
    }

    /// <summary>
    /// 取消选择命令
    /// </summary>
    [RelayCommand]
    public void CancelSelection()
    {
        _messenger.Send(new ClearSelectionMessage());
    }

    /// <summary>
    /// 批量删除命令
    /// </summary>
    [RelayCommand]
    private async Task BatchDeleteAsync(object parameter)
    {
        var selectedWords = GetSelectedWords(parameter);
        if (selectedWords.Count == 0) return;

        var wordList = string.Join(", ", selectedWords.Take(10).Select(w => $"'{w.Text}'"));
        var moreText = selectedWords.Count > 10 ? $"等 {selectedWords.Count} 个单词" : "";

        var result = MessageBox.Show(
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
                _messenger.Send(new WordsChangedMessage());

                MessageBox.Show($"成功删除 {selectedWords.Count} 个单词！", "删除成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"批量删除失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// 批量更新命令
    /// </summary>
    [RelayCommand]
    private async Task BatchUpdateAsync(object parameter)
    {
        var selectedWords = GetSelectedWords(parameter);
        if (selectedWords.Count == 0) return;

        IsBatchUpdateEnabled = false;
        BatchUpdateButtonText = "更新中...";

        try
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var word in selectedWords)
            {
                try
                {
                    var updatedWord = await _aiService.GetWordInfoAsync(word.Text);

                    if (updatedWord != null && !string.IsNullOrEmpty(updatedWord.Definition))
                    {
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
            _messenger.Send(new WordsChangedMessage());

            var message = $"更新完成！\n成功：{successCount} 个\n失败：{failCount} 个";
            MessageBox.Show(message, "批量更新完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"批量更新失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBatchUpdateEnabled = true;
            BatchUpdateButtonText = "批量更新";
        }
    }

    /// <summary>
    /// 更新单个单词命令
    /// </summary>
    [RelayCommand]
    private async Task UpdateWordAsync(Word word)
    {
        if (word == null) return;

        try
        {
            var updatedWord = await _aiService.GetWordInfoAsync(word.Text);

            if (updatedWord != null && !string.IsNullOrEmpty(updatedWord.Definition))
            {
                updatedWord.Id = word.Id;
                updatedWord.DisplayOrder = word.DisplayOrder;

                _databaseService.UpdateWord(updatedWord);
                LoadWords();
                _messenger.Send(new WordsChangedMessage());

                MessageBox.Show($"单词 '{word.Text}' 更新成功！", "更新成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"未能从网络获取单词 '{word.Text}' 的信息，请检查网络连接。",
                    "更新失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"更新单词时出错：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 删除单词命令
    /// </summary>
    [RelayCommand]
    private void DeleteWord(Word word)
    {
        if (word == null) return;

        var result = MessageBox.Show(
            $"确定要删除单词 '{word.Text}' 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _databaseService.DeleteWord(word.Id);
            LoadWords();
            _messenger.Send(new WordsChangedMessage());
        }
    }

    /// <summary>
    /// 从参数中获取选中的单词列表
    /// </summary>
    private List<Word> GetSelectedWords(object parameter)
    {
        if (parameter is IList<Word> words)
        {
            return new List<Word>(words);
        }
        if (parameter is System.Collections.IList list)
        {
            var result = new List<Word>();
            foreach (var item in list)
            {
                if (item is Word word)
                    result.Add(word);
            }
            return result;
        }
        return new List<Word>();
    }

    /// <summary>
    /// 关闭命令
    /// </summary>
    [RelayCommand]
    public void Close()
    {
        _messenger.Send(new CloseAllWordsMessage());
    }

    /// <summary>
    /// 重新排序单词
    /// </summary>
    public void ReorderWords(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex) return;

        var item = Words[fromIndex];
        Words.RemoveAt(fromIndex);
        Words.Insert(toIndex, item);

        var wordIds = Words.Select(w => w.Id).ToList();
        _databaseService.ReorderWords(wordIds);

        LoadWords();
        _messenger.Send(new WordsChangedMessage());
    }
}
