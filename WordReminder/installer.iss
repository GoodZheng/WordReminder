; WordReminder 安装程序脚本
; 使用 Inno Setup Compiler 编译此脚本生成安装包

#define AppName "WordReminder"
#define AppVersion "1.0.1"
#define AppPublisher "GoodZheng"
#define AppURL "https://github.com/GoodZheng/WordReminder"
#define AppExeName "WordReminder.exe"

[Setup]
; 应用程序基本信息
AppId={{A1B2C3D4-E5F6-4A5B-8C9D-1E2F3A4B5C6D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=..\installer
OutputBaseFilename=WordReminder-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; 安装程序图标
SetupIconFile=app.ico
; 卸载程序图标
UninstallDisplayIcon={app}\{#AppExeName}
; 不需要管理员权限
PrivilegesRequired=lowest

; 自定义中文消息
[Messages]
WelcomeLabel1=欢迎使用 [name] 安装向导
WelcomeLabel2=本程序将在您的计算机上安装 [name/ver]。%n%n建议您在继续安装前关闭所有其他应用程序。
LicenseLabel=请仔细阅读许可协议
LicenseLabel3=如果您接受许可协议中的条款，点击"我同意"继续安装。如果您选择"取消"，安装程序将终止。
InfoClick=安装程序已准备好安装 [name] 到您的计算机中。
InfoClickNext=点击"下一步"开始安装。
FinishedLabel=[name] 已成功安装到您的计算机中。%n%n点击"完成"关闭安装程序。
FinishedLabelNoIcons=[name] 已成功安装到您的计算机中。%n%n点击"完成"关闭安装程序。
WinVersionTooHighError=此程序不支持 %1 版本的 Windows。请安装 Windows 2000 或更高版本。
SelectDirDesc=选择 [name] 安装在哪？
SelectDirLabel3=安装程序将把 [name] 安装到以下文件夹。
SelectDirBrowseLabel=点击"下一步"继续。如果您想选择其他文件夹，点击"浏览"。
DiskSpaceLabelWarning=至少需要 %1 MB 的可用磁盘空间。
SelectGroupDesc=选择开始菜单文件夹
SelectGroupLabel3=安装程序将在下列开始菜单文件夹中创建程序快捷方式。
SelectStartMenuFolderLabel=开始菜单文件夹：
SelectStartMenuFolderBrowseLabel=点击"下一步"继续。如果您想选择其他文件夹，点击"浏览"。
NeedAdminTitle=需要管理员权限
NeedAdminMessage=安装此程序需要管理员权限。请以管理员身份重新运行此安装程序。
NoPrivilegeForOverrideWarning=要覆盖现有文件，您需要管理员权限。
SelectTasksLabel=选择附加任务
SelectTasksLabel2=选择 [name] 安装期间要执行的附加任务，然后点击"下一步"。
SelectTasksLabel3=选择 [name] 安装期间要执行的附加任务：
CompletedLabel=完成
CompletedLabel2=已完成 [name] 安装
PreparingDesc=正在准备安装 [name]
WizardSelectTasks=选择附加任务
WizardSelectProgramGroup=选择开始菜单文件夹
WizardReady=准备安装
WizardReadyLabel=准备安装 [name]
WizardRunning=正在复制文件
WizardRunningDesc=请稍候，正在安装 [name]...
CreatingIconsLabel=正在创建快捷方式...
FinishedRunLabel=点击"完成"完成安装后，启动 [name]

[CustomMessages]
CreateDesktopIcon=创建桌面快捷方式
AdditionalIcons=附加图标：
OtherTasks=其他选项：
LaunchProgram=启动 [name]
UninstallProgram=卸载 [name]

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "开机自启动"; GroupDescription: "{cm:OtherTasks}"; Flags: unchecked

[Files]
; 主程序和依赖文件
Source: "..\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; 开始菜单图标
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
; 桌面图标
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
; 安装完成后运行
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Registry]
; 开机自启动注册表项
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "WordReminder"; ValueData: """{app}\{#AppExeName}"""; Tasks: autostart; Flags: uninsdeletevalue
