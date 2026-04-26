# 翻译历史记录功能 - 设计文档

## 概述

在翻译界面中增加翻译历史记录的查看功能，采用左右分栏布局，历史记录持久化存储到 SQLite 数据库，支持分页和删除操作。

## 布局设计

### 翻译窗口结构改造

翻译窗口从现有的上-下结构（输入 + 结果）改为 三段式：

```
┌─────────────────────────────────────────────────────────┐
│ 顶部：输入框 + 翻译按钮（保持现有样式，不变）               │
├──────────────────┬──────────────────────────────────────┤
│ 左侧历史列表      │  右侧内容区域                         │
│ (宽 220px)        │                                      │
│                  │  未选中历史：显示当前翻译结果/空状态     │
│ ▸ hello world    │  选中历史：显示该条历史的完整翻译详情    │
│   你好，世界      │                                      │
│   2026-04-26     │                                      │
│                  │                                      │
│ ▸ machine learn  │                                      │
│   机器学习        │                                      │
│   2026-04-25     │                                      │
├──────────────────┴──────────────────────────────────────┤
│ 底部：分页控件（总条数 + 上一页/页码/下一页）               │
└─────────────────────────────────────────────────────────┘
```

### 窗口尺寸调整

- `MinWidth` 从 500 调整为 **750**（确保左右分栏不会过于拥挤）
- `Width` 默认从 600 调整为 **800**
- `MinHeight` 从 400 调整为 **450**（为分页栏留出空间）

### 左侧历史列表

- 固定宽度 220px，`BorderRight` 分隔线
- 每项双行显示：
  - 第一行：原文（加粗，蓝色选中状态）
  - 第二行：译文摘要（灰色，截断超长文本）
  - 第三行：日期时间（`yyyy-MM-dd HH:mm`，浅灰小字）
- 选中项：浅蓝背景 + 左侧蓝色强调条
- 支持右键菜单删除单条记录
- 支持 `ScrollViewer` 垂直滚动

### 右侧内容区域

- 未选中历史时：显示当前实时翻译结果（现有行为）
- 选中历史时：
  - 显示原文（顶部，加粗）
  - 显示译文（主色突出）
  - 显示单词详情/翻译选项等完整内容（复用现有 `TranslationResultViewModel` 模板）
  - 右上角提供"删除"按钮（红色文字）

### 底部分页控件

- 左侧显示总记录数
- 右侧显示页码按钮（上一页、数字页码、下一页）
- 每页 10 条记录
- 禁用状态使用灰色

## 数据存储

### 新增数据库表

```sql
CREATE TABLE TranslationHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InputText TEXT NOT NULL,
    TranslatedText TEXT,
    FullJson TEXT,
    TextType TEXT,
    Direction TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
)
```

| 字段 | 说明 |
|------|------|
| `InputText` | 用户输入的原文 |
| `TranslatedText` | 翻译结果纯文本摘要（用于列表展示） |
| `FullJson` | 完整翻译结果 JSON（用于详情还原，包含 WordDetails、Options 等） |
| `TextType` | `word` 或 `sentence` |
| `Direction` | `en2zh` 或 `zh2en` |
| `CreatedAt` | 创建时间，自动记录 |

### 操作

- **插入**：每次翻译成功后，将结果写入 `TranslationHistory`
- **分页查询**：`ORDER BY CreatedAt DESC LIMIT @PageSize OFFSET @Offset`
- **删除**：`DELETE FROM TranslationHistory WHERE Id = @Id`
- **统计总数**：`SELECT COUNT(*) FROM TranslationHistory`

## 架构设计

### ViewModel 层

`TranslationViewModel` 新增属性和命令：

| 新增成员 | 类型 | 说明 |
|---------|------|------|
| `HistoryItems` | `ObservableCollection<HistoryItemViewModel>` | 当前页的历史列表 |
| `SelectedHistoryItem` | `HistoryItemViewModel?` | 当前选中的历史记录 |
| `TotalHistoryCount` | `int` | 历史记录总数 |
| `CurrentPage` | `int` | 当前页码（从 1 开始） |
| `HistoryPageSize` | `int` | 每页条数，默认 10 |
| `SelectHistoryCommand` | `ICommand` | 选中某条历史 |
| `DeleteHistoryCommand` | `ICommand` | 删除某条历史 |
| `PreviousPageCommand` | `ICommand` | 上一页 |
| `NextPageCommand` | `ICommand` | 下一页 |
| `GoToPageCommand` | `ICommand` | 跳到指定页 |

`HistoryItemViewModel` 新类：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` | 数据库主键 |
| `InputText` | `string` | 原文 |
| `TranslatedText` | `string` | 译文摘要（截断至 50 字符） |
| `CreatedAt` | `DateTime` | 创建时间 |
| `FullJson` | `string` | 完整 JSON（内部使用） |

### Service 层

新增 `TranslationHistoryService`：

```csharp
public class TranslationHistoryService
{
    Task<int> InsertAsync(string inputText, TranslationResult result);
    Task<(List<HistoryItem> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<bool> DeleteAsync(int id);
}
```

其中 `HistoryItem` 是轻量 DTO，不包含完整 JSON。

### View 层

`TranslationWindow.xaml` 改造：

1. 在现有 Grid 中增加一列（`ColumnDefinition Width="220"`）
2. 新增左侧 `ListBox` 绑定 `HistoryItems`，使用 `DataTemplate` 双行显示
3. 右侧 `ContentControl` 绑定 `SelectedHistoryItem`（或当前翻译结果）
4. 底部新增分页控件 `StackPanel`
5. 调整窗口 `MinWidth`、`Width`、`MinHeight`

## 数据流

```
用户点击翻译 → AITranslationService.TranslateAsync
                    ↓
           返回 TranslationResult
                    ↓
        保存到 TranslationHistoryService.InsertAsync
                    ↓
        更新左侧历史列表（重新加载第1页）
                    ↓
用户点击历史项 → SelectHistoryCommand
                    ↓
        解析 FullJson → 构建 TranslationResultViewModel
                    ↓
        右侧显示完整翻译详情
```

## 错误处理

- 数据库操作失败时，在状态栏显示错误信息（不影响翻译功能）
- JSON 解析失败时，右侧显示"历史记录数据损坏"提示
- 空列表时左侧显示"暂无历史记录"占位文本

## 关键决策

1. **持久化存储**：使用 SQLite 而非内存，关闭后历史仍保留
2. **FullJson 存储**：不冗余存各子表，而是序列化整个结果，简化写入逻辑
3. **分页而非虚拟滚动**：历史表数据量可控，分页足够且实现简单
4. **左侧固定宽度**：不做可拖拽调节，保持实现简洁，后续可按需添加
