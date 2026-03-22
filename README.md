# 📚 WordReminder

<div align="center">

桌面单词记忆软件 - 在透明无边框窗口中循环展示英文单词

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub release](https://img.shields.io/github/v/release/GoodZheng/WordReminder)](https://github.com/GoodZheng/WordReminder/releases)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

[下载最新版本](https://github.com/GoodZheng/WordReminder/releases/latest)

</div>

---

## ✨ 功能特性

- 📖 **透明窗口展示** - 桌面透明窗口，循环展示英文单词
- 🔊 **音标显示** - 显示单词的国际音标
- 📝 **词性释义** - 显示词性和中文释义
- 📚 **例句展示** - 显示英文例句帮助理解
- 🎨 **自定义样式** - 支持自定义字体大小、颜色、透明度
- ⌨️ **全局快捷键** - 支持自定义快捷键控制各项功能
- ⚡ **开机自启** - 支持开机自动启动
- 🔄 **自动更新** - 支持检查更新和自动下载新版本
- 📦 **内置词库** - 内置20个四级核心词汇
- 🌐 **在线词典** - 支持从必应词典获取单词详情
- 🤖 **AI 翻译** - 支持 AI 翻译功能
- 🖱️ **简单操作** - 鼠标拖动、右键菜单、双击设置

---

## 📸 截图

```
┌─────────────────────────────────────┐
│                                     │
│   ability         /əˈbɪləti/        │
│                                     │
│   n. 能力；本领                     │
│                                     │
│   She has the ability to pass       │
│   the exam.                         │
│                                     │
└─────────────────────────────────────┘
```

---

## 🚀 安装方式

> ⚠️ **关于 SmartScreen 警告**
>
> 首次运行时，Windows 可能会显示 "Windows 已保护你的电脑" 的 SmartScreen 警告。这是因为本软件未进行商业代码签名（个人开源项目常见情况）。
>
> **解决方法**：
> 1. 点击 "更多信息"
> 2. 点击 "仍要运行"
> 3. 安装完成后不会再出现此警告

### 方式一：安装包（推荐）

下载 [WordReminder-Setup-1.0.6.exe](https://github.com/GoodZheng/WordReminder/releases/download/v1.0.6/WordReminder-Setup-1.0.6.exe) 运行安装程序

安装程序功能：
- 图形化安装向导
- 开始菜单快捷方式
- 桌面快捷方式
- 开机自启动选项
- 自动卸载功能

### 方式二：绿色版

下载 [WordReminder-portable-1.0.6.exe](https://github.com/GoodZheng/WordReminder/releases/download/v1.0.6/WordReminder-portable-1.0.6.exe) 直接运行（无需安装）

---

## 📖 使用方法

### 基本操作

| 操作 | 说明 |
|------|------|
| **拖动窗口** | 鼠标左键拖动窗口到任意位置 |
| **打开设置** | 双击窗口或右键菜单选择"设置" |
| **切换单词** | 右键菜单选择"上一个"/"下一个" |
| **暂停/继续** | 右键菜单选择"暂停"/"继续" |
| **打开翻译** | 右键菜单选择"翻译" |

### 全局快捷键

支持自定义全局快捷键（在设置中配置）：

| 功能 | 默认快捷键 |
|------|-----------|
| 上一个单词 | Ctrl + Alt + Left |
| 下一个单词 | Ctrl + Alt + Right |
| 播放/暂停 | Ctrl + Alt + Space |
| 打开翻译 | Ctrl + Alt + T |
| 窗口置顶 | Ctrl + Alt + Top |

### 设置选项

在设置窗口中可以自定义：

- **窗口设置**
  - 切换间隔（1-300秒）
  - 窗口透明度（30%-100%）
  - 窗口置顶
  - 开机自启

- **快捷键设置**
  - 上一个/下一个单词
  - 播放/暂停
  - 打开翻译
  - 窗口置顶

- **显示设置**
  - 显示/隐藏音标
  - 显示/隐藏释义
  - 显示/隐藏例句

- **字体样式**
  - 单词字体大小和颜色
  - 音标字体大小和颜色
  - 释义字体大小和颜色
  - 例句字体大小和颜色

---

## 🔧 技术栈

- **框架**: .NET 10 + WPF (Windows Presentation Foundation)
- **架构**: MVVM (CommunityToolkit.Mvvm)
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **数据库**: SQLite
- **配置**: JSON 文件
- **屏幕检测**: System.Windows.Forms.Screen

---

## 📁 项目结构

```
WordReminder/
├── Models/              # 数据模型
│   ├── Word.cs         # 单词实体
│   ├── AppSettings.cs  # 配置模型
│   └── HotKey.cs       # 快捷键模型
├── ViewModels/          # MVVM 视图模型
├── Views/               # 视图
├── Services/            # 业务逻辑
│   ├── DatabaseService.cs      # SQLite 操作
│   ├── ConfigService.cs        # JSON 配置管理
│   ├── BingDictionaryService.cs # 必应词典
│   ├── HotKeyService.cs        # 全局快捷键
│   └── WindowManagerService.cs # 窗口管理
├── Messages/            # 消息通信
├── Controls/            # 自定义控件
├── Converters/          # 值转换器
├── Bootstrapper.cs      # 依赖注入启动
└── WordReminder.csproj  # 项目文件
```

---

## 🛠️ 开发构建

### 环境要求

- .NET 10 SDK
- Windows 10 或更高版本
- Visual Studio 2022 或 Rider（可选）
- Inno Setup Compiler（构建安装包）

### 构建步骤

```bash
# 克隆仓库
git clone https://github.com/GoodZheng/WordReminder.git
cd WordReminder

# 构建解决方案
dotnet build WordReminder.slnx

# 运行应用程序
dotnet run --project WordReminder/WordReminder.csproj
```

### 创建安装包

```bash
# 运行构建脚本
build.bat
```

构建脚本会：
1. 清理旧的构建文件
2. 发布单文件应用程序
3. 制作绿色版
4. 调用 Inno Setup 编译安装包

详细构建指南请参考 [BUILD.md](BUILD.md)

---

## 📝 更新日志

### v1.0.6 (2025-03-22)

- ✨ 新增全局快捷键功能
- ✨ 新增快捷键设置界面
- 🎨 优化翻译界面
- 🔧 统一添加/编辑单词窗口

### v1.0.5 (2025-03-XX)

- ✨ 新增 AI 翻译功能
- ✨ 新增 AI API 配置界面

### v1.0.4 (2025-03-XX)

- 🐛 Bug 修复

### v1.0.0 (2025-03-17)

🎉 首次发布

- ✨ 透明窗口展示单词
- ✨ 自定义字体、颜色、透明度
- ✨ 支持开机自启
- ✨ 自动更新功能
- ✨ 内置20个四级核心词汇
- ✨ 从必应词典获取单词详情

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

## 📄 许可证

本项目采用 [MIT License](https://opensource.org/licenses/MIT) 开源协议。

---

## 👨‍💻 开发者

- **GoodZheng** - [GitHub](https://github.com/GoodZheng)

---

## ⭐ Star History

如果这个项目对你有帮助，请给一个 Star ⭐

---

<div align="center">

Made with ❤️ by [GoodZheng](https://github.com/GoodZheng)

</div>
