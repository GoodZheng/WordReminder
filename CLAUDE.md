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

### 项目结构

```
WordReminder/
├── Models/              # 数据模型
│   ├── Word.cs         # 单词实体
│   ├── AppSettings.cs  # 配置模型
│   └── HotKey.cs       # 快捷键模型
├── ViewModels/          # MVVM 视图模型
│   ├── MainViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── TranslationViewModel.cs
│   └── ...
├── Views/               # 视图
│   ├── MainWindow.xaml
│   ├── SettingsWindow.xaml
│   ├── TranslationWindow.xaml
│   └── ...
├── Services/            # 业务逻辑
│   ├── DatabaseService.cs      # SQLite 操作
│   ├── ConfigService.cs        # JSON 配置管理
│   ├── BingDictionaryService.cs # 必应词典
│   ├── AIDictionaryService.cs   # AI 词典
│   ├── AITranslationService.cs  # AI 翻译
│   ├── HotKeyService.cs         # 全局快捷键
│   └── WindowManagerService.cs  # 窗口管理
├── Messages/            # 消息通信
├── Controls/            # 自定义控件
├── Converters/          # 值转换器
├── Bootstrapper.cs      # 依赖注入启动
└── WordReminder.csproj  # 项目文件
```

### 数据流

1. **初始化**: `Bootstrapper` → DI 容器 → `MainWindow` → ViewModel 加载数据
2. **单词来源**: `DefaultWordData` 中预置20个四级核心单词（网络不可用时作为后备）
3. **存储**: SQLite (`words.db`) 存储单词数据；JSON (`appsettings.json`) 存储用户设置
4. **展示**: DispatcherTimer 按配置间隔循环切换单词
5. **外部API**: BingDictionaryService 从 cn.bing.com/dict 抓取单词详情

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
4. 使用 MVVM 模式开发，View 层只负责 UI 逻辑，业务逻辑放在 ViewModel 中。
