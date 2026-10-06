using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

static class RegressionTests {
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static bool Available(uint key) {
        using (var probe = new Probe()) {
            bool registered = Native.RegisterHotKey(probe.Handle, 103, 0x4006, key);
            if (registered) Native.UnregisterHotKey(probe.Handle, 103);
            return registered;
        }
    }
    static Preferences Seed(string root, bool startup) {
        Directory.CreateDirectory(root); Program.DataPath = root;
        Check(Available((uint)Keys.F10) && Available((uint)Keys.F9), "Regression test hotkeys unavailable");
        var settings = new Preferences { Modifiers = 6, Key = (uint)Keys.F10, Language = "en", Startup = startup };
        settings.Save(); return settings;
    }
    public static void Run(List<string> results, string root) {
        string previous = Program.DataPath;
        try {
            Closing(results, Path.Combine(root, "closing"));
            Diagnostics(results, Path.Combine(root, "diagnostics"));
            FailedSave(results, Path.Combine(root, "disable-startup"), true, false);
            FailedSave(results, Path.Combine(root, "enable-startup"), false, false);
            FailedSave(results, Path.Combine(root, "shortcut-failure"), true, true);
            FailedRollback(results, Path.Combine(root, "rollback-failure"));
        } finally { Program.DataPath = previous; }
    }
    static void Closing(List<string> results, string root) {
        Seed(root, false);
        var context = new ToggleContext(true, delegate { }, new string[0]);
        try {
            Check(context.Registered, "Closing test shortcut missing");
            using (var form = new SettingsForm(context)) {
                var onClosing = typeof(Form).GetMethod("OnFormClosing", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (CloseReason reason in Enum.GetValues(typeof(CloseReason))) {
                    var args = new FormClosingEventArgs(reason, false); onClosing.Invoke(form, new object[] { args });
                    Check(args.Cancel == (reason == CloseReason.UserClosing), "Incorrect close cancellation: " + reason);
                }
                form.AllowExit = true;
                var exit = new FormClosingEventArgs(CloseReason.UserClosing, false); onClosing.Invoke(form, new object[] { exit });
                Check(!exit.Cancel, "Explicit tray exit was cancelled");
                form.AllowExit = false;
                typeof(ToggleContext).GetField("form", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(context, form);
                typeof(Form).GetMethod("OnFormClosed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { new FormClosedEventArgs(CloseReason.WindowsShutDown) });
                Check(Available((uint)Keys.F10), "System close left the hotkey listener running");
            }
            results.Add("PASS: only user close hides to tray; shutdown/task-manager close and explicit exit are allowed; system close stops listener");
        } finally { context.ExitThread(); context.Dispose(); }
    }
    static void Diagnostics(List<string> results, string root) {
        Seed(root, false); string ready = Path.Combine(root, "ready.txt"); File.WriteAllText(ready, "locked");
        using (var locked = new FileStream(ready, FileMode.Open, FileAccess.Read, FileShare.None)) {
            var context = new ToggleContext(true, delegate { }, new string[0]);
            try {
                Check(context.Registered && !Available((uint)Keys.F10), "Diagnostic failure broke initial registration");
                var next = context.Settings.Copy(); next.Key = (uint)Keys.F9;
                context.Apply(next, null);
                Check(context.Registered && context.Settings.Key == (uint)Keys.F9, "Hotkey state did not commit");
                Check(!Available((uint)Keys.F9) && Available((uint)Keys.F10), "Diagnostic failure broke the new global hotkey");
                Check(Preferences.Load().Key == (uint)Keys.F9, "Committed hotkey was not persisted");
                results.Add("PASS: locked ready.txt does not break startup or a committed hotkey change");
            } finally { context.ExitThread(); context.Dispose(); }
        }
    }
    static void FailedSave(List<string> results, string root, bool startupInitially, bool shortcutFailure) {
        Seed(root, startupInitially);
        string startup = Path.Combine(root, "startup.lnk"), desktop = Path.Combine(root, "desktop.lnk"), executable = Path.Combine(root, "installed.exe");
        if (startupInitially) File.WriteAllText(startup, "original startup");
        File.WriteAllText(desktop, "original desktop"); File.WriteAllText(executable, "original executable");
        string config = File.ReadAllText(Program.ConfigPath); int edits = 0;
        var context = new ToggleContext(true, delegate(Preferences next) {
            if (next.Startup) File.WriteAllText(startup, "changed startup"); else File.Delete(startup);
            File.WriteAllText(desktop, "changed desktop"); File.WriteAllText(executable, "changed executable"); edits++;
            if (shortcutFailure) throw new IOException("Injected shortcut failure");
        }, new[] { startup, desktop, executable });
        try {
            var next = context.Settings.Copy(); next.Startup = !startupInitially; next.Key = (uint)Keys.F9;
            byte[] icon; using (var image = new Bitmap(32, 32)) { using (var g = Graphics.FromImage(image)) g.Clear(Color.Blue); icon = Icons.Build(image, new Rectangle(0, 0, 32, 32), 32); }
            bool failed = false;
            if (shortcutFailure) { try { context.Apply(next, icon); } catch (IOException) { failed = true; } }
            else using (var lockedConfig = new FileStream(Program.ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                try { context.Apply(next, icon); } catch (IOException) { failed = true; } catch (UnauthorizedAccessException) { failed = true; }
            }
            Check(failed && edits == 1, "Failure was not reached after shortcut changes");
            Check(File.Exists(startup) == startupInitially, "Startup existence was not restored");
            if (startupInitially) Check(File.ReadAllText(startup) == "original startup", "Startup bytes were not restored");
            Check(File.ReadAllText(desktop) == "original desktop" && File.ReadAllText(executable) == "original executable", "Desktop/executable rollback failed");
            Check(File.ReadAllText(Program.ConfigPath) == config && !File.Exists(Program.ConfigPath + ".tmp"), "Config/temp rollback failed");
            Check(Directory.GetFiles(root, "custom-*.ico").Length == 0, "Failed save left a new custom icon");
            Check(context.Registered && context.Settings.Key == (uint)Keys.F10 && context.Settings.Startup == startupInitially, "In-memory settings changed on failure");
            Check(!Available((uint)Keys.F10) && Available((uint)Keys.F9), "Failed save lost the old hotkey or kept the candidate");
            results.Add("PASS: " + (shortcutFailure ? "partial shortcut failure" : "locked configuration while " + (startupInitially ? "disabling" : "enabling") + " startup") + " restores files, removes staged icon and preserves old hotkey");
        } finally { context.ExitThread(); context.Dispose(); }
    }
    static void FailedRollback(List<string> results, string root) {
        Seed(root, false); string first = Path.Combine(root, "first.lnk"), second = Path.Combine(root, "second.lnk");
        File.WriteAllText(first, "original first"); File.WriteAllText(second, "original second");
        FileStream locked = null;
        var context = new ToggleContext(true, delegate {
            File.WriteAllText(first, "changed first"); File.WriteAllText(second, "changed second");
            locked = new FileStream(second, FileMode.Open, FileAccess.Read, FileShare.None);
            throw new IOException("Injected save failure");
        }, new[] { first, second });
        try {
            var next = context.Settings.Copy(); next.Key = (uint)Keys.F9;
            bool reported = false;
            try { context.Apply(next, null); } catch (InvalidOperationException ex) { var errors = ex.InnerException as AggregateException; reported = errors != null && errors.InnerExceptions.Count >= 2; }
            Check(reported, "Rollback failure was swallowed");
            Check(File.ReadAllText(first) == "original first", "Rollback did not continue after one file failed");
            Check(context.Settings.Key == (uint)Keys.F10 && !Available((uint)Keys.F10) && Available((uint)Keys.F9), "Rollback failure damaged hotkeys");
            results.Add("PASS: rollback errors are reported, other files are still restored and old hotkey remains active");
        } finally { if (locked != null) locked.Dispose(); context.ExitThread(); context.Dispose(); }
    }
    sealed class Probe : NativeWindow, IDisposable { public Probe() { CreateHandle(new CreateParams()); } public void Dispose() { DestroyHandle(); } }
}
