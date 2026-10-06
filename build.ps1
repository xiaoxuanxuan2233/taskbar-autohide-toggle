$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compilerPath)) {
    throw 'The 64-bit .NET Framework 4.x C# compiler is required.'
}
$outputDir = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$iconPath = Join-Path $PSScriptRoot 'assets\taskbar.ico'
$sourcePath = Join-Path $PSScriptRoot 'src\TaskbarAutoHideToggle.cs'
$executablePath = Join-Path $outputDir 'TaskbarAutoHideToggle.exe'
& $compilerPath /nologo /target:winexe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/win32icon:$iconPath" "/out:$executablePath" $sourcePath
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $outputDir 'taskbar.ico') -Force
Write-Output "Built $executablePath"
