# 翻译窗口双击添加单词功能实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在翻译窗口的单词详情区域实现双击单词名称弹出菜单，将单词加入数据库并显示反馈动画。

**Architecture:** 在 TranslationWindow.xaml 中为单词名称 TextBlock 添加 MouseDoubleClick 事件，弹出 ContextMenu。TranslationViewModel 提供 AddWordCommand 负责检查单词是否已存在并调用 DatabaseService 写入。WordDetailViewModel 增加 ShowFeedback 属性用于控制反馈动画。不新建自定义控件，直接在现有 ItemsControl ItemTemplate 中扩展。

**Tech Stack:** WPF (XAML), CommunityToolkit.Mvvm, SQLite (DatabaseService), Storyboard 动画

---

## 文件概览

| 文件 | 操作 | 职责 |
|------|------|------|
| `ViewModels/TranslationViewModel.cs` | 修改 | 添加 `AddWordCommand`、注入 `DatabaseService`、实现添加逻辑 |
| `ViewModels/TranslationResultViewModel.cs` | 修改 | 为 `WordDetailViewModel` 添加 `ShowFeedback` 属性（INotifyPropertyChanged） |
| `Views/TranslationWindow.xaml` | 修改 | 单词名称区域绑定双击事件、添加 ContextMenu 资源、添加反馈标签 |
| `Views/TranslationWindow.xaml.cs` | 修改 | 双击事件处理：获取 DataContext、弹出菜单、触发反馈动画 |

**决策说明：** 设计文档中提到可以新建 `Controls/WordDetailCard.xaml` 自定义控件。但经代码探索发现，当前项目的 Controls 目录仅包含 `WindowBase.cs`（基类）和 `HotKeyTextBox.cs`（特殊输入控件），没有通用的样式控件。且翻译窗口的单词卡片目前直接内联在 ItemTemplate 中，提取为独立控件会增加不必要的复杂度。因此改为**直接在现有 ItemTemplate 中扩展**，保持与现有代码模式一致。

---

### Task 1: 为 WordDetailViewModel 添加 ShowFeedback 属性

**Files:**
- Modify: `WordReminder/ViewModels/TranslationResultViewModel.cs`

- [ ] **Step 1: 修改 WordDetailViewModel 添加 ShowFeedback 属性**

`WordDetailViewModel` 当前是纯 POCO 类（不是 ViewModel），需要改为继承 `ObservableObject` 以支持 `ShowFeedback` 属性的通知绑定。读取当前文件，将 `WordDetailViewModel` 改为：

```csharp
/// <summary>
/// 单词详情 ViewModel
/// </summary>
public partial class WordDetailViewModel : ObservableObject
{
    public string? Word { get; }
    public string? Phonetic { get; }
    public string? PartOfSpeech { get; }
    public string? Definition { get; }
    public string? Example { get; }
    public string? ExampleTranslation { get; }

    [ObservableProperty]
    private bool _showFeedback;

    public WordDetailViewModel(AITranslationService.WordInfo word)
    {
        Word = word.Word;
        Phonetic = word.Phonetic;
        PartOfSpeech = word.PartOfSpeech;
        Definition = word.Definition;
        Example = word.Example;
        ExampleTranslation = word.ExampleTranslation;
    }

    public string DisplayHeader => !string.IsNullOrEmpty(Phonetic) ? $"{Word} {Phonetic}" : Word ?? "";
    public string DisplayDefinition => !string.IsNullOrEmpty(PartOfSpeech) ? $"[{PartOfSpeech}] {Definition}" : Definition ?? "";
}
```

关键改动：
1. 添加 `using CommunityToolkit.Mvvm.ComponentModel;`（如果尚未存在）
2. 类声明加 `partial` 关键字
3. 继承 `ObservableObject`
4. 添加 `[ObservableProperty] private bool _showFeedback;`

- [ ] **Step 2: 编译验证**

```bash
dotnet build WordReminder.slnx
```

Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add WordReminder/ViewModels/TranslationResultViewModel.cs
git commit -m "refactor: make WordDetailViewModel observable for feedback animation binding"
```

---

### Task 2: 在 TranslationViewModel 中添加 AddWordCommand

**Files:**
- Modify: `WordReminder/ViewModels/TranslationViewModel.cs`

- [ ] **Step 1: 注入 DatabaseService**

在构造函数中添加 `DatabaseService` 参数：

```csharp
private readonly AITranslationService _translationService;
private readonly ConfigService _configService;
private readonly DatabaseService _databaseService;

public TranslationViewModel(ConfigService configService, AITranslationService translationService, DatabaseService databaseService)
{
    _configService = configService;
    _translationService = translationService;
    _databaseService = databaseService;

    ShowLoading = true;
}
```

- [ ] **Step 2: 添加 AddWordCommand**

在 `TranslateAsync` 方法之后添加：

```csharp
/// <summary>
/// 将翻译结果中的单词加入单词列表
/// </summary>
[RelayCommand]
private async Task AddWordAsync(WordDetailViewModel wordDetail)
{
    if (wordDetail == null || string.IsNullOrWhiteSpace(wordDetail.Word))
        return;

    // 检查是否已存在
    if (_databaseService.WordExists(wordDetail.Word))
    {
        StatusText = $"单词 \"{wordDetail.Word}\" 已在列表中";
        return;
    }

    try
    {
        // 构建 Word 模型并写入
        var word = new Word
        {
            Text = wordDetail.Word,
            Phonetic = wordDetail.Phonetic,
            PartOfSpeech = wordDetail.PartOfSpeech,
            Definition = wordDetail.Definition,
            Example = wordDetail.Example
        };

        _databaseService.InsertWord(word);

        // 触发反馈动画
        wordDetail.ShowFeedback = true;
        StatusText = $"已添加 \"{wordDetail.Word}\"";

        // 1.5 秒后隐藏反馈
        await Task.Delay(1500);
        wordDetail.ShowFeedback = false;
    }
    catch (Exception ex)
    {
        StatusText = $"添加失败: {ex.Message}";
    }
}
```

- [ ] **Step 3: 添加必要的 using**

在文件顶部确认有 `using WordReminder.Models;`（已有 `using WordReminder.Services;`）。

- [ ] **Step 4: 编译验证**

```bash
dotnet build WordReminder.slnx
```

Expected: BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add WordReminder/ViewModels/TranslationViewModel.cs
git commit -m "feat: add AddWordCommand to TranslationViewModel with duplicate check and feedback"
```

---

### Task 3: 修改 TranslationWindow.xaml 添加双击、菜单和反馈

**Files:**
- Modify: `WordReminder/Views/TranslationWindow.xaml`

- [ ] **Step 1: 添加 ContextMenu 到 Window.Resources**

在现有的 `<Window.Resources>` 中添加 ContextMenu 资源，放在现有 converter 之后：

```xml
<Window.Resources>
    <converters:BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
    <converters:StringToVisibilityConverter x:Key="StringToVisibilityConverter"/>
    <converters:NullToVisibilityConverter x:Key="NullToVisibilityConverter"/>
    <converters:InverseBooleanConverter x:Key="InverseBooleanConverter"/>

    <!-- 单词详情上下文菜单 -->
    <ContextMenu x:Key="WordContextMenu" Style="{StaticResource FluentContextMenuStyle}">
        <MenuItem x:Name="AddWordMenuItem"
                  Header="加入单词列表"
                  Style="{StaticResource FluentMenuItemStyle}"
                  Click="AddWordMenuItem_Click"/>
    </ContextMenu>
</Window.Resources>
```

- [ ] **Step 2: 修改单词详情 ItemTemplate 中的单词名称区域**

找到单词详情的 ItemsControl（第 159-212 行），将原来的单词名称 TextBlock：

```xml
<TextBlock FontSize="16" FontWeight="Bold" Foreground="{StaticResource PrimaryBrush}">
    <Run Text="{Binding Word, Mode=OneWay}"/>
    <Run Text="{Binding Phonetic, Mode=OneWay}" Foreground="{StaticResource TextSecondaryBrush}" FontSize="14"/>
</TextBlock>
```

替换为带双击事件的版本：

```xml
<!-- 单词名称 + 音标（双击弹出菜单） -->
<TextBlock FontSize="16" FontWeight="Bold" Foreground="{StaticResource PrimaryBrush}"
           MouseLeftButtonDown="WordName_MouseLeftButtonDown"
           Cursor="Hand">
    <Run Text="{Binding Word, Mode=OneWay}"/>
    <Run Text="{Binding Phonetic, Mode=OneWay}" Foreground="{StaticResource TextSecondaryBrush}" FontSize="14"/>
</TextBlock>
```

- [ ] **Step 3: 在单词卡片 Border 中添加反馈标签**

找到单词详情的 Border（第 162 行），在其内部 StackPanel 的最前面添加反馈标签：

```xml
<Border BorderBrush="#DCDCDC" BorderThickness="1" CornerRadius="6" Padding="12" Margin="0,0,0,10" Background="White">
    <Grid>
        <!-- 添加成功反馈标签 -->
        <Border x:Name="FeedbackBadge"
                HorizontalAlignment="Right" VerticalAlignment="Top"
                Background="{StaticResource SuccessBrush}"
                CornerRadius="10" Padding="8,4"
                Visibility="Collapsed"
                Opacity="0.9"
                Panel.ZIndex="1">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="✓" Foreground="White" FontSize="12" FontWeight="Bold" Margin="0,0,4,0"/>
                <TextBlock Text="已添加" Foreground="White" FontSize="11"/>
            </StackPanel>
        </Border>

        <StackPanel>
            <!-- 原有内容保持不变 -->
```

然后在 StackPanel 闭合标签之后关闭 Grid：

```xml
        </StackPanel>
    </Grid>
</Border>
```

- [ ] **Step 4: 在 Window 级别添加 Storyboard 资源**

在 `<Window.Resources>` 中，在 ContextMenu 之后添加：

```xml
<!-- 反馈标签显示/隐藏动画 -->
<Storyboard x:Key="FeedbackShowStoryboard">
    <ObjectAnimationUsingKeyFrames Storyboard.TargetProperty="Visibility">
        <DiscreteObjectKeyFrame KeyTime="0:0:0" Value="{x:Static Visibility.Visible}"/>
    </ObjectAnimationUsingKeyFrames>
    <DoubleAnimationUsingKeyFrames Storyboard.TargetProperty="Opacity">
        <DoubleKeyFrame KeyTime="0:0:0" Value="0.9"/>
        <DoubleKeyFrame KeyTime="0:0:1.2" Value="0.9"/>
        <DoubleKeyFrame KeyTime="0:0:1.5" Value="0"/>
    </DoubleAnimationUsingKeyFrames>
</Storyboard>
```

- [ ] **Step 5: 编译验证**

```bash
dotnet build WordReminder.slnx
```

Expected: BUILD SUCCEEDED

- [ ] **Step 6: Commit**

```bash
git add WordReminder/Views/TranslationWindow.xaml
git commit -m "feat: add double-click context menu and feedback badge to translation word details"
```

---

### Task 4: 修改 TranslationWindow.xaml.cs 添加事件处理

**Files:**
- Modify: `WordReminder/Views/TranslationWindow.xaml.cs`

- [ ] **Step 1: 添加必要的 using 和事件处理**

读取当前 xaml.cs 文件，在现有代码后添加：

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WordReminder.Services;
using WordReminder.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

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
            if (FindResource("WordContextMenu") is not ContextMenu contextMenu)
                return;

            // 更新菜单项状态
            if (contextMenu.Items[0] is MenuItem addMenuItem)
            {
                bool exists = wordDetail.Word != null && _databaseService.WordExists(wordDetail.Word);
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
        if (sender is not MenuItem menuItem || menuItem.Tag is not WordDetailViewModel wordDetail)
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
        // 在视觉树中找到当前单词卡片对应的 FeedbackBadge
        // 通过遍历视觉树查找绑定到当前 wordDetail 的元素
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
            return (T)fe;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindVisualChild<T>(child, name, dataContext);
            if (result != null)
                return result;
        }

        return null;
    }

    private DatabaseService _databaseService =>
        ((App)System.Windows.Application.Current).Host.Services.GetRequiredService<DatabaseService>();
}
```

等等，这里有个问题——`App` 类的 `_host` 是 private 的，不能直接访问。让我检查 App 类是否有公开暴露 Host 的方式。

查看 `App.xaml.cs`，`_host` 是 `private IHost? _host;`，没有公开暴露。需要调整方案。

**修正方案：** 改为在 TranslationWindow 的构造函数中接收 DatabaseService，或者通过 ViewModel 暴露。更简洁的做法是**让 ViewModel 持有 DatabaseService 的引用**（已在 Task 2 中完成），然后在 xaml.cs 中通过 ViewModel 调用。

修正后的 xaml.cs：

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WordReminder.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

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
            if (FindResource("WordContextMenu") is not ContextMenu contextMenu)
                return;

            // 通过 ViewModel 检查单词是否已存在
            if (DataContext is not TranslationViewModel viewModel)
                return;

            // 更新菜单项状态
            if (contextMenu.Items[0] is MenuItem addMenuItem)
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
        if (sender is not MenuItem menuItem || menuItem.Tag is not WordDetailViewModel wordDetail)
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
            return (T)fe;

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
```

- [ ] **Step 2: 在 TranslationViewModel 中添加 IsWordInList 辅助方法**

在 `AddWordAsync` 方法之后添加：

```csharp
/// <summary>
/// 检查单词是否已在列表中（供 View 层调用以更新菜单状态）
/// </summary>
public bool IsWordInList(string word)
{
    return _databaseService.WordExists(word);
}
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build WordReminder.slnx
```

Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add WordReminder/Views/TranslationWindow.xaml.cs WordReminder/ViewModels/TranslationViewModel.cs
git commit -m "feat: wire up double-click event handler and context menu for word addition"
```

---

### Task 5: 手动测试与修复

**Files:**
- All modified files

- [ ] **Step 1: 运行应用并测试基本流程**

```bash
dotnet run --project WordReminder
```

测试步骤：
1. 打开翻译窗口（通过主窗口右键菜单或快捷键）
2. 输入英文句子（如 "The beautiful sunset reminded me of our journey."）
3. 点击翻译或按回车
4. 等待翻译完成，查看"难词解释"区域
5. 双击某个单词的名称行（如 "sunset /ˈsʌnˌsɛt/"）
6. 验证弹出菜单显示"加入单词列表"
7. 点击菜单项
8. 验证卡片右上角出现绿色"✓ 已添加"标签
9. 验证标签在 1.5 秒后淡出消失
10. 验证底部状态栏显示"已添加 'sunset'"

- [ ] **Step 2: 测试重复添加**

1. 再次双击同一个单词
2. 验证菜单项显示为灰色禁用状态，文字为 `"sunset" 已在列表中`
3. 验证菜单项不可点击

- [ ] **Step 3: 测试不同单词**

1. 双击另一个未添加的单词
2. 验证可以正常添加
3. 验证反馈动画正常

- [ ] **Step 4: 测试边界情况**

1. 输入纯中文翻译，验证不会出现单词详情区域（zh2en 模式显示"多种翻译"）
2. 输入英文单词（非句子），验证 en2zh word 模式下的添加功能
3. 快速连续双击同一单词，验证不会出现重复添加（数据库 ON CONFLICT 保护）

- [ ] **Step 5: 修复发现的问题**

根据测试结果修复任何 UI 或逻辑问题，每个修复单独提交。

- [ ] **Step 6: 最终提交**

```bash
git add -A
git commit -m "fix: address issues found during manual testing"
```

---

### Task 6: 更新 .gitignore（如果需要）

**Files:**
- `.gitignore`

- [ ] **Step 1: 确认 .superpowers/brainstorm 目录是否需要忽略**

视觉伴侣生成的 brainstorm 文件不应提交。检查 `.gitignore` 是否已包含：

```
.superpowers/brainstorm/
```

如果没有，添加：

```bash
echo ".superpowers/brainstorm/" >> .gitignore
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build WordReminder.slnx
```

Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add .gitignore
git commit -m "chore: ignore brainstorm artifacts"
```
