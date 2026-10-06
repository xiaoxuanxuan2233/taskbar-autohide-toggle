$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
# Open the requested interactive settings window. Startup is selected and saved in the UI.
Start-Process -FilePath (Join-Path $PSScriptRoot 'dist\TaskbarToggle.exe')
Write-Output 'Settings opened. Click Save settings to apply startup; click Desktop shortcut for a launcher.'
