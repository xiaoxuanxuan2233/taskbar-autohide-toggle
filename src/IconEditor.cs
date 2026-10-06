using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

static class Icons {
    public static Bitmap Read(string path) {
        using (var file = File.OpenRead(path)) {
            if (Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase)) { using (var icon = new Icon(file, 256, 256)) return icon.ToBitmap(); }
            using (var source = Image.FromStream(file)) {
                if (Array.IndexOf(source.PropertyIdList, 0x112) >= 0) {
                    int orientation = source.GetPropertyItem(0x112).Value[0];
                    var map = new[] { RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipX, RotateFlipType.Rotate180FlipNone, RotateFlipType.Rotate180FlipX, RotateFlipType.Rotate90FlipX, RotateFlipType.Rotate90FlipNone, RotateFlipType.Rotate270FlipX, RotateFlipType.Rotate270FlipNone };
                    if (orientation >= 1 && orientation <= 8) source.RotateFlip(map[orientation]);
                }
                return new Bitmap(source);
            }
        }
    }
    public static Bitmap Crop(Image image, Rectangle crop, int size) {
        if (size < 16 || size > 256 || crop.Width < 1 || crop.Height < 1 || crop.X < 0 || crop.Y < 0 || crop.Right > image.Width || crop.Bottom > image.Height) throw new ArgumentOutOfRangeException("crop");
        var output = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(output)) using (var attributes = new ImageAttributes()) {
            graphics.Clear(Color.Transparent); graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY); graphics.DrawImage(image, new Rectangle(0, 0, size, size), crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, attributes);
        }
        return output;
    }
    public static byte[] Build(Image image, Rectangle crop, int maxSize) {
        var sizes = new List<int>(); foreach (int size in new[] { 16, 24, 32, 48, 64, 128, 256 }) if (size <= maxSize) sizes.Add(size);
        var entries = new List<byte[]>();
        foreach (int size in sizes) using (Bitmap bitmap = Crop(image, crop, size)) using (var stream = new MemoryStream()) {
            if (size == 256) { bitmap.Save(stream, ImageFormat.Png); entries.Add(stream.ToArray()); continue; }
            // Write a 32-bit ICO bitmap directly: BGRA pixels followed by a padded AND mask.
            // Icon.FromHandle.Save can emit inconsistent headers for larger HICON images.
            using (var writer = new BinaryWriter(stream)) {
                int maskStride = ((size + 31) / 32) * 4;
                writer.Write(40); writer.Write(size); writer.Write(size * 2); writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(0); writer.Write(size * size * 4 + maskStride * size);
                writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                for (int y = size - 1; y >= 0; y--) for (int x = 0; x < size; x++) { Color pixel = bitmap.GetPixel(x, y); writer.Write(pixel.B); writer.Write(pixel.G); writer.Write(pixel.R); writer.Write(pixel.A); }
                for (int y = size - 1; y >= 0; y--) {
                    byte[] mask = new byte[maskStride];
                    for (int x = 0; x < size; x++) if (bitmap.GetPixel(x, y).A == 0) mask[x / 8] |= (byte)(0x80 >> (x % 8));
                    writer.Write(mask);
                }
                entries.Add(stream.ToArray());
            }
        }
        using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream)) {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Count); int offset = 6 + 16 * sizes.Count;
            for (int i = 0; i < sizes.Count; i++) { writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32); writer.Write(entries[i].Length); writer.Write(offset); offset += entries[i].Length; }
            foreach (byte[] entry in entries) writer.Write(entry); return stream.ToArray();
        }
    }
}

sealed class CropCanvas : Control {
    public Bitmap Source;
    public Rectangle Selection { get; private set; }
    public event EventHandler SelectionChanged;
    RectangleF imageBounds;
    PointF anchor;
    Rectangle original;
    int mode;
    float ScaleFactor { get { return Math.Min((float)Width / Source.Width, (float)Height / Source.Height); } }
    public CropCanvas(Bitmap image) {
        Source = image; DoubleBuffered = true; BackColor = Color.FromArgb(32, 33, 35); Cursor = Cursors.Cross; MinimumSize = new Size(240, 200);
        int side = Math.Min(image.Width, image.Height); Selection = new Rectangle((image.Width - side) / 2, (image.Height - side) / 2, side, side);
    }
    PointF Pixel(Point point) { return new PointF(Math.Max(0, Math.Min(Source.Width - 1, (point.X - imageBounds.X) / ScaleFactor)), Math.Max(0, Math.Min(Source.Height - 1, (point.Y - imageBounds.Y) / ScaleFactor))); }
    RectangleF ScreenSelection() { float scale = ScaleFactor; return new RectangleF(imageBounds.X + Selection.X * scale, imageBounds.Y + Selection.Y * scale, Selection.Width * scale, Selection.Height * scale); }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e); float scale = ScaleFactor; imageBounds = new RectangleF((Width - Source.Width * scale) / 2, (Height - Source.Height * scale) / 2, Source.Width * scale, Source.Height * scale);
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; e.Graphics.DrawImage(Source, imageBounds); RectangleF selected = ScreenSelection();
        using (var region = new Region(imageBounds)) using (var shade = new SolidBrush(Color.FromArgb(155, 0, 0, 0))) { region.Exclude(selected); e.Graphics.FillRegion(shade, region); }
        using (var pen = new Pen(Color.White, 2)) e.Graphics.DrawRectangle(pen, selected.X, selected.Y, selected.Width, selected.Height);
        e.Graphics.FillRectangle(Brushes.White, selected.Right - 6, selected.Bottom - 6, 12, 12);
    }
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e); if (e.Button != MouseButtons.Left || !imageBounds.Contains(e.Location)) return;
        Capture = true; anchor = Pixel(e.Location); original = Selection; RectangleF rectangle = ScreenSelection();
        mode = Math.Abs(e.X - rectangle.Right) < 16 && Math.Abs(e.Y - rectangle.Bottom) < 16 ? 2 : rectangle.Contains(e.Location) ? 1 : 3;
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e); if (!Capture || mode == 0) return; PointF point = Pixel(e.Location);
        if (mode == 1) Selection = new Rectangle(Math.Max(0, Math.Min(Source.Width - original.Width, original.X + (int)(point.X - anchor.X))), Math.Max(0, Math.Min(Source.Height - original.Height, original.Y + (int)(point.Y - anchor.Y))), original.Width, original.Height);
        else if (mode == 2) { int side = Math.Max(1, Math.Min(Math.Min(Source.Width - original.X, Source.Height - original.Y), (int)Math.Max(point.X - original.X, point.Y - original.Y))); Selection = new Rectangle(original.X, original.Y, side, side); }
        else {
            int side = Math.Max(1, (int)Math.Max(Math.Abs(point.X - anchor.X), Math.Abs(point.Y - anchor.Y)));
            int x = Math.Max(0, (int)(point.X < anchor.X ? anchor.X - side : anchor.X)), y = Math.Max(0, (int)(point.Y < anchor.Y ? anchor.Y - side : anchor.Y));
            side = Math.Min(side, Math.Min(Source.Width - x, Source.Height - y)); Selection = new Rectangle(x, y, side, side);
        }
        Changed();
    }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); mode = 0; Capture = false; }
    void Changed() { Invalidate(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty); }
    public void SetSelection(int x, int y, int side) { side = Math.Max(1, Math.Min(side, Math.Min(Source.Width, Source.Height))); Selection = new Rectangle(Math.Max(0, Math.Min(Source.Width - side, x)), Math.Max(0, Math.Min(Source.Height - side, y)), side, side); Changed(); }
}

sealed class CropForm : Form {
    public byte[] IconBytes;
    CropCanvas canvas;
    NumericUpDown x, y, side;
    ComboBox resolution;
    PictureBox preview;
    bool syncing, chinese;
    string T(string cn, string en) { return chinese ? cn : en; }
    public CropForm(Bitmap source, string language) {
        chinese = language == "zh"; Text = T("裁剪图标 · Taskbar Toggle", "Crop icon · Taskbar Toggle"); StartPosition = FormStartPosition.CenterParent;
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(820, 580); MinimumSize = new Size(660, 500); Font = new Font("Segoe UI", 10); BackColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(22) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); Controls.Add(layout);
        var hint = UI.Label(T("拖动方框移动；拖动右下角调整大小；也可输入精确像素。", "Drag to move; drag the lower-right corner to resize, or enter exact pixels."), false); layout.Controls.Add(hint, 0, 0); layout.SetColumnSpan(hint, 2);
        canvas = new CropCanvas(source) { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 18, 20) }; layout.Controls.Add(canvas, 0, 1);
        var right = UI.Stack(); right.Dock = DockStyle.Fill; right.AutoSize = false; right.Margin = new Padding(0, 8, 0, 0); layout.Controls.Add(right, 1, 1); UI.Add(right, UI.Label(T("预览", "Preview"), true));
        preview = new PictureBox { Width = 128, Height = 128, SizeMode = PictureBoxSizeMode.Zoom, BackColor = UI.Soft, Margin = new Padding(0, 0, 0, 20) }; UI.Add(right, preview);
        UI.Add(right, UI.Label(T("输出尺寸", "Output size"), true)); resolution = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 12) }; resolution.Items.AddRange(new object[] { "32 × 32", "64 × 64", "128 × 128", "256 × 256" }); resolution.SelectedIndex = 3; UI.Add(right, resolution);
        var sizeHelp = UI.Label(T("默认 256，包含小尺寸。小图片放大可能变模糊。", "Default: 256, with smaller sizes included. Enlarging small images can reduce clarity."), false); sizeHelp.MaximumSize = new Size(150, 0); UI.Add(right, sizeHelp); right.Resize += delegate { sizeHelp.MaximumSize = new Size(Math.Max(80, right.ClientSize.Width), 0); preview.Width = Math.Min(128, right.ClientSize.Width); };
        layout.Resize += delegate { hint.MaximumSize = new Size(Math.Max(200, layout.ClientSize.Width - layout.Padding.Horizontal), 0); };
        var coordinates = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
        for (int index = 0; index < 3; index++) coordinates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f)); layout.Controls.Add(coordinates, 0, 2); layout.SetColumnSpan(coordinates, 2);
        x = Number(coordinates, T("左 X", "Left X"), 0, source.Width - 1); y = Number(coordinates, T("上 Y", "Top Y"), 1, source.Height - 1); side = Number(coordinates, T("边长（像素）", "Size (pixels)"), 2, Math.Min(source.Width, source.Height)); side.Minimum = 1;
        EventHandler edited = delegate { if (!syncing) canvas.SetSelection((int)x.Value, (int)y.Value, (int)side.Value); }; x.ValueChanged += edited; y.ValueChanged += edited; side.ValueChanged += edited;
        canvas.SelectionChanged += delegate { Sync(); }; Sync();
        var cancel = UI.Button(T("取消", "Cancel"), false); cancel.DialogResult = DialogResult.Cancel; var use = UI.Button(T("使用图标", "Use icon"), true);
        use.Click += delegate { IconBytes = Icons.Build(source, canvas.Selection, new[] { 32, 64, 128, 256 }[resolution.SelectedIndex]); DialogResult = DialogResult.OK; Close(); };
        var buttons = UI.Flow(use, cancel); layout.Controls.Add(buttons, 0, 3); layout.SetColumnSpan(buttons, 2); CancelButton = cancel;
        Shown += delegate { var area = Screen.FromControl(this).WorkingArea; if (Width > area.Width || Height > area.Height) Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height)); };
    }
    NumericUpDown Number(TableLayoutPanel panel, string name, int column, int maximum) { var group = UI.Stack(); group.Margin = new Padding(0, 0, 16, 0); panel.Controls.Add(group, column, 0); UI.Add(group, UI.Label(name, false)); var number = new NumericUpDown { Dock = DockStyle.Top, Maximum = maximum, AccessibleName = name }; UI.Add(group, number); return number; }
    void Sync() { syncing = true; x.Value = canvas.Selection.X; y.Value = canvas.Selection.Y; side.Value = canvas.Selection.Width; syncing = false; var old = preview.Image; preview.Image = Icons.Crop(canvas.Source, canvas.Selection, 128); if (old != null) old.Dispose(); }
    protected override void Dispose(bool disposing) { if (disposing && preview != null && preview.Image != null) { preview.Image.Dispose(); preview.Image = null; } base.Dispose(disposing); }
}
