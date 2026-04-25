; WordReminder 安装程序脚本
; 使用 Inno Setup Compiler 编译此脚本生成安装包

#define AppName "WordReminder"
#define AppVersion "1.0.9"
#define AppPublisher "GoodZheng"
#define AppURL "https://github.com/GoodZheng/WordReminder"
#define AppExeName "WordReminder.exe"
#define AppID "A1B2C3D4-E5F6-4A5B-8C9D-1E2F3A4B5C6D"

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
; 需要管理员权限
PrivilegesRequired=admin
; 卸载旧版本后再安装，确保干净覆盖
; 进程检测和关闭由 PrepareToInstall 自定义代码处理，提供中文提示且不会循环弹窗
UninstallRestartComputer=no

; 自定义中文消息
[Messages]
WelcomeLabel1=欢迎使用 [name] 安装向导
WelcomeLabel2=本程序将在您的计算机上安装 [name/ver]。%n%n建议您在继续安装前关闭所有其他应用程序。
LicenseLabel=请仔细阅读许可协议
LicenseLabel3=如果您接受许可协议中的条款，点击"我同意"继续安装。如果您选择"取消"，安装程序将终止。
FinishedLabel=[name] 已成功安装到您的计算机中。%n%n点击"完成"关闭安装程序。
FinishedLabelNoIcons=[name] 已成功安装到您的计算机中。%n%n点击"完成"关闭安装程序。
WizardSelectTasks=选择附加任务
WizardReady=准备安装
WizardRunning=正在复制文件
WizardRunningDesc=请稍候，正在安装 [name]...

[CustomMessages]
CreateDesktopIcon=创建桌面快捷方式
AdditionalIcons=附加图标：
OtherTasks=其他选项：
LaunchProgram=启动 WordReminder
UninstallProgram=卸载 WordReminder

[Code]
var
  OldInstallPath: String;
  HasOldVersion: Boolean;

// 检查 WordReminder 是否正在运行
function IsWordReminderRunning(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  // 使用 tasklist 检测进程
  Exec('tasklist', '/FI "IMAGENAME eq WordReminder.exe" /NH', '', SW_HIDE,
       ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

// 终止正在运行的 WordReminder 进程
function KillWordReminderProcess(): Boolean;
var
  ResultCode: Integer;
begin
  if not IsWordReminderRunning() then
  begin
    Result := True;
    Exit;
  end;

  if Exec('taskkill', '/F /IM WordReminder.exe', '', SW_HIDE,
          ewWaitUntilTerminated, ResultCode) and (ResultCode = 0) then
  begin
    Sleep(1500);  // 等待进程完全退出
    Result := True;
  end
  else
    Result := False;
end;

function InitializeSetup(): Boolean;
var
  RegQueryString: String;
  RegSubKey: String;
begin
  Result := True;
  OldInstallPath := '';
  HasOldVersion := False;

  RegSubKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A1B2C3D4-E5F6-4A5B-8C9D-1E2F3A4B5C6D}_is1';

  if RegQueryStringValue(HKLM, RegSubKey, 'DisplayVersion', RegQueryString) then
  begin
    // 如果是升级安装
    if RegQueryString <> '{#AppVersion}' then
    begin
      HasOldVersion := True;

      // 获取旧版本的安装路径
      if RegQueryStringValue(HKLM, RegSubKey, 'InstallLocation', RegQueryString) then
        OldInstallPath := RegQueryString;
    end;
  end;
end;

// 准备安装（用户已确认，即将开始复制文件）
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  UninstallString: String;
  ResultCode: Integer;
begin
  Result := '';

  if not HasOldVersion then
    Exit;

  // 1. 检测并关闭正在运行的程序
  if IsWordReminderRunning() then
  begin
    if MsgBox('检测到 WordReminder 正在运行。' + #13#10 + #13#10 +
              '需要关闭后才能继续更新。是否关闭程序并继续？',
              mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := '安装已取消。';
      Exit;
    end;

    if not KillWordReminderProcess() then
    begin
      Result := '无法关闭 WordReminder 进程，请手动关闭后重试。';
      Exit;
    end;
  end;

  // 2. 卸载旧版本
  if RegQueryStringValue(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A1B2C3D4-E5F6-4A5B-8C9D-1E2F3A4B5C6D}_is1',
    'UninstallString', UninstallString) then
  begin
    UninstallString := RemoveQuotes(UninstallString);
    if not Exec(UninstallString, '/SILENT /SUPPRESSMSGBOXES /NORESTART', '',
                SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
    begin
      // 卸载失败，询问是否继续（新安装会覆盖文件）
      if MsgBox('卸载旧版本失败（错误代码：' + IntToStr(ResultCode) + '）。' + #13#10 + #13#10 +
                '是否跳过卸载，直接继续安装？',
                mbError, MB_YESNO) = IDNO then
      begin
        Result := '安装已取消。';
      end;
    end;
  end;
end;

// 在选择目录页面设置默认路径
procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpSelectDir) and (OldInstallPath <> '') then
  begin
    WizardForm.DirEdit.Text := OldInstallPath;
  end;
end;

// 安装后记录路径
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    RegWriteStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A1B2C3D4-E5F6-4A5B-8C9D-1E2F3A4B5C6D}_is1',
                       'InstallLocation', ExpandConstant('{app}'));
  end;
end;

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
