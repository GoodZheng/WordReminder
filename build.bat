@echo off
chcp 65001 >nul
echo ========================================
echo   WordReminder 安装包构建脚本
echo ========================================
echo.

echo [1/3] 清理旧的构建文件...
if exist "publish" rmdir /s /q "publish"
if exist "installer" rmdir /s /q "installer"

echo.
echo [2/3] 发布应用程序...
cd WordReminder
call dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "../publish" -p:DebugType=None -p:DebugSymbols=false

if %errorlevel% neq 0 (
    echo.
    echo 发布失败！请检查错误信息。
    pause
    exit /b 1
)

cd ..

echo.
echo [3/3] 准备 Inno Setup 编译...
echo.
echo 请使用 Inno Setup Compiler 打开以下文件进行编译:
echo   WordReminder\installer.iss
echo.
echo 或者如果你已安装 Inno Setup，直接运行编译命令:
echo   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" WordReminder\installer.iss
echo.

if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    echo 检测到 Inno Setup，开始编译安装包...
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" WordReminder\installer.iss
    if %errorlevel% equ 0 (
        echo.
        echo ========================================
        echo   构建成功！
        echo   安装包位置: installer\WordReminder-Setup-1.0.0.exe
        echo ========================================
    ) else (
        echo.
        echo 编译失败！请检查错误信息。
    )
) else (
    echo 未检测到 Inno Setup Compiler。
    echo.
    echo 请先安装 Inno Setup:
    echo   下载地址: https://jrsoftware.org/isdl.php
    echo.
)

echo.
pause
