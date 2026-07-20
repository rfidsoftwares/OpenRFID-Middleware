# OpenRFID Middleware - Windows Service Uninstaller Script
# Run as Administrator in PowerShell

param (
    [string]$ServiceName = "OpenRFIDMiddleware"
)

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "📡 Uninstalling OpenRFID Middleware Windows Service" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Stopping service '$ServiceName'..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName
    Write-Host "✅ Windows Service '$ServiceName' uninstalled successfully." -ForegroundColor Green
} else {
    Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow
}
