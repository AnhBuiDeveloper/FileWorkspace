param(
    [int]$Port = 5088
)

$ErrorActionPreference = 'Stop'

$projectFile = Join-Path $PSScriptRoot 'FileWorkspace.csproj'
$serverUrl = "http://127.0.0.1:$Port"

if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Không tìm thấy project: $projectFile"
}

$listeners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)

foreach ($listener in $listeners) {
    $processId = $listener.OwningProcess
    $processInfo = Get-CimInstance Win32_Process -Filter "ProcessId = $processId"
    $commandLine = [string]$processInfo.CommandLine
    $isFileWorkspace = $commandLine -match [regex]::Escape($projectFile) -or
        $commandLine -match 'FileWorkspace(\.dll|\.exe|\.csproj)'

    if (-not $isFileWorkspace) {
        try {
            $page = (Invoke-WebRequest -Uri "$serverUrl/" -UseBasicParsing -TimeoutSec 2).Content
            $isFileWorkspace = $page -match '<title>File Workspace</title>'
        }
        catch {
            $isFileWorkspace = $false
        }
    }

    if (-not $isFileWorkspace) {
        throw "Port $Port đang được process khác dùng (PID $processId). Không tự dừng process này."
    }

    Write-Host "Dừng FileWorkspace cũ (PID $processId)..."
    Stop-Process -Id $processId -Force

    for ($attempt = 0; $attempt -lt 50; $attempt++) {
        if (-not (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)) {
            break
        }
        Start-Sleep -Milliseconds 100
    }
}

$configuration = 'Debug'
$serverDll = Join-Path $PSScriptRoot "bin\$configuration\net10.0\FileWorkspace.dll"

Write-Host 'Build server...'
& dotnet build $projectFile --configuration $configuration --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'Build server thất bại.'
}

Write-Host "Chạy server: $serverUrl"
& dotnet $serverDll --urls $serverUrl
