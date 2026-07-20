# OpenRFID Middleware - Windows Service Installer Script
# Run as Administrator in PowerShell

param (
    [string]$ServiceName = "OpenRFIDMiddleware",
    [string]$DisplayName = "OpenRFID Middleware Service",
    [string]$BinaryPath = "$PSScriptRoot\..\..\src\OpenRFID.Management.Api\bin\Release\net10.0\OpenRFID.Management.Api.exe"
)

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "📡 Installing OpenRFID Middleware as Windows Service" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be executed as Administrator!"
    Exit 1
}

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Service '$ServiceName' already exists. Stopping and removing..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName
}

Write-Host "Registering New-Service '$ServiceName'..." -ForegroundColor Green
New-Service -Name $ServiceName `
            -BinaryPathName "`"$BinaryPath`"" `
            -DisplayName $DisplayName `
            -Description "Enterprise OpenRFID Middleware Service providing reader ingestion, filtering, dispatch, and REST management API." `
            -StartupType Automatic

Start-Service -Name $ServiceName
Write-Host "✅ OpenRFID Middleware Service installed and started successfully!" -ForegroundColor Green
