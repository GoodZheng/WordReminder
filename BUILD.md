# 构建发布包

## 前置要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Inno Setup Compiler](https://jrsoftware.org/isdl.php) - 仅构建安装包时需要

## 发布方式

本项目提供两种发布方式：

| 方式 | 文件名 | 说明 |
|------|--------|------|
| **绿色版（推荐）** | `WordReminder-portable-{版本号}.exe` | 单文件免安装，直接运行 |
| 安装包 | `WordReminder-Setup-{版本号}.exe` | 带安装向导，支持开始菜单快捷方式 |

## 快速构建

```bash
# 构建绿色版（推荐）
build-portable.bat

# 构建安装包
build-installer.bat

# 或使用通用脚本（同时构建两种版本）
build.bat
```

## 手动构建

### 1. 发布应用程序

```bash
cd WordReminder
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "../publish" -p:DebugType=None -p:DebugSymbols=false
```

**参数说明：**
- `-c Release` - Release 配置
- `-r win-x64` - 目标平台 Windows x64
- `--self-contained` - 包含 .NET 运行时（无需用户安装 .NET）
- `-p:PublishSingleFile=true` - 单文件发布
- `-p:IncludeNativeLibrariesForSelfExtract=true` - 包含原生库
- `-o "../publish"` - 输出目录
- `-p:DebugType=None -p:DebugSymbols=false` - 移除调试信息减小体积

### 2. 制作绿色版

```bash
# 复制并重命名为绿色版
copy publish\WordReminder.exe release\WordReminder-portable-1.0.5.exe
```

绿色版即单文件发布的 exe，无需安装，直接双击运行。

### 3. 编译安装包（可选）

使用 Inno Setup Compiler 编译 `WordReminder/installer.iss`：

```bash
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" WordReminder\installer.iss
```

或使用图形界面打开 `WordReminder/installer.iss` 文件编译。

## 输出文件

```
release/
├── WordReminder-portable-{版本号}.exe    # 绿色版（推荐）
└── WordReminder-Setup-{版本号}.exe        # 安装包（可选）

installer/
└── WordReminder-Setup-{版本号}.exe        # 安装包编译输出
```

## 版本号更新

发布新版本时需要更新以下文件中的版本号：

1. **WordReminder/WordReminder.csproj**
   ```xml
   <Version>1.0.5</Version>
   <AssemblyVersion>1.0.5</AssemblyVersion>
   <FileVersion>1.0.5</FileVersion>
   ```

2. **WordReminder/installer.iss**（仅构建安装包时需要）
   ```iss
   #define AppVersion "1.0.5"
   ```

## 发布流程

1. 更新版本号（.csproj 和可选的 installer.iss）
2. 提交代码到 Git
3. 创建 Git tag：`git tag v1.0.5`
4. 构建绿色版：`build-portable.bat`
5. 测试绿色版运行
6. 推送到 GitHub：`git push origin main --tags`
7. 在 GitHub Releases 发布并上传 `release/WordReminder-portable-{版本号}.exe`

## 常见问题

### 文件被占用无法发布

如果遇到 "The process cannot access the file" 错误：

```bash
# 终止可能锁定的进程
taskkill /F /IM MSBuild.exe
taskkill /F /IM WordReminder.exe

# 或使用清理脚本
dotnet clean
```

### 绿色版无法运行

确保使用自包含发布模式（`--self-contained`），生成的 exe 文件大小应约 168 MB。

如果 exe 只有约 200 KB，说明是依赖框架的版本，需要用户安装 .NET 运行时才能运行。

### Windows Defender 警告

绿色版单文件可能被 Windows Defender 误报，需要添加信任或允许运行。
