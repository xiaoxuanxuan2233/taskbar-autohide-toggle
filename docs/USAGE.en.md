# Taskbar Toggle · English guide

Double-click TaskbarToggle.exe to open settings. No extra files required. For Windows 10/11 x64 with .NET Framework 4.x.

1. Click Turn auto-hide on. Move the pointer to the taskbar's screen edge to reveal it.
2. Ctrl + Alt + Z toggles auto-hide. To change it, click the shortcut field, press your combination, then Save settings. Conflicts are reported.
3. Start at login is checked on first launch. Recommended: keep it enabled so your shortcut is ready after sign-in. Important: Save settings applies it. The app copies itself to %LOCALAPPDATA%\TaskbarToggle and starts quietly in the tray. Uncheck and save to disable; the disabled choice is remembered.
4. Desktop shortcut only creates a settings launcher with the saved icon. It does not save pending changes or enable startup; click Save settings to apply changes.
5. Choose image accepts PNG/JPG/BMP/GIF/ICO. Drag the crop box to move it, drag the lower-right corner to resize, or enter X, Y and size. Select 32/64/128/256 pixels, click Use icon, then save. Updates the tray and desktop shortcut.
6. Closing the window keeps the tray helper running. Double-click the tray icon to reopen settings. Right-click it to toggle or exit.

Recommended: choose a clear image and 256 pixels. Enlarging tiny images can reduce clarity. Avoid common copy/paste shortcut combinations.

Important: startup preserves the taskbar setting. Exiting keeps that setting and stops the hotkey listener. Before updating, exit the old version, run the new EXE and save settings.

Choose 中文 / English in the language menu and save. The app handles Windows messages without polling or network requests.

If the taskbar is unavailable, a non-modal notification appears and the tool keeps running. Retry the shortcut or Retry toggle shortly. Successful icon changes remove the previous generated icon without deleting external images; a locked old file does not invalidate the new settings.

Source and downloads: https://github.com/xiaoxuanxuan2233/taskbar-autohide-toggle
