$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$installDir = Join-Path $env:LOCALAPPDATA 'TaskbarAutoHideToggle'
$executablePath = Join-Path $installDir 'TaskbarAutoHideToggle.exe'
$existingProcesses = Get-Process -Name TaskbarAutoHideToggle -ErrorAction SilentlyContinue
if ($existingProcesses | Where-Object { $_.Path -eq $executablePath }) {
    throw 'Exit the existing tool using its tray menu, then rerun install.ps1.'
}
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'dist\TaskbarAutoHideToggle.exe') -Destination $executablePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'dist\taskbar.ico') -Destination (Join-Path $installDir 'taskbar.ico') -Force
$shortcutShell = New-Object -ComObject WScript.Shell
$desktopLink = $shortcutShell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'Taskbar Toggle.lnk'))
$desktopLink.TargetPath = $executablePath
$desktopLink.Arguments = '--toggle'
$desktopLink.WorkingDirectory = $installDir
$desktopLink.IconLocation = (Join-Path $installDir 'taskbar.ico') + ',0'
$desktopLink.Description = 'Toggle taskbar auto-hide (Ctrl + Alt + Z)'
$desktopLink.WindowStyle = 7
$desktopLink.Save()
$startupLink = $shortcutShell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Startup')) 'Taskbar Auto Hide Toggle.lnk'))
$startupLink.TargetPath = $executablePath
$startupLink.WorkingDirectory = $installDir
$startupLink.IconLocation = (Join-Path $installDir 'taskbar.ico') + ',0'
$startupLink.WindowStyle = 7
$startupLink.Save()
Start-Process -FilePath $executablePath -ArgumentList '--on' -WindowStyle Hidden -Wait
Start-Process -FilePath $executablePath -WindowStyle Hidden
Write-Output 'Installed Taskbar Toggle. Press Ctrl + Alt + Z to switch auto-hide.'
