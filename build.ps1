$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compilerPath)) {
    throw 'The 64-bit .NET Framework 4.x C# compiler is required.'
}
$outputDir = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$iconPath = Join-Path $outputDir 'default.ico'
# Draw the default icon from simple shapes; no external image is needed.
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class IconHandleCleanup {
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr handle);
}
'@ -ErrorAction SilentlyContinue
$bitmap = [Drawing.Bitmap]::new(64, 64)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$backgroundBrush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(32, 33, 35))
$graphics.FillEllipse($backgroundBrush, 1, 1, 62, 62)
$graphics.FillRectangle([Drawing.Brushes]::White, 14, 40, 36, 6)
$arrowPen = [Drawing.Pen]::new([Drawing.Color]::White, 4)
$graphics.DrawLines($arrowPen, [Drawing.Point[]]@([Drawing.Point]::new(22, 28), [Drawing.Point]::new(32, 18), [Drawing.Point]::new(42, 28)))
$graphics.DrawLine($arrowPen, 32, 18, 32, 35)
$handle = $bitmap.GetHicon()
$icon = [Drawing.Icon]::FromHandle($handle).Clone()
$stream = [IO.File]::Create($iconPath)
try { $icon.Save($stream) } finally { $stream.Dispose(); $icon.Dispose(); [IconHandleCleanup]::DestroyIcon($handle) | Out-Null; $arrowPen.Dispose(); $backgroundBrush.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
$sourcePaths = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
$executablePath = Join-Path $outputDir 'TaskbarToggle.exe'
$manifestPath = Join-Path $PSScriptRoot 'app.manifest'
& $compilerPath /nologo /target:winexe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll "/win32manifest:$manifestPath" "/win32icon:$iconPath" "/out:$executablePath" $sourcePaths
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output "Built $executablePath"
