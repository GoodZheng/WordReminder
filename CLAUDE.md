# CLAUDE.md

本文档为 Claude Code (claude.ai/code) 提供本代码仓库的开发指导。

## 项目概述

一个 WPF 桌面单词记忆软件，在透明无边框窗口中循环展示英文单词，包含音标、词性、中文释义和例句。

## 已经做的更改

- **AI 消息 Markdown 渲染**: 引入 MdXaml 1.27.0 NuGet 包，新建 `Converters/MarkdownHelper.cs` 附加属性，将 AI 回复从纯文本 TextBlock 改为 RichTextBox + MdXaml 渲染，支持标题、列表、代码块、粗体等 Markdown 格式
- **用户消息支持复制**: 将用户消息气泡从 TextBlock 改为只读 TextBox，支持文本选择和复制
- **气泡宽度调整**: AI 气泡 MaxWidth 设为 700px，用户气泡 MaxWidth 设为 500px，确保两者之间有明显间隔
- **版本升级至 1.0.13**

### 未提交更改 (feature/fluent-ui 分支)

- **WindowBase 最小化按钮**: 在 `Controls/WindowBase.cs` 和 `Styles/WindowBase.xaml`、`Themes/Generic.xaml` 中为窗口标题栏添加最小化按钮（`PART_MinimizeButton`），包含 Hover 高亮效果
- **助手管理窗口重构 (`AssistantListWindow`)**: 大幅重构为三栏布局（助手列表 | 聊天区 | 对话列表面板），窗口尺寸从 700x500 调整为 950x600；支持内嵌聊天模式（`IsChatMode`），无需打开独立 ChatWindow 即可直接对话；添加 GridSplitter 可拖拽调整面板宽度；自定义滚动条样式（悬停高亮）；窗口关闭时自动保存/恢复布局状态（面板宽度、选中助手、聊天模式等持久化到 `AppSettings`）
- **AI 翻译 DeepSeek 兼容**: `AITranslationService` 添加 DeepSeek 模型检测（`IsDeepSeekProvider`），显式发送 `thinking=disabled` 关闭思考模式以减少 Token 消耗；增强请求/响应日志记录（包含 Token usage、reasoning_content 检测等）；新增 `TokenUsageInfo` 模型记录 Prompt/Completion/Total Token 用量
- **翻译窗口 Token 用量显示**: `TranslationWindow.xaml` 状态栏新增 Token 消耗信息行（输入/输出/总计）；`TranslationViewModel` 新增 `TranslationTokenUsage` 属性
- **助手删除级联修复**: `AssistantService.DeleteAssistant` 使用事务级联删除 conversations 和 chat_messages，避免孤立数据
- **聊天服务 Provider 回退逻辑**: `ChatAIService` 优先使用助手指定的 Provider/Model，未指定时回退到全局激活的 Provider 和默认模型
- **助手编辑验证增强**: `AssistantEditViewModel` 将 `SaveCommand` 重构为 `TrySave()` 方法，添加必填字段校验（名称、系统提示词）和 `ValidationMessage` 属性；`AssistantEditDialog.xaml` 底部显示验证错误信息
- **聊天功能增强**: `ChatViewModel` 新增 `ClearAllConversations` 命令（清空全部对话历史，带确认对话框）；加载对话时保持选中项状态，避免触发不必要的消息重载；新增 `_isLoadingConversations` 标志防止加载期间触发 `OnSelectedConversationChanged`
- **AppSettings 布局持久化**: 新增助手窗口布局相关属性（`AssistantSelectedId`、`AssistantIsChatMode`、`AssistantConversationId`、面板宽度等）和聊天气泡宽度配置（`AiBubbleMaxWidth`、`UserBubbleMaxWidth`）
- **退出时关闭助手窗口**: `MainViewModel.Exit` 时主动关闭 `_assistantListWindow`
- **新增值转换器**: `BooleanToVisibilityConverter`、`InverseBooleanConverter`、`CollectionEmptyToVisibilityConverter`
- **ChatWindow AI 气泡 Markdown 渲染**: AI 消息气泡从 TextBlock 改为 RichTextBox + MdXaml，错误消息以红色边框标识

## 构建与运行命令

```bash
# 构建解决方案
dotnet build WordReminder.slnx

# 运行应用程序
dotnet run --project WordReminder/WordReminder.csproj

# 构建并运行
dotnet run --project WordReminder

# 清理构建产物
dotnet clean WordReminder.slnx
```

**构建安装包**:

参见 [BUILD.md](BUILD.md) 获取详细的安装包构建指南。

## 架构

### 技术栈
- **框架**: .NET 10, WPF (Windows Presentation Foundation)
- **MVVM**: CommunityToolkit.Mvvm (ViewModels、Messages、依赖注入)
- **DI**: Microsoft.Extensions.DependencyInjection
- **UI**: XAML 透明窗口样式 (`AllowsTransparency="True"`, `WindowStyle="None"`)
- **数据库**: SQLite (Microsoft.Data.Sqlite)
- **配置**: JSON 文件 (`appsettings.json`)
- **屏幕检测**: System.Windows.Forms.Screen (支持多显示器)

### 核心配置

- **窗口位置**: 自动保存/恢复；检测窗口是否在屏幕外（如外接显示器断开）并重置到主屏幕中央
- **显示选项**: 字体大小/颜色、透明度、音标/释义显示开关
- **切换间隔**: 可配置的单词切换时间间隔
- **全局快捷键**: 可自定义上一个/下一个/播放暂停/翻译/窗口置顶

### 数据文件

- `words.db` - SQLite 数据库（在输出目录自动生成）
- `appsettings.json` - 用户偏好设置（在输出目录自动生成）

### 窗口行为

- **透明**: 仅显示文字，背景完全透明
- **置顶**: 默认 `Topmost="True"`
- **可拖动**: 鼠标拖动移动窗口；双击打开设置
- **右键菜单**: 播放/暂停、上一个/下一个、翻译、设置、退出
- **全局快捷键**: 支持自定义快捷键控制各项功能

## Git 配置

使用 git 时如果网络不通需要使用代理：
```bash
git -c http.proxy=http://127.0.0.1:7890 -c https.proxy=http://127.0.0.1:7890 push
```

## ClaudeCode 注意事项
1. 注意符合开发模式和设计原则，注意分层，不要把所有功能堆积在一个实现类中；
2. 每次修改代码后需要执行 dotnet build 测试验证；
3. 如果需求不明确，需要向用户询问，不要擅自做决定；
4. 使用 MVVM 模式开发，View 层只负责 UI 逻辑，业务逻辑放在 ViewModel 中；
5. 用户功能实现后需要询问用户是否进行安装包构建过程、推送GitHub release 过程，如果需要则安装 `BUILD.md` 中的步骤操作。

# UI 设计要求
## 布局与对齐
- 所有同级输入控件（TextBox、ComboBox、PasswordBox）必须高度一致
- 标签（Label/TextBlock）与对应控件必须垂直居中对齐
- 使用 Grid 的 RowDefinitions / ColumnDefinitions 精确控制布局，禁止用 Margin 堆砌来凑对齐
- 同一行的多个按钮宽度保持一致，除非内容长度差异明显
- 整体内容区域设置统一的内边距，避免控件贴边

## 间距规范
- 标签与输入框之间保留适当间距
- 行与行之间使用统一的垂直间距
- 按钮组内部间距一致
- 页面四周留有足够的留白，不要让内容撑满边缘

## 控件样式
- 所有输入框必须有圆角边框，默认、聚焦、禁用三种状态要有视觉区分
- 按钮必须有 Hover 和 Press 状态的视觉反馈，不能使用默认的 Windows 原生样式
- 按钮根据语义分类：主操作（强调色填充）、次要操作（描边）、危险操作（警示色），样式不能混用
- 禁止直接使用未经样式覆盖的原生 WPF 控件外观

## 视觉层次
- 页面标题、分组标题、字段标签、输入内容、提示文字，使用不同字号和颜色加以区分
- 相关字段归组，组与组之间有明确的视觉分隔（分隔线或间距）
- 危险操作（如删除、重置）必须与普通操作在颜色上明显区分

## 样式管理
- 所有重复使用的样式必须定义在 Resources 中，禁止在控件上内联重复书写属性
- 颜色、圆角、字号等视觉 token 使用统一的资源键管理
- 同类控件使用同一个 Style，不允许同类控件出现两种不同的外观

