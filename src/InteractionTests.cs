using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

static class InteractionTests {
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static T Field<T>(object value, string name) { return (T)value.GetType().GetField(name, Private).GetValue(value); }
    static void Click(Button button) { typeof(Button).GetMethod("OnClick", Private).Invoke(button, new object[] { EventArgs.Empty }); }
    static bool Available(uint key) {
        var probe = new NativeWindow(); probe.CreateHandle(new CreateParams());
        try { bool result = Native.RegisterHotKey(probe.Handle, 104, 0x4006, key); if (result) Native.UnregisterHotKey(probe.Handle, 104); return result; }
        finally { probe.DestroyHandle(); }
    }
    static Preferences Seed(string root) {
        Directory.CreateDirectory(root); Program.DataPath = root;
        Check(Available((uint)Keys.F10) && Available((uint)Keys.F9), "Interaction test hotkeys unavailable");
        var settings = new Preferences { Modifiers = 6, Key = (uint)Keys.F10, Language = "en", Startup = false }; settings.Save(); return settings;
    }
    static byte[] ImageIcon(Color color) {
        using (var image = new Bitmap(64, 64)) { using (var graphics = Graphics.FromImage(image)) graphics.Clear(color); return Icons.Build(image, new Rectangle(0, 0, 64, 64), 64); }
    }
    static string OwnedIcon(string directory, Color color) { string file = Path.Combine(directory, "custom-" + Guid.NewGuid().ToString("N") + ".ico"); File.WriteAllBytes(file, ImageIcon(color)); return Native.ActualPath(file); }
    static ToggleContext Context() { return new ToggleContext(true, delegate { }, new string[0]); }
    static void Stop(ToggleContext context) { context.ExitThread(); context.Dispose(); }
    public static void Run(List<string> results, string root) {
        string previous = Program.DataPath;
        try {
            Desktop(results, Path.Combine(root, "desktop-button"));
            Recovery(results, Path.Combine(root, "taskbar-recovery"));
            ReplacedIcons(results, Path.Combine(root, "replaced-icons"));
            FailedIconSave(results, Path.Combine(root, "failed-icon-save"));
            ExternalIcons(results, Path.Combine(root, "protected-icons"));
            LockedOldIcon(results, Path.Combine(root, "locked-old-icon"));
        } finally { Program.DataPath = previous; }
    }
    static void Desktop(List<string> results, string root) {
        var initial = Seed(root); initial.Startup = true; initial.Save();
        string desktop = Path.Combine(root, "desktop.lnk"), startup = Path.Combine(root, "startup.lnk");
        int saves = 0, creates = 0; string iconUsed = null;
        var context = new ToggleContext(true, delegate { saves++; File.WriteAllText(startup, "enabled"); }, new[] { startup, desktop }, delegate(string icon) { creates++; iconUsed = icon; File.WriteAllText(desktop, "launcher"); }, delegate { }, delegate { return false; }, delegate { });
        try {
            // Mimic an unsaved first launch: default startup remains checked, with pending edits.
            File.Delete(Program.ConfigPath);
            using (var form = new SettingsForm(context)) {
                Check(Field<CheckBox>(form, "startup").Checked, "First-launch startup was not checked");
                var pending = Field<Preferences>(form, "pending"); pending.Key = (uint)Keys.F9; pending.Language = "zh"; pending.IconPath = Path.Combine(root, "unsaved.ico");
                typeof(SettingsForm).GetField("iconBytes", Private).SetValue(form, ImageIcon(Color.Blue));
                Click(Field<Button>(form, "desktop"));
                Field<CheckBox>(form, "startup").Checked = false;
                Click(Field<Button>(form, "desktop"));
                Check(creates == 2 && saves == 0 && File.Exists(desktop), "Desktop button saved settings or failed to create launcher");
                Check(!File.Exists(startup) && !File.Exists(Program.ConfigPath) && Directory.GetFiles(root, "custom-*.ico").Length == 0, "Desktop button committed pending startup/config/icon");
                Check(iconUsed == context.Settings.IconPath && context.Settings.IconPath == "" && context.Settings.Language == "en" && context.Settings.Startup, "Desktop button used pending settings");
                Check(context.Settings.Key == (uint)Keys.F10 && !Available((uint)Keys.F10) && Available((uint)Keys.F9), "Desktop button changed active hotkey");
            }
            results.Add("PASS: desktop button creates launcher without saving pending startup, hotkey, language or icon on first launch");
        } finally { Stop(context); }
    }
    static void Recovery(List<string> results, string root) {
        Seed(root); bool available = false, hidden = false; int attempts = 0, notifications = 0, uncaught = 0;
        var context = new ToggleContext(true, delegate { }, new string[0], delegate { }, delegate {
            attempts++; if (!available) throw new InvalidOperationException("Injected missing Explorer taskbar"); hidden = !hidden;
        }, delegate { if (!available) throw new InvalidOperationException("Injected missing taskbar status"); return hidden; }, delegate { notifications++; throw new IOException("Injected unavailable notification channel"); });
        ThreadExceptionEventHandler errors = delegate { uncaught++; }; Application.ThreadException += errors;
        try {
            using (var form = new SettingsForm(context)) {
                typeof(ToggleContext).GetField("form", Private).SetValue(context, form);
                Check(Field<Label>(form, "status").Text.Contains("Unavailable"), "Missing taskbar was shown as off");
                var listener = Field<Listener>(context, "listener");
                Check(Native.PostMessage(listener.Handle, 0x312, new IntPtr(context.HotkeyId), IntPtr.Zero), "Could not post test hotkey"); Application.DoEvents();
                var menu = Field<NotifyIcon>(context, "tray").ContextMenuStrip;
                ((ToolStripMenuItem)menu.Items[1]).PerformClick();
                Click(Field<Button>(form, "toggle"));
                Check(attempts == 3 && notifications == 1 && uncaught == 0, "Hotkey/tray/button errors escaped or spammed notifications");
                Check(context.Registered && !Available((uint)Keys.F10), "Taskbar failure stopped hotkey listener");
                available = true;
                Check(Native.PostMessage(listener.Handle, 0x312, new IntPtr(context.HotkeyId), IntPtr.Zero), "Listener was lost"); Application.DoEvents();
                ((ToolStripMenuItem)menu.Items[1]).PerformClick(); Click(Field<Button>(form, "toggle"));
                Check(attempts == 6 && hidden && uncaught == 0 && notifications == 1, "Taskbar triggers failed to recover");
                Check(Field<Label>(form, "status").Text.Contains("On"), "Recovered taskbar status was not refreshed");
                form.AllowExit = true;
            }
            results.Add("PASS: hotkey/tray/button tolerate missing taskbar and failed notification, rate-limit warnings and recover without restarting");
        } finally { Application.ThreadException -= errors; Stop(context); }
    }
    static void ReplacedIcons(List<string> results, string root) {
        var settings = Seed(root); string first = OwnedIcon(root, Color.Red); settings.IconPath = first; settings.Save();
        var context = Context();
        try {
            context.Apply(context.Settings.Copy(), ImageIcon(Color.Blue)); string second = context.Settings.IconPath;
            Check(!File.Exists(first) && File.Exists(second) && Directory.GetFiles(root, "custom-*.ico").Length == 1, "First replacement did not clean old icon");
            context.Apply(context.Settings.Copy(), ImageIcon(Color.Green)); string third = context.Settings.IconPath;
            Check(!File.Exists(second) && File.Exists(third) && Directory.GetFiles(root, "custom-*.ico").Length == 1, "Repeated replacement accumulated icons");
            var defaults = context.Settings.Copy(); defaults.IconPath = ""; context.Apply(defaults, null);
            Check(!File.Exists(third) && Directory.GetFiles(root, "custom-*.ico").Length == 0, "Reset to default retained old custom icon");
            results.Add("PASS: successful replacements and reset to default clean old generated icons while retaining the current icon");
        } finally { Stop(context); }
    }
    static void FailedIconSave(List<string> results, string root) {
        var settings = Seed(root); string oldIcon = OwnedIcon(root, Color.Red); settings.IconPath = oldIcon; settings.Save(); byte[] original = File.ReadAllBytes(oldIcon);
        var context = Context();
        try {
            bool failed = false;
            using (var locked = new FileStream(Program.ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                try { context.Apply(context.Settings.Copy(), ImageIcon(Color.Blue)); } catch (IOException) { failed = true; } catch (UnauthorizedAccessException) { failed = true; }
            }
            Check(failed && context.Settings.IconPath == oldIcon && File.Exists(oldIcon) && Directory.GetFiles(root, "custom-*.ico").Length == 1, "Failed save deleted old icon or retained staged icon");
            Check(Convert.ToBase64String(File.ReadAllBytes(oldIcon)) == Convert.ToBase64String(original), "Failed save modified old icon");
            results.Add("PASS: failed icon save preserves the previous icon and removes the staged replacement");
        } finally { Stop(context); }
    }
    static void ExternalIcons(List<string> results, string root) {
        var settings = Seed(root); string outside = Path.Combine(root, "outside"); Directory.CreateDirectory(outside);
        string external = OwnedIcon(outside, Color.Red), named = Path.Combine(root, "user-selected.ico"); File.WriteAllBytes(named, ImageIcon(Color.Blue));
        foreach (string source in new[] { external, named }) {
            settings.IconPath = source; settings.Save(); var context = Context();
            try { var next = context.Settings.Copy(); next.IconPath = ""; context.Apply(next, null); Check(File.Exists(source), "Cleanup deleted a non-owned image"); }
            finally { Stop(context); }
        }
        results.Add("PASS: icon cleanup preserves external icons and non-generated filenames");
    }
    static void LockedOldIcon(List<string> results, string root) {
        var settings = Seed(root); string oldIcon = OwnedIcon(root, Color.Red); settings.IconPath = oldIcon; settings.Save(); var context = Context();
        try {
            using (var locked = new FileStream(oldIcon, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                var next = context.Settings.Copy(); next.Key = (uint)Keys.F9; context.Apply(next, ImageIcon(Color.Blue));
                Check(File.Exists(oldIcon) && File.Exists(context.Settings.IconPath) && context.Settings.IconPath != oldIcon, "Locked cleanup damaged committed icon");
                Check(Preferences.Load().IconPath == context.Settings.IconPath && !Available((uint)Keys.F9) && Available((uint)Keys.F10), "Cleanup failure damaged committed settings/hotkey");
            }
            results.Add("PASS: locked old icon cleanup is non-fatal and does not roll back committed settings or hotkey");
        } finally { Stop(context); }
    }
}
