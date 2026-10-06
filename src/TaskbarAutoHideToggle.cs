using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

static class Taskbar {
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)] struct APPBARDATA { public uint cbSize; public IntPtr hWnd; public uint callback; public uint edge; public RECT rect; public IntPtr lParam; }
    [DllImport("shell32.dll")] static extern UIntPtr SHAppBarMessage(uint message, ref APPBARDATA data);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll", SetLastError=true)] public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint msg, IntPtr wParam, IntPtr lParam);
    public static string Dir = Path.GetDirectoryName(Application.ExecutablePath);
    public static uint State() { var d = new APPBARDATA(); d.cbSize = (uint)Marshal.SizeOf(d); return (uint)SHAppBarMessage(4, ref d).ToUInt64(); }
    public static void Set(bool hide) { var d = new APPBARDATA(); d.cbSize = (uint)Marshal.SizeOf(d); d.hWnd = FindWindow("Shell_TrayWnd", null); if(d.hWnd == IntPtr.Zero) throw new Exception("Windows taskbar not found"); uint current=State(); d.lParam = new IntPtr((long)(hide ? current | 1u : current & ~1u)); SHAppBarMessage(10, ref d); }
    [STAThread] static void Main(string[] args) {
        try {
            if(args.Length > 0) {
                if(args[0]=="--toggle") Set((State() & 1)==0);
                else if(args[0]=="--on") Set(true);
                else if(args[0]=="--off") Set(false);
                else if(args[0]=="--test-toggle") { var w=FindWindow(null,"TaskbarAutoHideToggle.Listener"); if(w==IntPtr.Zero) throw new Exception("Listener not found"); PostMessage(w,0x312,new IntPtr(1),IntPtr.Zero); }
                File.WriteAllText(Path.Combine(Dir,"query.txt"), "AutoHide="+((State() & 1)!=0)); return;
            }
            bool created;
            using(var mutex=new Mutex(true,"Local\\TaskbarAutoHideToggle",out created)) {
                if(!created) return;
                Application.EnableVisualStyles();
                using(var context=new ToggleContext()) Application.Run(context);
            }
        } catch(Exception ex) { File.WriteAllText(Path.Combine(Dir,"error.txt"),ex.ToString()); }
    }
}
sealed class ToggleContext : ApplicationContext {
    Listener listener;
    NotifyIcon tray;
    public ToggleContext() {
        listener=new Listener(this);
        if(!Taskbar.RegisterHotKey(listener.Handle,1,0x4003,0x5A)) throw new Exception("Ctrl+Alt+Z registration failed: "+Marshal.GetLastWin32Error());
        tray=new NotifyIcon(); tray.Icon=new Icon(Path.Combine(Taskbar.Dir,"taskbar.ico"),new Size(32,32)); tray.Text="任务栏自动隐藏：Ctrl + Alt + Z";
        var menu=new ContextMenuStrip();
        menu.Items.Add("切换自动隐藏 (Ctrl + Alt + Z)",null,delegate { Toggle(); });
        menu.Items.Add("退出快捷键工具",null,delegate { ExitThread(); });
        tray.ContextMenuStrip=menu; tray.DoubleClick+=delegate { Toggle(); }; tray.Visible=true;
        File.WriteAllText(Path.Combine(Taskbar.Dir,"ready.txt"),"Hotkey=Ctrl+Alt+Z; Registered=True; PID="+System.Diagnostics.Process.GetCurrentProcess().Id);
    }
    public void Toggle() { Taskbar.Set((Taskbar.State() & 1)==0); File.WriteAllText(Path.Combine(Taskbar.Dir,"last-toggle.txt"),"AutoHide="+((Taskbar.State() & 1)!=0)); }
    protected override void ExitThreadCore() { Taskbar.UnregisterHotKey(listener.Handle,1); tray.Icon.Dispose(); tray.Dispose(); listener.DestroyHandle(); base.ExitThreadCore(); }
}
sealed class Listener : NativeWindow {
    ToggleContext context;
    public Listener(ToggleContext value) { context=value; var p=new CreateParams(); p.Caption="TaskbarAutoHideToggle.Listener"; CreateHandle(p); }
    protected override void WndProc(ref Message message) { if(message.Msg==0x312 && message.WParam==new IntPtr(1)) context.Toggle(); base.WndProc(ref message); }
}