using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

static class UI {
    public static Color Ink = Color.FromArgb(32, 33, 35), Muted = Color.FromArgb(105, 107, 111), Soft = Color.FromArgb(246, 246, 247);
    public static Button Button(string text, bool primary) {
        var button = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16, 8, 16, 8), FlatStyle = FlatStyle.Flat, BackColor = primary ? Ink : Soft, ForeColor = primary ? Color.White : Ink, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 4), MinimumSize = new Size(110, 38) };
        button.FlatAppearance.BorderSize = 0; return button;
    }
    public static Label Label(string text, bool title) { return new Label { Text = text, AutoSize = true, ForeColor = title ? Ink : Muted, Font = new Font("Segoe UI", title ? 11 : 9.5f, title ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0, 0, 0, 8), Dock = DockStyle.Top }; }
    public static TableLayoutPanel Stack() { return new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(0), Margin = new Padding(0) }; }
    public static void Add(TableLayoutPanel stack, Control control) { stack.RowCount++; stack.RowStyles.Add(new RowStyle(SizeType.AutoSize)); stack.Controls.Add(control, 0, stack.RowCount - 1); }
    public static Panel Divider() { return new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Color.FromArgb(233, 234, 235), Margin = new Padding(0, 16, 0, 20) }; }
    public static FlowLayoutPanel Flow(params Control[] controls) { var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, Margin = new Padding(0), Padding = new Padding(0) }; panel.Controls.AddRange(controls); return panel; }
}

sealed class SettingsForm : Form {
    ToggleContext context;
    Preferences pending;
    byte[] iconBytes;
    public bool AllowExit;
    Label subtitle, status, statusHelp, shortcutTitle, shortcutHelp, startupHelp, iconTitle, iconHelp, feedback;
    Button toggle, choose, restore, desktop, save, hide;
    TextBox shortcut;
    CheckBox startup;
    PictureBox preview;
    ComboBox language;
    TableLayoutPanel content;
    public SettingsForm(ToggleContext value) {
        context = value; pending = context.Settings.Copy();
        Text = "Taskbar Toggle"; Icon = context.CurrentIcon; StartPosition = FormStartPosition.CenterScreen;
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(690, 680); MinimumSize = new Size(540, 560); Font = new Font("Segoe UI", 10); BackColor = Color.White; ForeColor = UI.Ink;
        var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(30, 24, 30, 20) }; Controls.Add(viewport);
        content = UI.Stack(); viewport.Controls.Add(content);
        var title = UI.Label("Taskbar Toggle", true); title.Font = new Font("Segoe UI", 25, FontStyle.Bold);
        language = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Margin = new Padding(16, 10, 0, 0) }; language.Items.AddRange(new object[] { "中文", "English" }); language.SelectedIndex = pending.Language == "zh" ? 0 : 1;
        UI.Add(content, UI.Flow(title, language));
        subtitle = UI.Label("", false); UI.Add(content, subtitle); UI.Add(content, UI.Divider());
        status = UI.Label("", true); UI.Add(content, status);
        statusHelp = UI.Label("", false); UI.Add(content, statusHelp);
        toggle = UI.Button("", true); toggle.Click += delegate { Try(delegate { context.Toggle(); }); }; UI.Add(content, UI.Flow(toggle));
        UI.Add(content, UI.Divider());
        shortcutTitle = UI.Label("", true); UI.Add(content, shortcutTitle);
        shortcut = new TextBox { ReadOnly = true, Dock = DockStyle.Top, Font = new Font("Segoe UI", 13), BackColor = UI.Soft, BorderStyle = BorderStyle.FixedSingle, Text = Program.HotkeyText(pending.Modifiers, pending.Key), AccessibleName = "Shortcut / 快捷键", Margin = new Padding(0, 0, 0, 10) }; UI.Add(content, shortcut); shortcut.KeyDown += CaptureShortcut;
        shortcutHelp = UI.Label("", false); UI.Add(content, shortcutHelp);
        UI.Add(content, UI.Divider());
        startup = new CheckBox { AutoSize = true, Checked = pending.Startup, Margin = new Padding(0, 0, 0, 8), Font = new Font(Font, FontStyle.Bold) }; UI.Add(content, startup);
        startupHelp = UI.Label("", false); UI.Add(content, startupHelp);
        UI.Add(content, UI.Divider());
        iconTitle = UI.Label("", true); UI.Add(content, iconTitle);
        iconHelp = UI.Label("", false); UI.Add(content, iconHelp);
        preview = new PictureBox { Width = 48, Height = 48, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 0, 18, 8), AccessibleName = "Icon preview / 图标预览" }; SetPreview(context.CurrentIcon.ToBitmap());
        choose = UI.Button("", false); restore = UI.Button("", false);
        choose.Click += delegate { ChooseIcon(); };
        restore.Click += delegate { pending.IconPath = ""; iconBytes = null; using (var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)) SetPreview(icon.ToBitmap()); feedback.Text = T("点保存应用默认图标。", "Save to apply the default icon."); };
        UI.Add(content, UI.Flow(preview, choose, restore)); UI.Add(content, UI.Divider());
        desktop = UI.Button("", false); hide = UI.Button("", false); save = UI.Button("", true);
        desktop.Click += delegate { if (Apply()) Try(delegate { Shortcuts.Desktop(context.Settings.IconPath); feedback.Text = T("桌面快捷方式已创建。", "Desktop shortcut created."); }); };
        hide.Click += delegate { Hide(); }; save.Click += delegate { Apply(); };
        UI.Add(content, UI.Flow(save, desktop, hide));
        feedback = UI.Label("", false); feedback.Margin = new Padding(0, 12, 0, 8); UI.Add(content, feedback);
        language.SelectedIndexChanged += delegate { pending.Language = language.SelectedIndex == 0 ? "zh" : "en"; Translate(); };
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!AllowExit) { e.Cancel = true; Hide(); } };
        Activated += delegate { RefreshState(); };
        viewport.Resize += delegate { WrapLabels(); };
        Translate(); WrapLabels();
        Shown += delegate {
            var area = Screen.FromControl(this).WorkingArea;
            float scale; using (var graphics = CreateGraphics()) scale = graphics.DpiX / 96f;
            MinimumSize = new Size(Math.Min((int)(540 * scale), area.Width), Math.Min((int)(560 * scale), area.Height));
            ClientSize = new Size(Math.Min((int)(690 * scale), area.Width - 24), Math.Min(content.PreferredSize.Height + viewport.Padding.Vertical + 12, area.Height - 48));
            Location = new Point(area.X + (area.Width - Width) / 2, area.Y + (area.Height - Height) / 2);
            toggle.Select(); viewport.AutoScrollPosition = Point.Empty;
        };
    }
    string T(string cn, string en) { return pending.Language == "zh" ? cn : en; }
    void WrapLabels() { foreach (var label in new[] { subtitle, statusHelp, shortcutTitle, shortcutHelp, startupHelp, iconHelp, feedback }) if (label != null) label.MaximumSize = new Size(Math.Max(200, content.Parent.ClientSize.Width - content.Parent.Padding.Horizontal - 20), 0); }
    void Translate() {
        subtitle.Text = T("任务栏自动隐藏，按你的习惯设置。", "Taskbar auto-hide, on your terms.");
        shortcutTitle.Text = T("快捷键", "Keyboard shortcut");
        shortcutHelp.Text = T("点击输入框，按下新组合，再保存。若已被占用，会提示你更换。", "Click the field, press a new combination, then save. Conflicts are checked.");
        startup.Text = T("开机自启", "Start at login");
        startupHelp.Text = T("建议保持勾选，登录后快捷键随时可用。重点：点“保存设置”才生效，程序会复制到用户本地目录；登录时只在托盘运行。取消勾选并保存可关闭自启。", "Recommended: keep enabled so your shortcut is ready after sign-in. Important: click Save settings to apply. The app copies itself to your local user folder and starts quietly in the tray. Uncheck and save to disable.");
        iconTitle.Text = T("图标", "Icon"); iconHelp.Text = T("自选图片，拖动和缩放裁剪区域，选择输出尺寸。应用到托盘和桌面快捷方式。", "Choose a picture, adjust the crop and select a size. Applies to the tray and desktop shortcut.");
        choose.Text = T("选择图片…", "Choose image…"); restore.Text = T("恢复默认", "Use default");
        desktop.Text = T("桌面快捷方式", "Desktop shortcut"); save.Text = T("保存设置", "Save settings"); hide.Text = T("收起到托盘", "Hide to tray");
        feedback.Text = T("关闭窗口仍会在托盘运行。右键托盘图标可退出。", "Closing this window keeps the tool running. Right-click the tray icon to exit.");
        RefreshState(); WrapLabels();
    }
    public void RefreshState() {
        bool enabled = Native.AutoHide; status.Text = T("自动隐藏 · ", "Auto-hide · ") + (enabled ? T("已开启", "On") : T("已关闭", "Off"));
        toggle.Text = enabled ? T("关闭自动隐藏", "Turn auto-hide off") : T("开启自动隐藏", "Turn auto-hide on");
        statusHelp.Text = T("开启后，鼠标移到屏幕底部会显示任务栏。", "When enabled, move the pointer to the bottom edge to reveal the taskbar.");
        if (!context.Registered) feedback.Text = T("当前快捷键被占用，请设置新组合并保存。", "The current shortcut is in use. Record another combination and save.");
    }
    void CaptureShortcut(object sender, KeyEventArgs e) {
        if (e.Modifiers == Keys.None && (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)) return;
        e.SuppressKeyPress = true; e.Handled = true;
        if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin) return;
        uint mods = (uint)((e.Control ? 2 : 0) | (e.Alt ? 1 : 0) | (e.Shift ? 4 : 0));
        if (mods == 0 || e.KeyCode == Keys.F12) { feedback.Text = T("请使用 Ctrl、Alt 或 Shift 加一个按键（F12 除外）。", "Use Ctrl, Alt or Shift with a key (except F12)."); return; }
        pending.Modifiers = mods; pending.Key = (uint)e.KeyCode; shortcut.Text = Program.HotkeyText(mods, pending.Key); feedback.Text = T("新组合已录入，点保存生效。", "Shortcut recorded. Save to apply.");
    }
    void SetPreview(Image image) { var old = preview.Image; preview.Image = image; if (old != null) old.Dispose(); }
    void ChooseIcon() {
        using (var dialog = new OpenFileDialog { Title = T("选择图片", "Choose an image"), Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|All files|*.*" }) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            Try(delegate {
                using (Bitmap image = Icons.Read(dialog.FileName)) using (var crop = new CropForm(image, pending.Language)) {
                    if (crop.ShowDialog(this) != DialogResult.OK) return;
                    iconBytes = crop.IconBytes; using (var memory = new MemoryStream(iconBytes)) using (var icon = new Icon(memory, 48, 48)) SetPreview(icon.ToBitmap());
                    feedback.Text = T("预览已更新，保存后生效。", "Preview updated. Save to apply.");
                }
            });
        }
    }
    bool Apply() {
        try { pending.Startup = startup.Checked; context.Apply(pending.Copy(), iconBytes); pending = context.Settings.Copy(); iconBytes = null; feedback.Text = T("已保存，设置立即生效。", "Saved. Your settings are active."); return true; }
        catch (Exception ex) { feedback.Text = ex.Message; MessageBox.Show(this, ex.Message, "Taskbar Toggle", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
    }
    void Try(Action action) { try { action(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Taskbar Toggle", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
    protected override void Dispose(bool disposing) { if (disposing && preview != null && preview.Image != null) { preview.Image.Dispose(); preview.Image = null; } base.Dispose(disposing); }
}
