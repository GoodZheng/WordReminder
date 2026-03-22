@echo off
chcp 65001 >nul
echo ========================================
echo   WordReminder 绿色版构建脚本
echo ========================================
echo.

echo [1/3] 清理旧的构建文件...
if exist "publish" rmdir /s /q "publish"
if not exist "release" mkdir "release"

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
echo [3/3] 制作绿色版...
for /f "tokens=2 delims==" %%a in ('findstr "Version" WordReminder\WordReminder.csproj ^| findstr "<Version>"') do set "version=%%~a"
set version=%version:<Version>=%
set version=%version:</Version>=%
set version=%version: =%

copy /Y "publish\WordReminder.exe" "release\WordReminder-portable-%version%.exe" >nul

if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo   构建成功！
    echo   绿色版位置: release\WordReminder-portable-%version%.exe
    echo ========================================
) else (
    echo.
    echo 制作绿色版失败！
)

echo.
pause
