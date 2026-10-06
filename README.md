# Taskbar Toggle

![Icon](assets/preview.png)

A small Windows tray utility that toggles the taskbar's native auto-hide setting.

- **Ctrl + Alt + Z** switches auto-hide on or off.
- Move the pointer to the bottom edge to reveal the taskbar when auto-hide is enabled.
- Double-click the **Taskbar Toggle** desktop shortcut to switch the setting.
- Runs at sign-in after installation and preserves the current taskbar setting.
- Right-click its tray icon to toggle auto-hide or exit the hotkey listener.

## Build

Windows 10/11, 64-bit, with .NET Framework 4.x is required. No AutoHotkey or .NET SDK is needed.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The executable and icon are written to `dist`. Keep `taskbar.ico` beside the executable.

## Install

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Installs to `%LOCALAPPDATA%\TaskbarAutoHideToggle`, creates a desktop shortcut and a sign-in startup shortcut, enables auto-hide, and starts the listener. Exit an existing copy using the tray menu before installing an update.

## Portable use

Run `dist\TaskbarAutoHideToggle.exe` for the hotkey listener. Commands `--toggle`, `--on`, `--off`, and `--status` work without a listener. Status is written to `query.txt`. If the shortcut is already registered by another application, details are written to `error.txt`.

To disable sign-in startup, remove **Taskbar Auto Hide Toggle.lnk** from your Windows Startup folder. Exiting the listener preserves the current auto-hide setting.

## 中文说明

按 **Ctrl + Alt + Z** 切换任务栏自动隐藏，也可以双击桌面的 **Taskbar Toggle**。鼠标移到屏幕底部会显示隐藏的任务栏。安装后随 Windows 登录自动启动；托盘菜单可以退出。

The icon uses a user-supplied anime image; no ownership of the original artwork is claimed. No license for the artwork is granted by this repository.
