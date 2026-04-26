using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WordReminder.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfMenuItem = System.Windows.Controls.MenuItem;

namespace WordReminder.Views;

public partial class TranslationWindow : Controls.WindowBase
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

    /// <summary>
    /// 单词名称双击：弹出上下文菜单
    /// </summary>
    private void WordName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement element)
        {
            e.Handled = true;

            // 获取当前单词的 ViewModel
            if (element.DataContext is not WordDetailViewModel wordDetail)
                return;

            // 获取菜单资源
            if (FindResource("WordContextMenu") is not WpfContextMenu contextMenu)
                return;

            // 通过 ViewModel 检查单词是否已存在
            if (DataContext is not TranslationViewModel viewModel)
                return;

            // 更新菜单项状态
            if (contextMenu.Items[0] is WpfMenuItem addMenuItem)
            {
                bool exists = wordDetail.Word != null && viewModel.IsWordInList(wordDetail.Word);
                addMenuItem.IsEnabled = !exists;
                addMenuItem.Header = exists ? $"\"{wordDetail.Word}\" 已在列表中" : "加入单词列表";
                addMenuItem.Tag = wordDetail;
            }

            // 弹出菜单
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }
    }

    /// <summary>
    /// 菜单项点击：添加单词
    /// </summary>
    private void AddWordMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfMenuItem menuItem || menuItem.Tag is not WordDetailViewModel wordDetail)
            return;

        if (DataContext is not TranslationViewModel viewModel)
            return;

        // 执行添加命令
        viewModel.AddWordCommand.Execute(wordDetail);

        // 播放反馈动画
        if (wordDetail.ShowFeedback)
        {
            PlayFeedbackAnimation(wordDetail);
        }
    }

    /// <summary>
    /// 播放反馈标签显示/隐藏动画
    /// </summary>
    private void PlayFeedbackAnimation(WordDetailViewModel wordDetail)
    {
        if (Content is not FrameworkElement rootContent)
            return;

        var border = FindVisualChild<Border>(rootContent, "FeedbackBadge", wordDetail);
        if (border == null)
            return;

        // 获取动画资源
        if (FindResource("FeedbackShowStoryboard") is not Storyboard storyboard)
            return;

        var clone = storyboard.Clone();
        Storyboard.SetTarget(clone, border);

        border.Visibility = Visibility.Visible;
        border.Opacity = 0.9;
        clone.Begin();

        // 动画结束后重置
        clone.Completed += (_, _) =>
        {
            border.Visibility = Visibility.Collapsed;
            border.Opacity = 0.9;
        };
    }

    /// <summary>
    /// 在视觉树中查找绑定到指定 DataContext 的命名元素
    /// </summary>
    private T? FindVisualChild<T>(DependencyObject parent, string name, object dataContext) where T : DependencyObject
    {
        if (parent is FrameworkElement fe && fe.Name == name && fe.DataContext == dataContext)
            return (T)(DependencyObject)fe;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindVisualChild<T>(child, name, dataContext);
            if (result != null)
                return result;
        }

        return null;
    }
}
