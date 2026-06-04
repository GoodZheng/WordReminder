# 翻译历史记录功能实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在翻译界面中增加持久化的翻译历史记录，采用左右分栏布局，支持分页和删除。

**Architecture:** 新增 `TranslationHistoryService` 处理 SQLite 数据持久化，新增 `HistoryItemViewModel` 作为列表展示模型，改造 `TranslationViewModel` 增加历史相关属性和命令，改造 `TranslationWindow.xaml` 为左右分栏布局。

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, MVVM

---

### File Structure

| 文件 | 操作 | 职责 |
|------|------|------|
| `Models/TranslationHistoryEntry.cs` | 创建 | 数据库实体模型 |
| `Services/TranslationHistoryService.cs` | 创建 | 数据库 CRUD 操作 |
| `ViewModels/HistoryItemViewModel.cs` | 创建 | 历史列表项 ViewModel |
| `ViewModels/TranslationViewModel.cs` | 修改 | 新增历史相关属性、命令、逻辑 |
| `Views/TranslationWindow.xaml` | 修改 | 改造为左右分栏 + 分页布局 |
| `Views/TranslationWindow.xaml.cs` | 修改 | 新增历史列表选中事件处理 |
| `Bootstrapper.cs` | 修改 | 注册 `TranslationHistoryService` |

---

### Task 1: 创建数据模型

**Files:**
- Create: `WordReminder/Models/TranslationHistoryEntry.cs`

- [ ] **Step 1: 创建 TranslationHistoryEntry 模型**

```csharp
namespace WordReminder.Models;

/// <summary>
/// 翻译历史记录数据库实体
/// </summary>
public class TranslationHistoryEntry
{
    public int Id { get; set; }
    public string InputText { get; set; } = string.Empty;
    public string? TranslatedText { get; set; }
    public string? FullJson { get; set; }
    public string? TextType { get; set; }
    public string? Direction { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build WordReminder/WordReminder.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add WordReminder/Models/TranslationHistoryEntry.cs
git commit -m "feat: add TranslationHistoryEntry model for persistent history storage"
```

---

### Task 2: 创建翻译历史服务

**Files:**
- Create: `WordReminder/Services/TranslationHistoryService.cs`

- [ ] **Step 1: 创建 TranslationHistoryService**

```csharp
using Microsoft.Data.Sqlite;
using WordReminder.Models;

namespace WordReminder.Services;

/// <summary>
/// 翻译历史记录服务 - 持久化翻译历史到 SQLite
/// </summary>
public class TranslationHistoryService
{
    private readonly string _connectionString;

    public TranslationHistoryService()
    {
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordReminder");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        var dbPath = Path.Combine(dataDir, "words.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeTable();
    }

    private void InitializeTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            CREATE TABLE IF NOT EXISTS TranslationHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                InputText TEXT NOT NULL,
                TranslatedText TEXT,
                FullJson TEXT,
                TextType TEXT,
                Direction TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 插入一条翻译历史
    /// </summary>
    public int Insert(string inputText, string? translatedText, string? fullJson, string? textType, string? direction)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            INSERT INTO TranslationHistory (InputText, TranslatedText, FullJson, TextType, Direction)
            VALUES (@InputText, @TranslatedText, @FullJson, @TextType, @Direction);
            SELECT last_insert_rowid();";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@InputText", inputText);
        cmd.Parameters.AddWithValue("@TranslatedText", translatedText ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@FullJson", fullJson ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@TextType", textType ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Direction", direction ?? (object)DBNull.Value);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// 分页查询历史记录（按时间倒序）
    /// </summary>
    public (List<TranslationHistoryEntry> Items, int Total) GetPaged(int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // 查询总数
        using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM TranslationHistory", connection);
        var total = Convert.ToInt32(countCmd.ExecuteScalar());

        // 查询分页数据
        var sql = @"
            SELECT Id, InputText, TranslatedText, FullJson, TextType, Direction, CreatedAt
            FROM TranslationHistory
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        cmd.Parameters.AddWithValue("@Offset", offset);

        var items = new List<TranslationHistoryEntry>();
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            items.Add(new TranslationHistoryEntry
            {
                Id = reader.GetInt32(0),
                InputText = reader.GetString(1),
                TranslatedText = reader.IsDBNull(2) ? null : reader.GetString(2),
                FullJson = reader.IsDBNull(3) ? null : reader.GetString(3),
                TextType = reader.IsDBNull(4) ? null : reader.GetString(4),
                Direction = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = reader.GetDateTime(6)
            });
        }

        return (items, total);
    }

    /// <summary>
    /// 获取历史记录总数
    /// </summary>
    public int GetTotalCount()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = new SqliteCommand("SELECT COUNT(*) FROM TranslationHistory", connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// 删除一条历史记录
    /// </summary>
    public bool Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "DELETE FROM TranslationHistory WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        return cmd.ExecuteNonQuery() > 0;
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build WordReminder/WordReminder.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add WordReminder/Services/TranslationHistoryService.cs
git commit -m "feat: add TranslationHistoryService with CRUD operations"
```

---

### Task 3: 创建 HistoryItemViewModel

**Files:**
- Create: `WordReminder/ViewModels/HistoryItemViewModel.cs`

- [ ] **Step 1: 创建 HistoryItemViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using WordReminder.Models;

namespace WordReminder.ViewModels;

/// <summary>
/// 翻译历史列表项 ViewModel
/// </summary>
public partial class HistoryItemViewModel : ObservableObject
{
    public int Id { get; }
    public string InputText { get; }
    public string? TranslatedText { get; }
    public string? FullJson { get; }
    public DateTime CreatedAt { get; }

    /// <summary>
    /// 截断后的译文摘要（最多 50 字符）
    /// </summary>
    public string? TranslatedTextPreview =>
        string.IsNullOrEmpty(TranslatedText) ? null :
        TranslatedText.Length > 50 ? TranslatedText[..50] + "…" : TranslatedText;

    /// <summary>
    /// 格式化的创建时间（如 "2026-04-26 14:30"）
    /// </summary>
    public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm");

    public HistoryItemViewModel(TranslationHistoryEntry entry)
    {
        Id = entry.Id;
        InputText = entry.InputText;
        TranslatedText = entry.TranslatedText;
        FullJson = entry.FullJson;
        CreatedAt = entry.CreatedAt;
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build WordReminder/WordReminder.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add WordReminder/ViewModels/HistoryItemViewModel.cs
git commit -m "feat: add HistoryItemViewModel for history list display"
```

---

### Task 4: 注册服务并改造 TranslationViewModel

**Files:**
- Modify: `WordReminder/Bootstrapper.cs`
- Modify: `WordReminder/ViewModels/TranslationViewModel.cs`

- [ ] **Step 1: 在 Bootstrapper 中注册 TranslationHistoryService**

在 `Bootstrapper.cs` 中，找到 `services.AddSingleton<AITranslationService>();` 这一行，在其下方添加：

```csharp
services.AddSingleton<TranslationHistoryService>();
```

完整的相关区域应该变成：
```csharp
services.AddSingleton<AITranslationService>();
services.AddSingleton<TranslationHistoryService>();
services.AddSingleton<AIConnectivityTestService>();
```

- [ ] **Step 2: 改造 TranslationViewModel - 构造函数和新增属性**

打开 `WordReminder/ViewModels/TranslationViewModel.cs`，修改如下：

1. 在构造函数中注入 `TranslationHistoryService`：

```csharp
public partial class TranslationViewModel : ViewModelBase
{
    private readonly AITranslationService _translationService;
    private readonly ConfigService _configService;
    private readonly DatabaseService _databaseService;
    private readonly TranslationHistoryService _historyService;  // 新增

    public TranslationViewModel(ConfigService configService, AITranslationService translationService, DatabaseService databaseService, TranslationHistoryService historyService)
    {
        _configService = configService;
        _translationService = translationService;
        _databaseService = databaseService;
        _historyService = historyService;

        ShowLoading = true;
        LoadHistory();  // 新增：加载历史
    }
```

2. 新增属性（放在现有属性之后）：

```csharp
    [ObservableProperty]
    private ObservableCollection<HistoryItemViewModel> _historyItems = new();

    [ObservableProperty]
    private HistoryItemViewModel? _selectedHistoryItem;

    [ObservableProperty]
    private int _totalHistoryCount;

    [ObservableProperty]
    private int _currentPage = 1;

    private const int HistoryPageSize = 10;
```

文件顶部需要添加 using：
```csharp
using System.Collections.ObjectModel;
using System.Text.Json;
```

- [ ] **Step 3: 新增 LoadHistory 方法**

在 `TranslationViewModel` 中添加：

```csharp
    /// <summary>
    /// 加载历史列表（当前页）
    /// </summary>
    private void LoadHistory()
    {
        var (items, total) = _historyService.GetPaged(CurrentPage, HistoryPageSize);
        HistoryItems = new ObservableCollection<HistoryItemViewModel>(items.Select(i => new HistoryItemViewModel(i)));
        TotalHistoryCount = total;
        OnPropertyChanged(nameof(PageCount));
    }

    /// <summary>
    /// 总页数
    /// </summary>
    public int PageCount => (int)Math.Ceiling((double)TotalHistoryCount / HistoryPageSize);
```

- [ ] **Step 4: 新增命令**

在 `TranslationViewModel` 中添加命令：

```csharp
    /// <summary>
    /// 选中历史项
    /// </summary>
    [RelayCommand]
    private void SelectHistory(HistoryItemViewModel item)
    {
        if (item == null || string.IsNullOrEmpty(item.FullJson))
            return;

        try
        {
            var jsonDoc = JsonDocument.Parse(item.FullJson);
            var result = AITranslationService.DeserializeTranslationResult(jsonDoc.RootElement, item.InputText);

            TranslationResult = new TranslationResultViewModel(result);
            ShowLoading = false;
            ShowError = false;
            StatusText = "查看历史记录";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"历史记录数据损坏: {ex.Message}";
            ShowError = true;
            ShowLoading = false;
            StatusText = "查看历史失败";
        }
    }

    /// <summary>
    /// 删除历史项
    /// </summary>
    [RelayCommand]
    private void DeleteHistory(HistoryItemViewModel item)
    {
        if (item == null) return;

        _historyService.Delete(item.Id);

        // 如果删除的是当前选中的，清空右侧显示
        if (SelectedHistoryItem?.Id == item.Id)
        {
            SelectedHistoryItem = null;
            TranslationResult = null;
            ShowLoading = true;
            StatusText = "就绪";
        }

        LoadHistory();
        StatusText = "已删除历史记录";
    }

    /// <summary>
    /// 上一页
    /// </summary>
    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            SelectedHistoryItem = null;
            LoadHistory();
        }
    }

    /// <summary>
    /// 下一页
    /// </summary>
    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < PageCount)
        {
            CurrentPage++;
            SelectedHistoryItem = null;
            LoadHistory();
        }
    }

    /// <summary>
    /// CanExecute for PreviousPageCommand
    /// </summary>
    public bool CanPreviousPage => CurrentPage > 1;

    /// <summary>
    /// CanExecute for NextPageCommand
    /// </summary>
    public bool CanNextPage => CurrentPage < PageCount;
```

注意：`CanPreviousPage` 和 `CanNextPage` 需要在 `CurrentPage` 和 `TotalHistoryCount` 变化时通知 UI 更新。需要添加 `OnPropertyChanged` 调用。修改 `LoadHistory` 方法末尾：

```csharp
    private void LoadHistory()
    {
        var (items, total) = _historyService.GetPaged(CurrentPage, HistoryPageSize);
        HistoryItems = new ObservableCollection<HistoryItemViewModel>(items.Select(i => new HistoryItemViewModel(i)));
        TotalHistoryCount = total;
        OnPropertyChanged(nameof(PageCount));
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }
```

同时命令需要设置 `CanExecute`：

```csharp
    [RelayCommand(CanExecute = nameof(CanPreviousPage))]
    private void PreviousPage()

    [RelayCommand(CanExecute = nameof(CanNextPage))]
    private void NextPage()
```

- [ ] **Step 5: 修改 TranslateAsync 方法 - 翻译成功后保存历史**

在 `TranslateAsync` 方法中，找到翻译成功后保存结果的代码块（`TranslationResult = new TranslationResultViewModel(result);` 那一块），替换为：

```csharp
            if (result != null)
            {
                TranslationResult = new TranslationResultViewModel(result);
                ShowLoading = false;
                ShowError = false;
                StatusText = "翻译完成";

                // 保存翻译历史
                try
                {
                    var fullJson = SerializeTranslationResult(result);
                    _historyService.Insert(
                        inputText: text,
                        translatedText: result.TranslatedText,
                        fullJson: fullJson,
                        textType: result.Type,
                        direction: result.Direction);

                    // 刷新历史列表（回到第一页）
                    CurrentPage = 1;
                    LoadHistory();
                }
                catch (Exception ex)
                {
                    // 历史保存失败不影响翻译结果展示
                    StatusText = $"翻译完成，但历史保存失败: {ex.Message}";
                }
            }
```

- [ ] **Step 6: 新增 SerializeTranslationResult 方法**

在 `TranslationViewModel` 中添加：

```csharp
    /// <summary>
    /// 将翻译结果序列化为 JSON 存储到数据库
    /// </summary>
    private static string SerializeTranslationResult(AITranslationService.TranslationResult result)
    {
        return JsonSerializer.Serialize(result);
    }
```

- [ ] **Step 7: 编译验证**

```bash
dotnet build WordReminder/WordReminder.csproj
```

Expected: Build succeeds.

- [ ] **Step 8: Commit**

```bash
git add WordReminder/Bootstrapper.cs WordReminder/ViewModels/TranslationViewModel.cs
git commit -m "feat: integrate TranslationHistoryService into TranslationViewModel"
```

---

### Task 5: 改造 TranslationWindow.xaml 为左右分栏

**Files:**
- Modify: `WordReminder/Views/TranslationWindow.xaml`

- [ ] **Step 1: 修改窗口尺寸**

将 TranslationWindow.xaml 开头的属性修改为：

```xml
<controls:WindowBase x:Class="WordReminder.Views.TranslationWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:converters="clr-namespace:WordReminder.Converters"
        xmlns:controls="clr-namespace:WordReminder.Controls"
        TitleText="万能翻译"
        Width="800"
        Height="550"
        CanResize="True"
        MinWidth="750"
        MinHeight="500">
```

- [ ] **Step 2: 修改 Grid 列定义**

将现有的 `<Grid Margin="16">` 替换为三列布局：

```xml
    <Grid Margin="0">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="220"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
```

注意：输入区域跨两列，所以需要 `Grid.ColumnSpan="2"`。

- [ ] **Step 3: 修改输入区域（跨两列）**

输入区域（原来的 Grid.Row="0" 和 Row="1"）改为合并到一行，并添加 `Grid.ColumnSpan="2"`：

```xml
        <!-- 输入区域 -->
        <Border Grid.Row="0" Grid.ColumnSpan="2" Padding="16,12" BorderBrush="{StaticResource SurfaceAltBrush}" BorderThickness="0,0,0,1">
            <StackPanel>
                <TextBlock Text="请输入要翻译的文本（支持中英文互翻）" FontSize="12" Foreground="{StaticResource TextSecondaryBrush}" Margin="0,0,0,5"/>
                <TextBox Height="60"
                         Name="InputTextBox"
                         TextWrapping="Wrap"
                         AcceptsReturn="True"
                         VerticalScrollBarVisibility="Auto"
                         FontSize="14"
                         Padding="8"
                         PreviewKeyDown="InputTextBox_PreviewKeyDown"
                         Text="{Binding InputText, UpdateSourceTrigger=PropertyChanged}"/>
                <Button Content="翻译"
                        Width="100"
                        Height="32"
                        HorizontalAlignment="Left"
                        Command="{Binding TranslateCommand}"
                        IsEnabled="{Binding IsTranslating, Converter={StaticResource InverseBooleanConverter}}"
                        Margin="0,8,0,0" Style="{StaticResource PrimaryButtonStyle}"/>
            </StackPanel>
        </Border>
```

- [ ] **Step 4: 新增左侧历史列表**

在主内容区域（Grid.Row="1", Grid.Column="0"）添加：

```xml
        <!-- 左侧历史列表 -->
        <Border Grid.Row="1" Grid.Column="0" BorderBrush="{StaticResource SurfaceAltBrush}" BorderThickness="0,0,1,0" Background="{StaticResource SurfaceAltBrush}">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- 历史标题 -->
                <TextBlock Grid.Row="0" Text="翻译历史" FontSize="12" FontWeight="SemiBold" Foreground="{StaticResource TextSecondaryBrush}" Padding="12,8" Background="{StaticResource SurfaceAltBrush}"/>

                <!-- 历史列表 -->
                <ListBox Grid.Row="1"
                         ItemsSource="{Binding HistoryItems}"
                         SelectedItem="{Binding SelectedHistoryItem}"
                         BorderThickness="0"
                         Background="Transparent"
                         ScrollViewer.VerticalScrollBarVisibility="Auto"
                         SelectionMode="Single">
                    <ListBox.ItemContainerStyle>
                        <Style TargetType="ListBoxItem">
                            <Setter Property="Padding" Value="12,6"/>
                            <Setter Property="BorderThickness" Value="3,0,0,0"/>
                            <Setter Property="BorderBrush" Value="Transparent"/>
                            <Setter Property="Cursor" Value="Hand"/>
                            <Setter Property="Template">
                                <Setter.Value>
                                    <ControlTemplate TargetType="ListBoxItem">
                                        <Border Background="{TemplateBinding Background}"
                                                BorderBrush="{TemplateBinding BorderBrush}"
                                                BorderThickness="{TemplateBinding BorderThickness}"
                                                Padding="{TemplateBinding Padding}">
                                            <ContentPresenter/>
                                        </Border>
                                        <ControlTemplate.Triggers>
                                            <Trigger Property="IsSelected" Value="True">
                                                <Setter Property="Background" Value="{StaticResource PrimaryBgBrush}"/>
                                                <Setter Property="BorderBrush" Value="{StaticResource PrimaryBrush}"/>
                                            </Trigger>
                                            <Trigger Property="IsMouseOver" Value="True">
                                                <Setter Property="Background" Value="#E8F0FE"/>
                                            </Trigger>
                                        </ControlTemplate.Triggers>
                                    </ControlTemplate>
                                </Setter.Value>
                            </Setter>
                        </Style>
                    </ListBox.ItemContainerStyle>
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel>
                                <TextBlock Text="{Binding InputText}" FontWeight="SemiBold" FontSize="13"
                                           TextTrimming="CharacterEllipsis" Foreground="{StaticResource TextPrimaryBrush}"/>
                                <TextBlock Text="{Binding TranslatedTextPreview}" FontSize="12"
                                           TextTrimming="CharacterEllipsis" Foreground="{StaticResource TextSecondaryBrush}" Margin="0,2,0,0"/>
                                <TextBlock Text="{Binding CreatedAtFormatted}" FontSize="10"
                                           Foreground="{StaticResource TextSecondaryBrush}" Opacity="0.7" Margin="0,2,0,0"/>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>

                <!-- 空状态提示 -->
                <TextBlock Grid.Row="1" Text="暂无历史记录" FontSize="12" Foreground="{StaticResource TextSecondaryBrush}"
                           HorizontalAlignment="Center" VerticalAlignment="Center"
                           Visibility="{Binding HistoryItems.Count, Converter={StaticResource NullToVisibilityConverter}, ConverterParameter=Invert}"/>
            </Grid>
        </Border>
```

注意：需要在 Resources 中添加一个 `InverseCountToVisibilityConverter` 或者用代码方式处理空状态。更简单的方式是用一个 TextBlock 绑定 `HistoryItems.Count`，在 ViewModel 中添加 `bool HasHistoryItems => HistoryItems.Count > 0` 属性，然后用 `BooleanToVisibilityConverter` 加 `InverseBooleanConverter` 组合。

更简单的方案：在 ViewModel 中添加：
```csharp
    public bool HasHistoryItems => HistoryItems.Count > 0;
```

并在 ListBox 下方添加：
```xml
                <TextBlock Grid.Row="1" Text="暂无历史记录" FontSize="12" Foreground="{StaticResource TextSecondaryBrush}"
                           HorizontalAlignment="Center" VerticalAlignment="Center">
                    <TextBlock.Visibility>
                        <MultiBinding Converter="{StaticResource BooleanToVisibilityConverter}">
                            <!-- 使用 HasHistoryItems 的否定 -->
                        </MultiBinding>
                    </TextBlock.Visibility>
                </TextBlock>
```

最简单方案：在 ViewModel 添加 `bool ShowHistoryEmpty => HistoryItems.Count == 0;` 并通知变化，然后直接绑定。

- [ ] **Step 5: 修改右侧内容区域**

将原来的结果区域（Grid.Row="2"）改为 Grid.Row="1", Grid.Column="1"，保持内容不变：

```xml
        <!-- 右侧内容区域 -->
        <Border Grid.Row="1" Grid.Column="1" Padding="16" Background="White">
            <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
                <StackPanel>
                    <!-- 加载提示 -->
                    <TextBlock Text="{Binding LoadingText}"
                               Foreground="{StaticResource TextSecondaryBrush}"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center"
                               FontSize="14"
                               Visibility="{Binding ShowLoading, Converter={StaticResource BooleanToVisibilityConverter}}"/>

                    <!-- 错误消息 -->
                    <TextBox Text="{Binding ErrorMessage, Mode=OneWay}"
                             Foreground="{StaticResource DangerBrush}"
                             TextWrapping="Wrap"
                             FontSize="14"
                             TextAlignment="Justify"
                             IsReadOnly="True"
                             BorderThickness="0"
                             Background="Transparent"
                             Padding="0"
                             VerticalAlignment="Top"
                             Visibility="{Binding ShowError, Converter={StaticResource BooleanToVisibilityConverter}}"/>

                    <!-- 翻译结果（含历史记录查看） -->
                    <Grid Visibility="{Binding TranslationResult, Converter={StaticResource NullToVisibilityConverter}}">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>

                        <!-- 删除按钮（查看历史时显示） -->
                        <StackPanel Grid.Row="0" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,0,0,8"
                                    Visibility="{Binding SelectedHistoryItem, Converter={StaticResource BooleanToVisibilityConverter}}">
                            <Button Content="删除" Command="{Binding DeleteHistoryCommand}"
                                    CommandParameter="{Binding SelectedHistoryItem}"
                                    FontSize="11" Padding="8,4" Style="{StaticResource DangerButtonStyle}"/>
                        </StackPanel>

                        <!-- 翻译结果内容 -->
                        <ContentControl Grid.Row="1" Content="{Binding TranslationResult}">
                            <ContentControl.ContentTemplate>
                                <DataTemplate>
                                    <StackPanel>
                                        <!-- 原文 -->
                                        <TextBlock Text="原文" FontWeight="Bold" FontSize="13" Foreground="{StaticResource TextSecondaryBrush}" Margin="0,0,0,5"/>
                                        <TextBox Text="{Binding Text, Mode=OneWay}"
                                                 TextWrapping="Wrap"
                                                 FontSize="14"
                                                 Margin="0,0,0,12"
                                                 Foreground="{StaticResource TextPrimaryBrush}"
                                                 TextAlignment="Justify"
                                                 IsReadOnly="True"
                                                 BorderThickness="0"
                                                 Background="Transparent"
                                                 Padding="0"/>

                                        <!-- 错误情况 -->
                                        <TextBox Text="{Binding TranslatedText, Mode=OneWay}"
                                                 Foreground="{StaticResource DangerBrush}"
                                                 TextWrapping="Wrap"
                                                 FontSize="14"
                                                 TextAlignment="Justify"
                                                 IsReadOnly="True"
                                                 BorderThickness="0"
                                                 Background="Transparent"
                                                 Padding="0"
                                                 Visibility="{Binding IsError, Converter={StaticResource BooleanToVisibilityConverter}}"/>

                                        <!-- 译文（英译中单词） -->
                                        <TextBlock Text="译文" FontWeight="Bold" FontSize="13" Foreground="{StaticResource TextSecondaryBrush}" Margin="0,5,0,5" Visibility="{Binding IsEnToZhWord, Converter={StaticResource BooleanToVisibilityConverter}}"/>
                                        <TextBox Text="{Binding TranslatedText, Mode=OneWay}"
                                                 TextWrapping="Wrap"
                                                 FontSize="15"
                                                 FontWeight="Bold"
                                                 Margin="0,0,0,15"
                                                 Foreground="{StaticResource PrimaryBrush}"
                                                 TextAlignment="Justify"
                                                 IsReadOnly="True"
                                                 BorderThickness="0"
                                                 Background="Transparent"
                                                 Padding="0"
                                                 Visibility="{Binding IsEnToZhWord, Converter={StaticResource BooleanToVisibilityConverter}}"/>

                                        <!-- 译文（英译中句子） -->
                                        <TextBlock Text="译文" FontWeight="Bold" FontSize="13" Foreground="{StaticResource TextSecondaryBrush}" Margin="0,5,0,5" Visibility="{Binding IsEnToZhSentence, Converter={StaticResource BooleanToVisibilityConverter}}"/>
                                        <TextBox Text="{Binding TranslatedText, Mode=OneWay}"
                                                 TextWrapping="Wrap"
                                                 FontSize="15"
                                                 FontWeight="Bold"
                                                 Margin="0,0,0,10"
                                                 Foreground="{StaticResource PrimaryBrush}"
                                                 TextAlignment="Justify"
                                                 IsReadOnly="True"
                                                 BorderThickness="0"
                                                 Background="Transparent"
                                                 Padding="0"
                                                 Visibility="{Binding IsEnToZhSentence, Converter={StaticResource BooleanToVisibilityConverter}}"/>

                                        <!-- 分割线 -->
                                        <Border Height="1" Background="#E0E0E0" Margin="0,5,0,15" Visibility="{Binding IsEnToZhSentence, Converter={StaticResource BooleanToVisibilityConverter}}"/>

                                        <!-- 难词解释 -->
                                        <TextBlock Text="难词解释" FontWeight="Bold" FontSize="13" Foreground="{StaticResource TextSecondaryBrush}" Margin="0,0,0,8" Visibility="{Binding IsEnToZhSentence, Converter={StaticResource BooleanToVisibilityConverter}}"/>

                                        <!-- 单词详情 -->
                                        <ItemsControl ItemsSource="{Binding WordDetails}" Margin="0,0,0,10">
                                            <ItemsControl.ItemTemplate>
                                                <DataTemplate>
                                                    <Border BorderBrush="#DCDCDC" BorderThickness="1" CornerRadius="6" Padding="12" Margin="0,0,0,10" Background="White">
                                                        <StackPanel>
                                                            <TextBlock FontSize="16" FontWeight="Bold" Foreground="{StaticResource PrimaryBrush}">
                                                                <Run Text="{Binding Word, Mode=OneWay}"/>
                                                                <Run Text="{Binding Phonetic, Mode=OneWay}" Foreground="{StaticResource TextSecondaryBrush}" FontSize="14"/>
                                                            </TextBlock>
                                                            <TextBox Text="{Binding DisplayDefinition, Mode=OneWay}"
                                                                     FontSize="14"
                                                                     Margin="0,5,0,0"
                                                                     TextWrapping="Wrap"
                                                                     TextAlignment="Justify"
                                                                     IsReadOnly="True"
                                                                     BorderThickness="0"
                                                                     Background="Transparent"
                                                                     Padding="0"/>
                                                            <TextBox Text="{Binding Example, Mode=OneWay}"
                                                                     FontSize="12"
                                                                     Foreground="{StaticResource TextSecondaryBrush}"
                                                                     FontStyle="Italic"
                                                                     TextWrapping="Wrap"
                                                                     Margin="0,8,0,0"
                                                                     TextAlignment="Justify"
                                                                     IsReadOnly="True"
                                                                     BorderThickness="0"
                                                                     Background="Transparent"
                                                                     Padding="0"
                                                                     Visibility="{Binding Example, Converter={StaticResource StringToVisibilityConverter}}"/>
                                                            <TextBox Text="{Binding ExampleTranslation, Mode=OneWay}"
                                                                     FontSize="12"
                                                                     Foreground="{StaticResource TextSecondaryBrush}"
                                                                     TextWrapping="Wrap"
                                                                     Margin="0,2,0,0"
                                                                     TextAlignment="Justify"
                                                                     IsReadOnly="True"
                                                                     BorderThickness="0"
                                                                     Background="Transparent"
                                                                     Padding="0"
                                                                     Visibility="{Binding ExampleTranslation, Converter={StaticResource StringToVisibilityConverter}}"/>
                                                        </StackPanel>
                                                    </Border>
                                                </DataTemplate>
                                            </ItemsControl.ItemTemplate>
                                        </ItemsControl>

                                        <!-- 多种翻译（中译英） -->
                                        <TextBlock Text="多种翻译" FontWeight="Bold" FontSize="13" Foreground="{StaticResource TextSecondaryBrush}" Margin="0,8,0,8" Visibility="{Binding IsZhToEn, Converter={StaticResource BooleanToVisibilityConverter}}"/>

                                        <!-- 翻译选项 -->
                                        <ItemsControl ItemsSource="{Binding Options}">
                                            <ItemsControl.ItemTemplate>
                                                <DataTemplate>
                                                    <Border BorderBrush="{StaticResource PrimaryBgBrush}" BorderThickness="1" CornerRadius="6" Padding="12" Margin="0,0,0,12" Background="{StaticResource SurfaceAltBrush}">
                                                        <StackPanel>
                                                            <TextBox Text="{Binding Text, Mode=OneWay}" FontSize="15" FontWeight="SemiBold" Margin="0,0,0,5" Foreground="{StaticResource PrimaryBrush}" TextWrapping="Wrap" TextAlignment="Justify" IsReadOnly="True" BorderThickness="0" Background="Transparent" Padding="0"/>
                                                            <TextBox Text="{Binding Scenario, Mode=OneWay}" FontSize="12" Foreground="{StaticResource TextSecondaryBrush}" FontStyle="Italic" Visibility="{Binding Scenario, Converter={StaticResource StringToVisibilityConverter}}" TextWrapping="Wrap" TextAlignment="Justify" IsReadOnly="True" BorderThickness="0" Background="Transparent" Padding="0" Margin="0,0,0,5"/>
                                                        </StackPanel>
                                                    </Border>
                                                </DataTemplate>
                                            </ItemsControl.ItemTemplate>
                                        </ItemsControl>
                                    </StackPanel>
                                </DataTemplate>
                            </ContentControl.ContentTemplate>
                        </ContentControl>
                    </Grid>
                </StackPanel>
            </ScrollViewer>
        </Border>
```

- [ ] **Step 6: 新增底部分页控件**

在 Grid.Row="2"（跨两列）添加：

```xml
        <!-- 底部分页控件 -->
        <Border Grid.Row="2" Grid.ColumnSpan="2" Padding="12,8" BorderBrush="{StaticResource SurfaceAltBrush}" BorderThickness="0,1,0,0" Background="{StaticResource SurfaceAltBrush}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- 总记录数 -->
                <TextBlock Grid.Column="0" Text="{Binding TotalHistoryCount, StringFormat=共 {0} 条记录}" FontSize="11" Foreground="{StaticResource TextSecondaryBrush}" VerticalAlignment="Center"/>

                <!-- 页码按钮 -->
                <StackPanel Grid.Column="2" Orientation="Horizontal">
                    <Button Content="‹" Command="{Binding PreviousPageCommand}" Width="28" Height="24" FontSize="11" Margin="0,0,4,0" Style="{StaticResource SecondaryButtonStyle}"/>
                    <TextBlock Text="{Binding CurrentPage}" FontSize="11" VerticalAlignment="Center" Margin="0,0,4,0" MinWidth="20" TextAlignment="Center"/>
                    <TextBlock Text="/" FontSize="11" Foreground="{StaticResource TextSecondaryBrush}" VerticalAlignment="Center" Margin="0,0,4,0"/>
                    <TextBlock Text="{Binding PageCount}" FontSize="11" Foreground="{StaticResource TextSecondaryBrush}" VerticalAlignment="Center" Margin="0,0,4,0" MinWidth="20" TextAlignment="Center"/>
                    <Button Content="›" Command="{Binding NextPageCommand}" Width="28" Height="24" FontSize="11" Style="{StaticResource SecondaryButtonStyle}"/>
                </StackPanel>
            </Grid>
        </Border>
```

- [ ] **Step 7: 清理不需要的 Resources**

删除原来的 `FeedbackShowStoryboard` 和 `WordContextMenu`（如果不再使用的话——检查发现单词详情的右键菜单还在使用，需要保留）。只删除不再使用的 Storyboard。

实际上，单词详情的反馈动画（FeedbackShowStoryboard）和右键菜单（WordContextMenu）仍然在翻译结果中用到，需要保留。

- [ ] **Step 8: 编译验证**

```bash
dotnet build WordReminder/WordReminder.csproj
```

Expected: Build succeeds. 如果 XAML 有绑定错误，需要修复。

- [ ] **Step 9: Commit**

```bash
git add WordReminder/Views/TranslationWindow.xaml
git commit -m "feat: redesign TranslationWindow with left-right split layout for history"
```

---

### Task 6: 修改 TranslationWindow.xaml.cs 支持历史选中

**Files:**
- Modify: `WordReminder/Views/TranslationWindow.xaml.cs`

- [ ] **Step 1: 处理 ListBox 选中事件触发命令**

ListBox 的 `SelectedItem` 已经通过双向绑定到 `SelectedHistoryItem`，但我们还需要在选中变化时触发 `SelectHistoryCommand`。使用 `Interaction.Triggers` 或者在 ViewModel 中使用 `partial void OnSelectedHistoryItemChanged(HistoryItemViewModel? value)`。

在 `TranslationViewModel.cs` 中添加：

```csharp
    partial void OnSelectedHistoryItemChanged(HistoryItemViewModel? value)
    {
        if (value != null)
        {
            SelectHistoryCommand.Execute(value);
        }
    }
```

这利用了 CommunityToolkit.Mvvm 的 `[ObservableProperty]` 生成的部分方法。

不需要修改 xaml.cs 文件。

- [ ] **Step 2: 运行应用程序验证**

```bash
dotnet run --project WordReminder/WordReminder.csproj
```

Expected: 应用程序启动，翻译窗口显示左右分栏布局。

- [ ] **Step 3: Commit**

```bash
git add WordReminder/Views/TranslationWindow.xaml.cs WordReminder/ViewModels/TranslationViewModel.cs
git commit -m "feat: wire up history selection in TranslationViewModel"
```

---

### Task 7: 修复 AITranslationService 序列化支持

**Files:**
- Modify: `WordReminder/Services/AITranslationService.cs`

- [ ] **Step 1: 在 AITranslationService 中添加静态反序列化方法**

`TranslationViewModel.SerializeTranslationResult` 直接使用 `JsonSerializer.Serialize(result)` 可以工作，但从历史加载时需要从 JSON 重建 `TranslationResult`。在 `AITranslationService` 中添加：

```csharp
    /// <summary>
    /// 从 JSON 反序列化翻译结果
    /// </summary>
    public static TranslationResult DeserializeTranslationResult(JsonElement root, string inputText)
    {
        var result = new TranslationResult
        {
            Text = inputText
        };

        if (root.TryGetProperty("translatedText", out var translatedText))
        {
            result.TranslatedText = translatedText.GetString();
        }

        if (root.TryGetProperty("type", out var type))
        {
            result.Type = type.GetString();
        }

        if (root.TryGetProperty("direction", out var direction))
        {
            result.Direction = direction.GetString();
        }

        if (root.TryGetProperty("wordDetails", out var wordDetails) && wordDetails.ValueKind == JsonValueKind.Array)
        {
            result.WordDetails = new List<WordInfo>();
            foreach (var item in wordDetails.EnumerateArray())
            {
                result.WordDetails.Add(new WordInfo
                {
                    Word = item.TryGetProperty("word", out var w) ? w.GetString() : null,
                    Phonetic = item.TryGetProperty("phonetic", out var p) ? p.GetString() : null,
                    PartOfSpeech = item.TryGetProperty("partOfSpeech", out var pos) ? pos.GetString() : null,
                    Definition = item.TryGetProperty("definition", out var d) ? d.GetString() : null,
                    Example = item.TryGetProperty("example", out var e) ? e.GetString() : null,
                    ExampleTranslation = item.TryGetProperty("exampleTranslation", out var et) ? et.GetString() : null
                });
            }
        }

        if (root.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
        {
            result.Options = new List<TranslationOption>();
            foreach (var item in options.EnumerateArray())
            {
                result.Options.Add(new TranslationOption
                {
                    Text = item.TryGetProperty("text", out var t) ? t.GetString() : null,
                    Scenario = item.TryGetProperty("scenario", out var s) ? s.GetString() : null
                });
            }
        }

        return result;
    }
```

但是 `SerializeTranslationResult` 应该放在 `AITranslationService` 中保持一致性。修改 `TranslationViewModel` 中的调用：

```csharp
    private static string SerializeTranslationResult(AITranslationService.TranslationResult result)
    {
        return JsonSerializer.Serialize(result);
    }
```

这个保持现状即可，因为 `JsonSerializer.Serialize` 已经能正确序列化对象。

- [ ] **Step 2: 编译验证**

```bash
dotnet build WordReminder/WordReminder.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add WordReminder/Services/AITranslationService.cs
git commit -m "feat: add DeserializeTranslationResult for loading history from JSON"
```

---

### Task 8: 添加空状态和边界处理

**Files:**
- Modify: `WordReminder/ViewModels/TranslationViewModel.cs`
- Modify: `WordReminder/Views/TranslationWindow.xaml`

- [ ] **Step 1: 在 ViewModel 中添加空状态属性**

```csharp
    /// <summary>
    /// 是否没有历史记录
    /// </summary>
    public bool ShowHistoryEmpty => HistoryItems.Count == 0;

    partial void OnHistoryItemsChanged(ObservableCollection<HistoryItemViewModel> value)
    {
        OnPropertyChanged(nameof(ShowHistoryEmpty));
        OnPropertyChanged(nameof(CanPreviousPage));
        OnPropertyChanged(nameof(CanNextPage));
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }
```

注意：`OnHistoryItemsChanged` 是 CommunityToolkit.Mvvm 为 `[ObservableProperty]` 生成的部分方法回调。

- [ ] **Step 2: 在 XAML 中添加空状态 TextBlock 的可见性绑定**

在 ListBox 下方（同 Grid.Row="1"）添加：

```xml
                <!-- 空状态提示 -->
                <TextBlock Grid.Row="1" Text="暂无历史记录" FontSize="12" Foreground="{StaticResource TextSecondaryBrush}"
                           HorizontalAlignment="Center" VerticalAlignment="Center"
                           Visibility="{Binding ShowHistoryEmpty, Converter={StaticResource BooleanToVisibilityConverter}}"/>
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build WordReminder/WordReminder.csproj
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add WordReminder/ViewModels/TranslationViewModel.cs WordReminder/Views/TranslationWindow.xaml
git commit -m "feat: add empty state for history list"
```
