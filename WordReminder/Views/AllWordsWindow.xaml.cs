using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;
using WordReminder.ViewModels;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;

namespace WordReminder.Views;

public partial class AllWordsWindow : Window
{
    private AllWordsViewModel? _viewModel;
    private int _dragStartIndex = -1;
    private bool _isDragging = false;

    public AllWordsWindow()
    {
        InitializeComponent();

        // 注册消息监听
        WeakReferenceMessenger.Default.Register<CloseAllWordsMessage>(this, (_, _) => Close());
        WeakReferenceMessenger.Default.Register<SelectAllWordsMessage>(this, (_, _) => WordsDataGrid.SelectAll());
        WeakReferenceMessenger.Default.Register<ClearSelectionMessage>(this, (_, _) => WordsDataGrid.SelectedItems.Clear());
        WeakReferenceMessenger.Default.Register<InvertSelectionMessage>(this, ReceiveInvertSelection);

        Loaded += OnLoaded;
        Activated += (_, _) =>
        {
            if (_viewModel != null)
            {
                // 窗口激活时重新加载
                _viewModel.LoadWords();
            }
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as AllWordsViewModel;
        if (_viewModel != null)
        {
            _viewModel.LoadWords();
        }
    }

    private void WordsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.UpdateSelectedCount(WordsDataGrid.SelectedItems.Count);
        }
    }

    private void ReceiveInvertSelection(object recipient, InvertSelectionMessage message)
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

    private void WordsDataGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _dragStartIndex >= 0)
        {
            var row = GetDataGridRowFromMouse(e);
            if (row != null && !_isDragging)
            {
                _isDragging = true;
                DragDrop.DoDragDrop(row, row.Item, DragDropEffects.Move);
                _isDragging = false;
                _dragStartIndex = -1;
            }
        }
    }

    private void WordsDataGrid_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
    }

    private void WordsDataGrid_Drop(object sender, DragEventArgs e)
    {
        if (_dragStartIndex < 0 || _viewModel == null) return;

        var row = GetDataGridRowFromDrag(e);
        if (row != null)
        {
            var dropIndex = WordsDataGrid.Items.IndexOf(row.Item);
            _viewModel.ReorderWords(_dragStartIndex, dropIndex);
        }
    }

    private DataGridRow? GetDataGridRowFromMouse(MouseEventArgs e)
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

    private DataGridRow? GetDataGridRowFromDrag(DragEventArgs e)
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
