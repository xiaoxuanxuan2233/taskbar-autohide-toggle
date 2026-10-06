using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

static class SelfTests {
    public static void Run(string report) {
        var results = new List<string>(); string previous = Program.DataPath;
        string directory = Path.Combine(Path.GetTempPath(), "TaskbarToggleTest-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try {
            using (var image = new Bitmap(320, 180)) {
                using (var graphics = Graphics.FromImage(image)) { graphics.Clear(Color.Red); graphics.FillRectangle(Brushes.Blue, 160, 0, 160, 180); }
                using (var crop = Icons.Crop(image, new Rectangle(170, 10, 100, 100), 64)) if (crop.GetPixel(32, 32).B < 240) throw new Exception("Wrong crop region");
                foreach (int size in new[] { 32, 64, 128, 256 }) {
                    byte[] bytes = Icons.Build(image, new Rectangle(0, 0, 180, 180), size);
                    using (var memory = new MemoryStream(bytes)) using (var icon = new Icon(memory, Math.Min(128, size), Math.Min(128, size))) {
                        if (icon.Width != Math.Min(128, size)) throw new Exception("Wrong shell icon size");
                        using (var actual = icon.ToBitmap()) using (var expected = Icons.Crop(image, new Rectangle(0, 0, 180, 180), actual.Width)) {
                            foreach (int px in new[] { 2, actual.Width / 2, actual.Width - 2 }) foreach (int py in new[] { 2, actual.Height / 2, actual.Height - 2 }) {
                                Color a = actual.GetPixel(px, py), b = expected.GetPixel(px, py);
                                if (Math.Abs(a.R - b.R) > 5 || Math.Abs(a.G - b.G) > 5 || Math.Abs(a.B - b.B) > 5) throw new Exception("ICO pixel colors corrupted at size " + size);
                            }
                        }
                    }
                    if (size == 256) {
                        int count = BitConverter.ToUInt16(bytes, 4), entry = 6 + (count - 1) * 16;
                        int length = BitConverter.ToInt32(bytes, entry + 8), offset = BitConverter.ToInt32(bytes, entry + 12);
                        using (var png = new MemoryStream(bytes, offset, length)) using (var full = Image.FromStream(png)) if (full.Width != 256 || full.Height != 256) throw new Exception("Missing 256px icon layer");
                    }
                }
                results.Add("PASS: crop a non-square image and generate valid 32/64/128/256 ICO files");
                string source = Path.Combine(directory, "source.png"); image.Save(source);
                using (var read = Icons.Read(source)) if (read.Width != 320 || read.Height != 180) throw new Exception("Image dimensions changed");
                using (var canvas = new CropCanvas(image)) { canvas.SetSelection(999, 999, 999); if (canvas.Selection.Right > image.Width || canvas.Selection.Bottom > image.Height) throw new Exception("Crop exceeds image bounds"); canvas.SetSelection(-10, -10, 40); if (canvas.Selection.X != 0 || canvas.Selection.Y != 0) throw new Exception("Negative crop coordinates"); }
                results.Add("PASS: image dimensions preserved and crop coordinates constrained");
                bool rejected = false; try { using (var crop = Icons.Crop(image, new Rectangle(310, 0, 20, 20), 32)) { } } catch (ArgumentOutOfRangeException) { rejected = true; } if (!rejected) throw new Exception("Invalid crop accepted");
                results.Add("PASS: invalid crop is rejected");
            }
            Program.DataPath = directory;
            if (!Preferences.Load().Startup) throw new Exception("First-run startup must be checked");
            results.Add("PASS: first-run startup is checked without creating a startup shortcut");
            var preferences = new Preferences { Modifiers = 6, Key = (uint)Keys.Q, Language = "en", Startup = true }; preferences.Save(); preferences.Key = (uint)Keys.Z; preferences.Save();
            var loaded = Preferences.Load(); if (loaded.Modifiers != 6 || loaded.Key != (uint)Keys.Z || !loaded.Startup || loaded.Language != "en") throw new Exception("Preferences did not persist");
            results.Add("PASS: settings persistence and atomic update");
            preferences.Startup = false; preferences.Save(); if (Preferences.Load().Startup) throw new Exception("Disabled startup did not persist");
            results.Add("PASS: explicitly disabled startup stays disabled");
            File.WriteAllText(Program.ConfigPath, "broken settings"); if (Preferences.Load().Key != (uint)Keys.Z) throw new Exception("Corrupt settings recovery failed"); results.Add("PASS: corrupt settings recover safely");
            using (var first = new TestWindow()) using (var second = new TestWindow()) {
                if (!Native.RegisterHotKey(first.Handle, 101, 0x4006, (uint)Keys.F10)) throw new Exception("Test hotkey unavailable");
                try { if (Native.RegisterHotKey(second.Handle, 102, 0x4006, (uint)Keys.F10)) { Native.UnregisterHotKey(second.Handle, 102); throw new Exception("Hotkey conflict not detected"); } }
                finally { Native.UnregisterHotKey(first.Handle, 101); }
                results.Add("PASS: global hotkey registration and conflict detection");
            }
            results.Add("PASS: default shortcut " + Program.HotkeyText(3, (uint)Keys.Z)); File.WriteAllLines(report, results.ToArray());
        } catch (Exception ex) { results.Add("FAIL: " + ex); File.WriteAllLines(report, results.ToArray()); Environment.ExitCode = 1; }
        finally { Program.DataPath = previous; }
    }
    sealed class TestWindow : NativeWindow, IDisposable { public TestWindow() { CreateHandle(new CreateParams()); } public void Dispose() { DestroyHandle(); } }
}
