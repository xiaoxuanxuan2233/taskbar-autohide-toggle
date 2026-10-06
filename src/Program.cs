using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

[assembly: System.Reflection.AssemblyVersion("2.0.2.0")]
[assembly: System.Reflection.AssemblyFileVersion("2.0.2.0")]
[assembly: System.Reflection.AssemblyTitle("Taskbar Toggle")]

public class Preferences {
    public uint Modifiers = 3, Key = (uint)Keys.Z;
    public string Language = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh") ? "zh" : "en";
    public string IconPath = "";
    public bool Startup = true;
    public Preferences Copy() { return (Preferences)MemberwiseClone(); }
    public static Preferences Load() {
        try {
            using (var file = File.OpenRead(Program.ConfigPath)) {
                var value = (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(file);
                if (value.Modifiers < 1 || value.Modifiers > 7 || value.Key < 1 || value.Key > 255 || value.Key == (uint)Keys.F12) return new Preferences();
                value.IconPath = value.IconPath ?? ""; return value;
            }
        } catch { return new Preferences(); }
    }
    public void Save() {
        Directory.CreateDirectory(Program.DataPath);
        string temporary = Program.ConfigPath + ".tmp";
        using (var file = File.Create(temporary)) new XmlSerializer(typeof(Preferences)).Serialize(file, this);
        if (File.Exists(Program.ConfigPath)) File.Replace(temporary, Program.ConfigPath, null);
        else File.Move(temporary, Program.ConfigPath);
    }
}

static class Native {
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct APPBARDATA { public uint Size; public IntPtr Window; public uint Callback, Edge; public RECT Rect; public IntPtr Param; }
    [DllImport("shell32.dll")] static extern UIntPtr SHAppBarMessage(uint message, ref APPBARDATA data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr wp, IntPtr lp);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern void SHChangeNotify(uint evt, uint flags, string path, IntPtr extra);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern uint GetFinalPathNameByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle handle, StringBuilder path, uint size, uint flags);
    public static string ActualPath(string path) {
        using (var file = File.OpenRead(path)) {
            var output = new StringBuilder(4096);
            if (GetFinalPathNameByHandle(file.SafeFileHandle, output, 4096, 0) == 0) return Path.GetFullPath(path);
            string value = output.ToString();
            return value.StartsWith(@"\\?\UNC\") ? @"\\" + value.Substring(8) : value.StartsWith(@"\\?\") ? value.Substring(4) : value;
        }
    }
    public static bool AutoHide {
        get { var data = new APPBARDATA(); data.Size = (uint)Marshal.SizeOf(data); data.Window = FindWindow("Shell_TrayWnd", null); if (data.Window == IntPtr.Zero) throw new InvalidOperationException("Windows taskbar is unavailable."); return (SHAppBarMessage(4, ref data).ToUInt64() & 1) != 0; }
        set {
            var data = new APPBARDATA(); data.Size = (uint)Marshal.SizeOf(data); data.Window = FindWindow("Shell_TrayWnd", null);
            if (data.Window == IntPtr.Zero) throw new InvalidOperationException("Windows taskbar is unavailable.");
            uint state = (uint)SHAppBarMessage(4, ref data).ToUInt64();
            data.Param = new IntPtr((long)(value ? state | 1u : state & ~1u)); SHAppBarMessage(10, ref data);
        }
    }
}

static class Program {
    public const string ListenerTitle = "TaskbarToggle.V2.Listener";
    public static string DataPath = ResolveDataPath();
    static string ResolveDataPath() {
        string installedSettings = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "settings.xml");
        if (File.Exists(installedSettings)) return Path.GetDirectoryName(Native.ActualPath(installedSettings));
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskbarToggle");
    }
    public static string ConfigPath { get { return Path.Combine(DataPath, "settings.xml"); } }
    public static string HotkeyText(uint modifiers, uint key) {
        string text = ((modifiers & 2) != 0 ? "Ctrl + " : "") + ((modifiers & 1) != 0 ? "Alt + " : "") + ((modifiers & 4) != 0 ? "Shift + " : "");
        return text + (key >= (uint)Keys.D0 && key <= (uint)Keys.D9 ? ((char)key).ToString() : ((Keys)key).ToString());
    }
    [STAThread] static void Main(string[] args) {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try {
            if (args.Length > 0 && args[0] == "--self-test") { SelfTests.Run(args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "taskbar-toggle-tests.txt")); return; }
            if (args.Length > 1 && args[0] == "--test-data") { DataPath = Path.GetFullPath(args[1]); args = new string[0]; }
            Directory.CreateDirectory(DataPath);
            if (args.Length > 0 && args[0] == "--status") { File.WriteAllText(Path.Combine(DataPath, "status.txt"), "AutoHide=" + Native.AutoHide); return; }
            if (args.Length > 0 && (args[0] == "--toggle" || args[0] == "--on" || args[0] == "--off")) { Native.AutoHide = args[0] == "--toggle" ? !Native.AutoHide : args[0] == "--on"; return; }
            IntPtr existing = Native.FindWindow(null, ListenerTitle);
            if (args.Length > 0 && args[0] == "--exit") { if (existing != IntPtr.Zero) Native.PostMessage(existing, 0x8002, IntPtr.Zero, IntPtr.Zero); return; }
            if (args.Length > 0 && args[0] == "--test-hotkey") { if (existing == IntPtr.Zero) throw new Exception("Listener missing"); Native.PostMessage(existing, 0x8003, IntPtr.Zero, IntPtr.Zero); return; }
            bool isNew;
            using (var mutex = new Mutex(true, @"Local\TaskbarToggle.V2", out isNew)) {
                if (!isNew) { if (existing != IntPtr.Zero && !(args.Length > 0 && args[0] == "--background")) Native.PostMessage(existing, 0x8001, IntPtr.Zero, IntPtr.Zero); return; }
                using (var context = new ToggleContext(args.Length > 0 && args[0] == "--background")) Application.Run(context);
            }
        } catch (Exception ex) {
            try { Directory.CreateDirectory(DataPath); File.WriteAllText(Path.Combine(DataPath, "error.txt"), ex.ToString()); } catch { }
            MessageBox.Show(ex.Message, "Taskbar Toggle", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

static class Shortcuts {
    public static string StartupPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Taskbar Toggle.lnk"); } }
    public static string DesktopPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Taskbar Toggle.lnk"); } }
    public static string StableExe() {
        string source = Native.ActualPath(Application.ExecutablePath);
        Directory.CreateDirectory(Program.DataPath);
        string destination = Path.Combine(Program.DataPath, "TaskbarToggle.exe");
        if (!File.Exists(destination) || !String.Equals(Native.ActualPath(destination), source, StringComparison.OrdinalIgnoreCase)) File.Copy(source, destination, true);
        return Native.ActualPath(destination);
    }
    static void Write(string path, string target, string args, string icon) {
        object shell = null, link = null;
        try {
            shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            link = shell.GetType().InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { path });
            var type = link.GetType();
            foreach (var pair in new string[][] { new[] { "TargetPath", target }, new[] { "Arguments", args }, new[] { "WorkingDirectory", Path.GetDirectoryName(target) }, new[] { "IconLocation", (icon.Length > 0 ? icon : target) + ",0" }, new[] { "Description", "Taskbar Toggle settings / 任务栏自动隐藏设置" } }) type.InvokeMember(pair[0], System.Reflection.BindingFlags.SetProperty, null, link, new object[] { pair[1] });
            type.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, link, null);
            Native.SHChangeNotify(0x2000, 5, path, IntPtr.Zero);
        } finally { if (link != null) Marshal.FinalReleaseComObject(link); if (shell != null) Marshal.FinalReleaseComObject(shell); }
    }
    public static void Startup(bool enabled, string icon) {
        if (enabled) Write(StartupPath, StableExe(), "--background", icon);
        else if (File.Exists(StartupPath)) { File.Delete(StartupPath); Native.SHChangeNotify(0x4, 5, StartupPath, IntPtr.Zero); }
    }
    public static void Desktop(string icon) { Write(DesktopPath, StableExe(), "", icon); }
    public static void RefreshDesktop(string icon) { if (File.Exists(DesktopPath)) Desktop(icon); }
    public static void Apply(Preferences next) { Startup(next.Startup, next.IconPath); RefreshDesktop(next.IconPath); }
    public static string[] TransactionPaths { get { return new[] { StartupPath, DesktopPath, Path.Combine(Program.DataPath, "TaskbarToggle.exe") }; } }
}

// Snapshots are created before any writes, and used only during an explicit save.
// A failed rollback is reported; it must never be mistaken for a successful save.
sealed class SettingsTransaction {
    sealed class Snapshot {
        public string Path;
        public byte[] Bytes;
        public Snapshot(string path) { Path = path; Bytes = File.Exists(path) ? File.ReadAllBytes(path) : null; }
        public void Restore() {
            bool exists = File.Exists(Path);
            if (Bytes == null) { if (exists) { File.Delete(Path); Native.SHChangeNotify(0x4, 5, Path, IntPtr.Zero); } return; }
            if (exists) {
                byte[] current = File.ReadAllBytes(Path);
                bool same = current.Length == Bytes.Length;
                for (int i = 0; same && i < current.Length; i++) same = current[i] == Bytes[i];
                if (same) return;
            }
            File.WriteAllBytes(Path, Bytes); Native.SHChangeNotify(0x2000, 5, Path, IntPtr.Zero);
        }
    }
    public static void Save(Preferences next, byte[] iconBytes, Action<Preferences> updateShortcuts, string[] shortcutPaths) {
        var snapshots = new System.Collections.Generic.List<Snapshot>();
        foreach (string path in shortcutPaths) snapshots.Add(new Snapshot(path));
        snapshots.Add(new Snapshot(Program.ConfigPath)); snapshots.Add(new Snapshot(Program.ConfigPath + ".tmp"));
        string newIcon = null;
        try {
            if (iconBytes != null) {
                newIcon = Path.Combine(Program.DataPath, "custom-" + Guid.NewGuid().ToString("N") + ".ico");
                File.WriteAllBytes(newIcon, iconBytes); next.IconPath = Native.ActualPath(newIcon);
            }
            updateShortcuts(next); next.Save();
        } catch (Exception original) {
            var failures = new System.Collections.Generic.List<Exception>();
            for (int i = snapshots.Count - 1; i >= 0; i--) try { snapshots[i].Restore(); } catch (Exception ex) { failures.Add(ex); }
            if (newIcon != null) try { if (File.Exists(newIcon)) File.Delete(newIcon); } catch (Exception ex) { failures.Add(ex); }
            if (failures.Count > 0) {
                failures.Insert(0, original);
                throw new InvalidOperationException(next.Language == "zh" ? "保存失败，部分文件未能恢复。请检查文件权限后重新保存。" : "Save failed and some files could not be restored. Check file permissions and save again.", new AggregateException(failures));
            }
            throw;
        }
    }
}

sealed class ToggleContext : ApplicationContext {
    public Preferences Settings;
    public bool Registered;
    public int HotkeyId = 1;
    public Icon CurrentIcon;
    NotifyIcon tray;
    Listener listener;
    SettingsForm form;
    Action<Preferences> persistShortcuts;
    string[] shortcutPaths;
    Action<string> createDesktop;
    Action toggleTaskbar;
    Func<bool> readTaskbar;
    Action<string, string> showWarning;
    DateTime lastToggleWarning = DateTime.MinValue;
    bool exiting;
    public string T(string cn, string en) { return Settings.Language == "zh" ? cn : en; }
    public ToggleContext(bool background) : this(background, Shortcuts.Apply, Shortcuts.TransactionPaths) { }
    internal ToggleContext(bool background, Action<Preferences> updateShortcuts, string[] files)
        : this(background, updateShortcuts, files, Shortcuts.Desktop, delegate { Native.AutoHide = !Native.AutoHide; }, delegate { return Native.AutoHide; }, null) { }
    internal ToggleContext(bool background, Action<Preferences> updateShortcuts, string[] files, Action<string> desktopAction, Action toggleAction, Func<bool> readAction, Action<string, string> warningAction) {
        persistShortcuts = updateShortcuts; shortcutPaths = files;
        createDesktop = desktopAction; toggleTaskbar = toggleAction; readTaskbar = readAction;
        showWarning = warningAction ?? delegate(string title, string message) { tray.ShowBalloonTip(4000, title, message, ToolTipIcon.Warning); };
        Settings = Preferences.Load(); listener = new Listener(this);
        Registered = Native.RegisterHotKey(listener.Handle, HotkeyId, Settings.Modifiers | 0x4000, Settings.Key);
        LoadIcon(); tray = new NotifyIcon { Icon = CurrentIcon, Visible = true }; tray.DoubleClick += delegate { ShowSettings(); }; UpdateTray(); WriteReady();
        if (!background || !Registered) ShowSettings();
    }
    void WriteReady() {
        try { File.WriteAllText(Path.Combine(Program.DataPath, "ready.txt"), "Hotkey=" + Program.HotkeyText(Settings.Modifiers, Settings.Key) + "; Registered=" + Registered + "; PID=" + Process.GetCurrentProcess().Id); }
        catch (Exception ex) { Debug.WriteLine("Taskbar Toggle diagnostic write failed: " + ex.Message); }
    }
    void LoadIcon() {
        Icon replacement;
        try { replacement = Settings.IconPath.Length > 0 ? new Icon(Settings.IconPath, 32, 32) : Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { replacement = (Icon)SystemIcons.Application.Clone(); }
        Icon old = CurrentIcon; CurrentIcon = replacement;
        if (tray != null) tray.Icon = CurrentIcon; if (form != null && !form.IsDisposed) form.Icon = CurrentIcon;
        if (old != null) old.Dispose();
    }
    void UpdateTray() {
        var menu = new ContextMenuStrip();
        menu.Items.Add(T("打开设置", "Settings"), null, delegate { ShowSettings(); });
        menu.Items.Add(T("切换自动隐藏", "Toggle auto-hide"), null, delegate { Toggle(); });
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(T("退出", "Exit"), null, delegate { ExitThread(); });
        var old = tray.ContextMenuStrip; tray.ContextMenuStrip = menu; if (old != null) old.Dispose();
        string caption = "Taskbar Toggle · " + Program.HotkeyText(Settings.Modifiers, Settings.Key); tray.Text = caption.Substring(0, Math.Min(63, caption.Length));
    }
    public void ShowSettings() {
        if (form == null || form.IsDisposed) form = new SettingsForm(this);
        form.Show(); if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
        form.Activate(); Native.SetForegroundWindow(form.Handle); form.RefreshState();
    }
    public void SettingsClosed(SettingsForm closed) { if (form == closed) form = null; ExitThread(); }
    public void CreateDesktopShortcut() { createDesktop(Settings.IconPath); }
    public bool TryReadAutoHide(out bool enabled) {
        try { enabled = readTaskbar(); return true; }
        catch (Exception ex) { Debug.WriteLine("Taskbar Toggle status unavailable: " + ex.Message); enabled = false; return false; }
    }
    public bool Toggle() {
        try { toggleTaskbar(); if (form != null && !form.IsDisposed) form.RefreshState(); return true; }
        catch (Exception ex) {
            Debug.WriteLine("Taskbar Toggle switch failed: " + ex.Message);
            try { if (form != null && !form.IsDisposed) form.ReportToggleFailure(); } catch (Exception displayError) { Debug.WriteLine(displayError.Message); }
            // Non-modal, rate-limited notification: never interrupt the background message loop.
            if ((DateTime.UtcNow - lastToggleWarning).TotalSeconds >= 10) {
                lastToggleWarning = DateTime.UtcNow;
                try { showWarning(T("暂时无法切换任务栏", "Unable to toggle taskbar"), T("任务栏暂时不可用，资源管理器可能正在重启。请稍后重试，工具仍在运行。", "The taskbar may be unavailable while Explorer restarts. Try again shortly; the tool is still running.")); }
                catch (Exception notificationError) { Debug.WriteLine(notificationError.Message); }
            }
            return false;
        }
    }
    public void Apply(Preferences next, byte[] iconBytes) {
        if (next.Modifiers == 0 || next.Key == 0) throw new InvalidOperationException(T("请使用 Ctrl、Alt 或 Shift 加一个按键。", "Use Ctrl, Alt or Shift together with a key."));
        bool changed = !Registered || next.Modifiers != Settings.Modifiers || next.Key != Settings.Key;
        int candidate = HotkeyId == 1 ? 2 : 1;
        if (changed && !Native.RegisterHotKey(listener.Handle, candidate, next.Modifiers | 0x4000, next.Key)) throw new InvalidOperationException(T("这个快捷键已被其他程序占用，请换一个。", "This shortcut is already in use. Choose another combination."));
        try { SettingsTransaction.Save(next, iconBytes, persistShortcuts, shortcutPaths); }
        catch { if (changed) Native.UnregisterHotKey(listener.Handle, candidate); throw; }
        // Persistence has committed. No later diagnostic failure may undo this registration.
        if (changed) { if (Registered) Native.UnregisterHotKey(listener.Handle, HotkeyId); HotkeyId = candidate; }
        string previousIcon = Settings.IconPath;
        Registered = true; Settings = next; LoadIcon(); UpdateTray(); WriteReady();
        Icons.CleanupPrevious(previousIcon, Settings.IconPath);
    }
    protected override void ExitThreadCore() {
        if (exiting) return; exiting = true;
        Native.UnregisterHotKey(listener.Handle, 1); Native.UnregisterHotKey(listener.Handle, 2);
        if (form != null) { form.AllowExit = true; form.Close(); form.Dispose(); }
        tray.Visible = false; tray.ContextMenuStrip.Dispose(); tray.Dispose(); CurrentIcon.Dispose(); listener.DestroyHandle(); base.ExitThreadCore();
    }
}

sealed class Listener : NativeWindow {
    ToggleContext context;
    public Listener(ToggleContext value) { context = value; var p = new CreateParams(); p.Caption = Program.ListenerTitle; CreateHandle(p); }
    protected override void WndProc(ref Message message) {
        if (message.Msg == 0x312 && message.WParam == new IntPtr(context.HotkeyId)) context.Toggle();
        else if (message.Msg == 0x8001) context.ShowSettings(); else if (message.Msg == 0x8002) context.ExitThread(); else if (message.Msg == 0x8003) context.Toggle();
        base.WndProc(ref message);
    }
}
