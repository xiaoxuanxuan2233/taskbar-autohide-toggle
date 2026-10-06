# Taskbar Toggle

轻量 Windows 任务栏自动隐藏工具 · A lightweight Windows taskbar auto-hide utility.

**[⬇ 下载 EXE / Download EXE](https://github.com/xiaoxuanxuan2233/taskbar-autohide-toggle/raw/refs/heads/main/downloads/TaskbarToggle.exe)** · **[⬇ 下载 ZIP（含中英文说明）/ ZIP with guides](https://github.com/xiaoxuanxuan2233/taskbar-autohide-toggle/raw/refs/heads/main/downloads/TaskbarToggle-Windows-x64.zip)**

Windows 10 / 11 · x64 · v2.0.1 · 双击即可打开设置 / Double-click to open settings.

v2.0.1 修复：关机 / 注销可正常退出；诊断文件写入失败不影响快捷键；保存失败时恢复自启、桌面入口及配置。

v2.0.1 fixes: allow shutdown / sign-out, keep hotkeys working when diagnostic writes fail, and roll back startup, desktop and configuration changes when saving fails.

## 中文使用说明

1. 下载并运行 **TaskbarToggle.exe**，点击“开启自动隐藏”。鼠标移到任务栏所在的屏幕边缘即可显示。
2. 默认快捷键 **Ctrl + Alt + Z**，随时切换开启 / 关闭。修改时点击快捷键输入框，按新组合，再点“保存设置”；组合被占用会提示。
3. **开机自启首次默认勾选。建议保持开启，让快捷键登录后随时可用。重点：点击“保存设置”才生效。** 程序会复制到 `%LOCALAPPDATA%\TaskbarToggle`，登录时只在托盘运行。取消勾选并保存即可关闭，选择会被记住。
4. 点击“桌面快捷方式”创建设置入口。点击“选择图片…”可自选 PNG、JPG、BMP、GIF 或 ICO，拖动裁剪框 / 右下角，或输入精确坐标，选择 **32 / 64 / 128 / 256** 像素并保存。托盘与桌面快捷方式一起更新。
5. 关闭窗口会收起到托盘；双击托盘图标或桌面快捷方式重新打开设置。右键托盘图标 → “退出”结束运行。

**建议：** 图标优先选清晰图片和 256 像素；小图片放大可能模糊。快捷键优先使用 Ctrl + Alt + 一个字母，避开常用的复制、粘贴组合。

**重点：** 自启不会强制改变任务栏的当前隐藏状态。退出工具后，任务栏状态保持不变，快捷键停止响应。更新前先退出旧版本，再运行新 EXE 并保存设置。

## English guide

1. Download and run **TaskbarToggle.exe**. Click **Turn auto-hide on**. Move the pointer to the taskbar's screen edge to reveal it.
2. Press **Ctrl + Alt + Z** to toggle on / off. To change it, click the shortcut field, press your combination, then **Save settings**. Conflicts are reported.
3. **Start at login is checked on first launch. Recommended: keep it enabled so your shortcut is ready after sign-in. Important: click Save settings to apply.** The app copies itself to `%LOCALAPPDATA%\TaskbarToggle` and starts quietly in the tray. Uncheck and save to disable; your choice is remembered.
4. Click **Desktop shortcut** for a settings launcher. **Choose image…** accepts PNG, JPG, BMP, GIF or ICO. Drag the crop box / its lower-right corner, or enter exact coordinates; choose **32 / 64 / 128 / 256** pixels and save. The tray and desktop shortcut update together.
5. Closing the window keeps the tray helper running. Double-click the tray icon or desktop shortcut to reopen settings. Right-click the tray icon → **Exit** to quit.

**Recommended:** use a clear image at 256 pixels. Enlarging a tiny image can reduce clarity. Prefer Ctrl + Alt + a letter and avoid everyday copy / paste shortcuts.

**Important:** startup preserves the current taskbar setting. Exiting preserves that setting and stops the hotkey listener. Before updating, exit the old version, run the new EXE and save settings.

## Lightweight by design / 低占用设计

Native C# / Windows Forms UI, DPI scaling and scrollable layouts for smaller screens. Uses Windows taskbar and hotkey messages, with no polling loop, browser engine or network requests. Images are processed locally when you edit them.

原生 C# / Windows Forms，按系统 DPI 缩放，小屏可滚动。使用 Windows 消息响应任务栏和快捷键，无轮询、浏览器内核或联网请求，图片在本机处理。

## Build / 源码构建

Requires Windows x64 and .NET Framework 4.x (normally included in Windows 10/11). No .NET SDK or AutoHotkey required. The downloaded EXE needs no extra files. The binary is unsigned.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
.\dist\TaskbarToggle.exe
.\dist\TaskbarToggle.exe --self-test test-results.txt
```

`install.ps1` builds and opens settings; startup and desktop options remain in the UI. Optional commands: `--background`, `--toggle`, `--on`, `--off`, `--status`, `--exit`. Status is written to `%LOCALAPPDATA%\TaskbarToggle\status.txt`.

Download checksums: [SHA256SUMS.txt](downloads/SHA256SUMS.txt).
