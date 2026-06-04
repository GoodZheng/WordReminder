using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WordReminder.ViewModels;

namespace WordReminder.Views;

/// <summary>
/// 助手编辑对话框
/// </summary>
public partial class AssistantEditDialog : Controls.WindowBase
{
    private readonly AssistantEditViewModel _viewModel;

    public AssistantEditDialog(AssistantEditViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        // 根据编辑/新建模式设置标题
        TitleText = _viewModel.IsEditing ? "编辑助手" : "新建助手";
    }

    /// <summary>
    /// 图标按钮点击事件 - 显示图标选择弹出窗口
    /// </summary>
    private void IconButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (System.Windows.Controls.Button)sender;
        var popup = new Popup
        {
            PlacementTarget = button,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade
        };

        // 创建图标网格
        var grid = new Grid
        {
            Background = System.Windows.Media.Brushes.White,
            Margin = new Thickness(4)
        };

        var icons = AssistantEditViewModel.AvailableIcons;
        var columns = 6;
        var rows = (int)Math.Ceiling(icons.Length / (double)columns);

        for (int i = 0; i < rows; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (int i = 0; i < columns; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        }

        for (int i = 0; i < icons.Length; i++)
        {
            var iconButton = new System.Windows.Controls.Button
            {
                Content = icons[i],
                FontSize = 24,
                Width = 40,
                Height = 40,
                Padding = new Thickness(0),
                Margin = new Thickness(2),
                Style = (Style)FindResource("SecondaryButtonStyle"),
                Tag = icons[i]
            };

            iconButton.Click += (s, args) =>
            {
                _viewModel.Icon = (string)iconButton.Tag;
                popup.IsOpen = false;
            };

            Grid.SetRow(iconButton, i / columns);
            Grid.SetColumn(iconButton, i % columns);
            grid.Children.Add(iconButton);
        }

        // 添加边框
        var border = new Border
        {
            Child = grid,
            BorderBrush = System.Windows.Media.Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8)
        };

        popup.Child = border;
        popup.IsOpen = true;
    }

    /// <summary>
    /// 取消按钮点击
    /// </summary>
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 保存按钮点击
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.TrySave())
        {
            DialogResult = true;
            Close();
        }
    }
}
