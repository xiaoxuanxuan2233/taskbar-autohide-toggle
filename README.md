# Taskbar Toggle

轻量 Windows 任务栏自动隐藏工具 · A lightweight Windows taskbar auto-hide utility.

**[⬇ 下载 EXE / Download EXE](https://github.com/xiaoxuanxuan2233/taskbar-autohide-toggle/raw/refs/heads/main/downloads/TaskbarToggle.exe)** · **[⬇ 下载 ZIP（含中英文说明）/ ZIP with guides](https://github.com/xiaoxuanxuan2233/taskbar-autohide-toggle/raw/refs/heads/main/downloads/TaskbarToggle-Windows-x64.zip)**

Windows 10 / 11 · x64 · v2.0.2 · 双击即可打开设置 / Double-click to open settings.

v2.0.2 修复：创建桌面入口独立于保存设置；任务栏暂不可用时可安全重试；成功换图标后清理程序生成的旧图标。包含 v2.0.1 的退出与保存回滚修复。

v2.0.2 fixes: desktop launcher creation is independent of saving settings, unavailable taskbar errors allow safe retries, and successful icon changes clean up previous generated icons. Includes v2.0.1 shutdown and save rollback fixes.

## 中文使用说明

1. 下载并运行 **TaskbarToggle.exe**，点击“开启自动隐藏”。鼠标移到任务栏所在的屏幕边缘即可显示。
2. 默认快捷键 **Ctrl + Alt + Z**，随时切换开启 / 关闭。修改时点击快捷键输入框，按新组合，再点“保存设置”；组合被占用会提示。
3. **开机自启首次默认勾选。建议保持开启，让快捷键登录后随时可用。重点：点击“保存设置”才生效。** 程序会复制到 `%LOCALAPPDATA%\TaskbarToggle`，登录时只在托盘运行。取消勾选并保存即可关闭，选择会被记住。
4. 点击“桌面快捷方式”只创建设置入口，使用已保存的图标，不会保存待修改设置或启用自启。点击“选择图片…”可自选 PNG、JPG、BMP、GIF 或 ICO，拖动裁剪框 / 右下角，或输入精确坐标，选择 **32 / 64 / 128 / 256** 像素并保存。托盘与桌面快捷方式一起更新。
5. 关闭窗口会收起到托盘；双击托盘图标或桌面快捷方式重新打开设置。右键托盘图标 → “退出”结束运行。

**建议：** 图标优先选清晰图片和 256 像素；小图片放大可能模糊。快捷键优先使用 Ctrl + Alt + 一个字母，避开常用的复制、粘贴组合。

**重点：** 自启不会强制改变任务栏的当前隐藏状态。退出工具后，任务栏状态保持不变，快捷键停止响应。更新前先退出旧版本，再运行新 EXE 并保存设置。

任务栏暂不可用时，工具显示非阻塞提示并继续运行；稍后可再次按快捷键或点“重试切换”。成功保存后会清理被替换的程序图标，外部图片不删除；旧文件被占用时，清理失败不影响新设置。

## English guide

1. Download and run **TaskbarToggle.exe**. Click **Turn auto-hide on**. Move the pointer to the taskbar's screen edge to reveal it.
2. Press **Ctrl + Alt + Z** to toggle on / off. To change it, click the shortcut field, press your combination, then **Save settings**. Conflicts are reported.
3. **Start at login is checked on first launch. Recommended: keep it enabled so your shortcut is ready after sign-in. Important: click Save settings to apply.** The app copies itself to `%LOCALAPPDATA%\TaskbarToggle` and starts quietly in the tray. Uncheck and save to disable; your choice is remembered.
4. **Desktop shortcut** only creates a settings launcher using the saved icon; it does not save pending changes or enable startup. **Choose image…** accepts PNG, JPG, BMP, GIF or ICO. Drag the crop box / its lower-right corner, or enter exact coordinates; choose **32 / 64 / 128 / 256** pixels and save. The tray and desktop shortcut update together.
5. Closing the window keeps the tray helper running. Double-click the tray icon or desktop shortcut to reopen settings. Right-click the tray icon → **Exit** to quit.

**Recommended:** use a clear image at 256 pixels. Enlarging a tiny image can reduce clarity. Prefer Ctrl + Alt + a letter and avoid everyday copy / paste shortcuts.

**Important:** startup preserves the current taskbar setting. Exiting preserves that setting and stops the hotkey listener. Before updating, exit the old version, run the new EXE and save settings.

If the taskbar is unavailable, a non-modal notification appears and the tool keeps running. Retry the shortcut or **Retry toggle** shortly. Successful saves remove the replaced generated icon, while preserving external images; a locked old file does not invalidate the new settings.

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
