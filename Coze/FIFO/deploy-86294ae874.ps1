# Windows部署脚本
$ErrorActionPreference = "Stop"

# 1. 检查依赖
Write-Host "检查系统依赖..." -ForegroundColor Cyan

# 检查Java
if (!(Get-Command java -ErrorAction SilentlyContinue)) {
    Write-Host "未找到Java运行时，请先安装JDK 17或更高版本" -ForegroundColor Red
    exit 1
}

# 检查WebView2运行时
$webView2Path = "HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
if (!(Test-Path $webView2Path)) {
    Write-Host "请先安装Microsoft Edge WebView2运行时" -ForegroundColor Yellow
    $installWebView2 = Read-Host "是否现在下载安装? (Y/N)"
    if ($installWebView2 -eq 'Y') {
        Start-Process "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
    }
}

# 2. 构建项目
Write-Host "构建Java后端..." -ForegroundColor Cyan
Set-Location ".\GoldTradingBackend"
mvn clean package
Copy-Item "target\GoldTradingBackend.jar" "..\GoldTradingDesktop\bin\Release\net7.0-windows10.0.19041.0\win10-x64\"

Write-Host "构建Vue前端..." -ForegroundColor Cyan
Set-Location "..\GoldTradingFrontend"
npm install
npm run build

# 将构建好的前端复制到Java资源目录
Write-Host "构建C#桌面应用..." -ForegroundColor Cyan
Set-Location "..\GoldTradingDesktop"
dotnet publish -c Release -r win10-x64 --self-contained

# 3. 创建安装包
Write-Host "创建安装包..." -ForegroundColor Cyan
$publishDir = "bin\Release\net7.0-windows10.0.19041.0\win10-x64\publish"
$installerDir = "..\Installer"
New-Item -ItemType Directory -Force -Path $installerDir
Copy-Item "$publishDir\*" $installerDir -Recurse

# 创建快捷方式
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$installerDir\GoldTradingSystem.lnk")
$Shortcut.TargetPath = "$installerDir\GoldTradingDesktop.exe"
$Shortcut.Save()

Write-Host "部署完成！" -ForegroundColor Green
Write-Host "安装文件位于: $installerDir" -ForegroundColor Yellow