# 翻译窗口双击添加单词功能设计

## 概述

在翻译窗口的翻译结果列表中，用户双击单词名称区域即可弹出上下文菜单，将当前单词加入单词列表。菜单结构支持后续扩展。

## 交互设计

### 触发区域

- 仅限翻译结果卡片中的**单词名称 + 音标**那一行文字（即 `WordDetailViewModel.DisplayHeader` 对应的 TextBlock/Run 区域）
- 不扩展至整个卡片边框或释义/例句区域，避免误触

### 双击行为

- 双击触发弹出 WPF 上下文菜单（ContextMenu）
- 菜单项设计为可扩展结构，初始包含：
  - **"加入单词列表"** — 将当前单词写入 SQLite 数据库
- 后续可添加：
  - **"复制单词"**
  - **"查看详情"**
  - 其他翻译相关操作

### 已存在单词处理

- 若该单词（按 `Word.Text` 匹配）已存在于单词列表中：
  - 菜单项显示为灰色禁用状态
  - 文字变为 **"已在单词列表中"**
  - 用户无法再次添加

### 成功反馈

- 添加成功后，在单词卡片右上角显示绿色圆形"✓"图标 + 文字"已添加"标签
- 标签使用 `Storyboard` 动画，1.5 秒后淡出并隐藏
- 若添加失败（数据库写入异常），不显示反馈标签，仅在底部状态栏显示错误信息

## 架构设计

### 数据流

```
用户双击单词名称
  → TranslationWindow 捕获 MouseDoubleClick 事件
  → 从 DataContext 获取当前 WordDetailViewModel
  → 弹出 ContextMenu（绑定命令到 TranslationViewModel）
  → TranslationViewModel 检查单词是否已存在（DatabaseService.WordExists）
  → 若不存在，调用 DatabaseService.InsertWord 写入
    → 将 WordDetailViewModel 转换为 Word 模型并写入
  → 返回结果，触发 UI 反馈动画
```

### 涉及的文件与改动

| 文件 | 改动类型 |
|------|----------|
| `Views/TranslationWindow.xaml` | 为单词名称 TextBlock 添加 `MouseDoubleClick` 事件和 ContextMenu 资源 |
| `Views/TranslationWindow.xaml.cs` | 添加双击事件处理，弹出菜单，添加 `Storyboard` 反馈动画 |
| `ViewModels/TranslationViewModel.cs` | 添加 `AddWordCommand`（含已存在检查）、`CanAddWord` 属性 |
| `ViewModels/TranslationResultViewModel.cs` | 为 `WordDetailViewModel` 添加 `IsAdded` 和 `ShowFeedback` 属性 |
| `Controls/WordDetailCard.xaml` (新增) | 提取单词卡片为可复用控件，包含 ContextMenu 和反馈标签样式 |

### 分层职责

- **View 层**（TranslationWindow.xaml / xaml.cs）：
  - 捕获双击事件，提取当前单词的 ViewModel 数据
  - 弹出上下文菜单
  - 控制反馈动画的显示/隐藏
- **ViewModel 层**（TranslationViewModel）：
  - `AddWordCommand`：检查单词是否已存在，调用 DatabaseService 写入
  - 返回操作结果（成功/失败/已存在）
- **Service 层**（DatabaseService）：
  - 复用现有的 `InsertWord` 和 `WordExists` 方法，无需修改

## 错误处理

- AI 翻译返回的单词信息不完整（缺少 Word 字段）：不触发菜单
- 数据库写入失败：底部状态栏显示"添加失败: {错误信息}"，不显示成功反馈
- 单词已存在：菜单项禁用，不触发数据库操作

## 样式要求

- ContextMenu 使用 Fluent Design 风格（复用现有 `PrimaryButtonStyle` 等主题资源）
- 反馈标签：绿色圆形背景 + 白色对勾，卡片右上角定位
- 禁用状态的菜单项：灰色文字，无 Hover 效果
