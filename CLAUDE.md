# CLAUDE.md

本文档为 Claude Code (claude.ai/code) 提供本代码仓库的开发指导。

## 项目概述

一个 WPF 桌面单词记忆软件，在透明无边框窗口中循环展示英文单词，包含音标、词性、中文释义和例句。

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

## 代码结构
- 样式定义与布局结构分离，Resources 在前，布局在后
- 每个逻辑区域加注释说明用途
- 不生成任何硬编码在控件属性上的一次性样式